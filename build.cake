#addin Newtonsoft.Json&version=10.0.3
#addin Cake.Coolblue&version=0.1.1
#addin nuget:?package=Cake.Yaml&version=2.0.0
#addin nuget:?package=YamlDotNet&version=4.2.1
#addin nuget:?package=AWSSDK.KeyManagementService&version=3.3.5.2
#addin nuget:?package=AWSSDK.Core&version=3.3.21.17
#addin nuget:?package=AWSSDK.SecurityToken&version=3.3.3.6
#tool nuget:?package=OctopusTools&version=4.30.10

#load "lib.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");
var verbosity = Argument<Cake.Core.Diagnostics.Verbosity>("verbosity");
var version = Argument<string>("version", "0.0.1");

///////////////////////////////////////////////////////////////////////////////
// ENVIRONMENT INPUTS - Build Server Contract
///////////////////////////////////////////////////////////////////////////////
var octopusServer = EnvironmentVariable("OCTOPUS_SERVER");
var octopusApiKey = EnvironmentVariable("OCTOPUS_APIKEY");
var environment = EnvironmentVariable("RELEASE_ENVIRONMENT"); // Used by "release" - Which is the target environment we are releasing to?
var releaseVersion = EnvironmentVariable("RELEASE_VERSION");  // Used by "release" - Which version is supposed to be released?
var buildId = EnvironmentVariable("TEAMCITY_BUILD_ID");       // Used by "buildrelease" - To identify the current build for artifacts

var dotNetCoreVerbosity = (Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity)(int)verbosity;
BuildConfiguration buildConfiguration; 

///////////////////////////////////////////////////////////////////////////////
// Setup
///////////////////////////////////////////////////////////////////////////////
Setup(ctx =>
{
    var buildVersion = EnvironmentVariable("BUILD_NUMBER");
    if(string.IsNullOrWhiteSpace(buildVersion) == false)
    {
        Information("Version found from BUILD_NUMBER environment variable. Using new value of '{0}' instead of previously specified '{1}'.", buildVersion, version);
        version = buildVersion;
    }

    buildConfiguration = DeserializeYamlFromFile<BuildConfiguration>("build-info.yml");
    buildConfiguration.Context = ctx;
    buildConfiguration.SetEnvironmentVariables();
});

///////////////////////////////////////////////////////////////////////////////
// STANDARD PIPELINE TARGETS - INVOKED FROM TEAMCITY
///////////////////////////////////////////////////////////////////////////////
Task("pullrequest")
    .Description("Runs on TeamCity after creating a PR.")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test-Unit")
    .Does(() => {
        //Fill in additional customization for pullrequest target. Try to make it modular ;)
    });

Task("buildrelease")
    .Description("Create a new release and provide package ready for deployment")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test-Unit")
    .IsDependentOn("Test-Integration")
    .IsDependentOn("Publish")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish-Octopus")
    .IsDependentOn("Create-Release-Octopus")
    .Does(() => {

        // Output contract: Report RELEASE_VERSION back to Build Server - extensibility point for flexible versioning schemes
        TeamCity.SetParameter("env.RELEASE_VERSION", version);
        
        // Output contract: Publish artifacts to TeamCity
        TeamCity.PublishArtifacts(buildConfiguration.Output.ArtifactsFolder);

        // Output contract: Make the artifacts URL available for chained build configurations
        TeamCity.SetParameter("env.ARTIFACTS_ROOT_URL", string.Format("{0}/httpAuth/app/rest/builds/id:{1}/artifacts/content/", buildConfiguration.GetEnvironmentVariable("TEAMCITY_URL"), buildId));

        //Fill in additional customization for buildrelease target. Try to make it modular ;)
    });

Task("release")
    .Description("Deploys the release to the RELEASE_ENVIROMENT environment and runs acceptance tests")
    .IsDependentOn("Release-To-Environment")
    .IsDependentOn("Validate-Deployment")
    .Does(() => {
        //Fill in additional customization for release target. Try to make it modular ;) 
    });

///////////////////////////////////////////////////////////////////////////////
// Task definitions
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Description("Cleans all directories that are used during the build process.")
    .Does(() =>
{
    CleanDirectories("./**/bin/" + configuration);
    CleanDirectories("./**/obj");
    CleanDirectory(buildConfiguration.Output.ArtifactsFolder);
    CleanDirectory(buildConfiguration.Output.PublishFolder);

    Information("Cleaning completed.");
});

Task("WriteBuildMetaJsonFile")
    .Description("Builds all the different parts of the project.")
    .Does(() =>
    {
        var projectsForBuildMeta = buildConfiguration.Output.ForBuildMeta();

        foreach(var proj in projectsForBuildMeta)
        {
            DirectoryPath propertiesDir = ((FilePath)File(proj.Path)).GetDirectory().Combine(Directory("Properties"));
            FilePath buildMetaFile = propertiesDir.CombineWithFilePath(File("build.meta.json"));

            CreateDirectory(propertiesDir);

            var metadataFileSettings = new CoolblueBuildMetadataFileSettings
            {
                BuildNumber = version,
                FileLocation = buildMetaFile.FullPath
            };

            CreateBuildMetadataFile(metadataFileSettings);
            Information("Generated build.meta.json information at {0}", buildMetaFile.FullPath);
        }
    });

Task("Build")
    .Description("Builds all the different parts of the project.")
    .IsDependentOn("WriteBuildMetaJsonFile")
    .Does(() =>
{
    var msBuildSettings = new DotNetCoreMSBuildSettings {
        // Default to use MSBuildTreatAllWarningsAs.Error. 
        // Switch to MSBuildTreatAllWarningsAs.Default for default MSBuild behavior or MSBuildTreatAllWarningsAs.Message to just log warnings from build.
        TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error,
        Verbosity = dotNetCoreVerbosity,
        DiagnosticOutput = true
    };

    // Ensure the version property in all project files is overriden
    // by the build version so it matches the build meta data.
    msBuildSettings.Properties.Add("Version", new [] { version });
    msBuildSettings.Properties.Add("AssemblyVersion", new [] { version });
    msBuildSettings.Properties.Add("PackageVersion", new [] { version });
    msBuildSettings.Properties.Add("FileVersion", new [] { version });
    msBuildSettings.Properties.Add("Copyright", new [] { "Copyright " + DateTime.Now.Year + " (c) Coolblue BV. All rights reserved." });

    var settings = new DotNetCoreBuildSettings {
        Configuration = configuration,
        MSBuildSettings = msBuildSettings
    };

    DotNetCoreBuild(buildConfiguration.SolutionName, settings);
});

Task("Test-Unit")
    .Description("Runs all your unit tests, using dotnet test.")
    .Does(() => { Test(buildConfiguration.Test.UnitProjectPattern, dotNetCoreVerbosity, configuration); });

Task("Test-Integration")
    .Description("Runs all your integration tests, using dotnet test.")
    .Does(async () => { 
        //TODO: Create a fallback local-only dev secrets file to run local integration tests
        await buildConfiguration.DecryptAndProvideSecrets("testing");
        Test(buildConfiguration.Test.IntegrationProjectPattern, dotNetCoreVerbosity, configuration); 
    });

Task("Publish")
    .Description("Publishes all the different parts of the project, using OctoPack")
    .Does(() => 
{
        var msBuildSettings = new DotNetCoreMSBuildSettings {};
        
        msBuildSettings.Properties.Add("AssemblyVersion", new [] { version });
        msBuildSettings.Properties.Add("PackageVersion", new [] { version });
        msBuildSettings.Properties.Add("FileVersion", new [] { version });
        msBuildSettings.Properties.Add("Version", new [] { version });
        msBuildSettings.Properties.Add("Copyright", new [] { "Copyright " + DateTime.Now.Year + " (c) Coolblue BV. All rights reserved." });

    var projectsToPublish = buildConfiguration.Output.Projects;
    foreach(var proj in projectsToPublish)
    {
        var projectPublishFolder = ((DirectoryPath)Directory(buildConfiguration.Output.PublishFolder)).Combine(Directory(proj.Id));
        Verbose("Publishing '{0}'...", proj.Path);

        var settings = new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = projectPublishFolder,
            Verbosity = dotNetCoreVerbosity,
            MSBuildSettings = msBuildSettings
        };

        DotNetCorePublish(File(proj.Path), settings);

        Verbose("'{0}' has been published.", proj.Path);
    }
});

Task("Pack")
    .Description("Packs your application, using OctoPack.")
    .Does(() => {
        var publishRoot = ((DirectoryPath)buildConfiguration.Output.PublishFolder);
        Verbose("Packaging the projects under '" + buildConfiguration.Output.PublishFolder + "'...");
        
        foreach(var packProject in buildConfiguration.Output.Projects) {
            var packSource = publishRoot.Combine(Directory(packProject.Id)).ToString();

            var settings = new OctopusPackSettings
            {
                BasePath = packSource,
                OutFolder = buildConfiguration.Output.ArtifactsFolder,
                Version = version,
                Overwrite = true
            };
            
            var packageName = packProject.GetPackageName(buildConfiguration.Output.PackageNameSuffix);

            OctoPack(packageName, settings);

            Verbose("'" + buildConfiguration.Output.PublishFolder + "' has been packaged.");
        }
    });

Task("Publish-Octopus")
    .Description("Publish your packages, using OctoPush.")
    .Does(() => { 
        var packagesToPublish = buildConfiguration.Output.PackagesForRelease(version);

        foreach(var package in packagesToPublish)
        {
            Verbose("Pushing the '" + package.File.FullPath + "' to Octopus...");

            var settings = new OctopusPushSettings
            {
                ApiKey = octopusApiKey,
                Server = octopusServer,
            };

            OctoPush(octopusServer, octopusApiKey, package.File.FullPath, settings);

            Verbose("'" + package.File.FullPath + "' has been pushed to Octopus.");
        }
    });

Task("Create-Release-Octopus")
    .Description("Create your release, using OctoRelease.")
    .Does(() => { 
        Verbose("Creating the release in Octopus...");
        var releasePackages = buildConfiguration.Output.PackagesForRelease(version);
        
        var settings = new CreateReleaseSettings
        {
            ReleaseNumber = version,
            ApiKey = octopusApiKey,
            Server = octopusServer,
            Packages = releasePackages.ToDictionary(p => p.Name, p => p.Version)
        };

        OctoCreateRelease(buildConfiguration.Octopus.ProjectName, settings);
        
        Verbose("Release has created in Octopus.");
    });

Task("Release-To-Environment")
    .Description("Triggers a deployment of a previously created release to RELEASE_ENVIRONMENT environment.")
    .Does(() => {
        var isEnvironmentSpecified = !string.IsNullOrEmpty(environment);
        var isReleaseVersionSpecified = !string.IsNullOrEmpty(releaseVersion);

        if(!isEnvironmentSpecified) {
            throw new Exception("'Release' target requires a valid environment target set on RELEASE_ENVIRONMENT environment variable.");
        }
        if(!isReleaseVersionSpecified) {
            throw new Exception("'Release' target required a valid release version set on RELEASE_VERSION environment variable.");
        }
        
        TeamCity.WriteStatus(string.Format("Releasing version '{0}' to environment '{1}'.", releaseVersion, environment));
        
        TeamCity.WriteStartBuildBlock("deployment");
        
        var octoDeploySettings = new OctopusDeployReleaseDeploymentSettings {
            ApiKey = octopusApiKey,
            Server = octopusServer,
            ShowProgress = true,
        };

        OctoDeployRelease(octopusServer, octopusApiKey, buildConfiguration.Octopus.ProjectName, environment, releaseVersion, octoDeploySettings);
        
        TeamCity.WriteEndBuildBlock("deployment");
    });

Task("GetAcceptanceTestArtifacts")
    .Does(() => 
    {
        foreach(var p in buildConfiguration.Output.GetPackages(releaseVersion)) {
            Verbose("Package in release '{0}': {1}", releaseVersion, p.ToString());
        }

        CleanDirectory("./acceptance-tests");

        var accProjectPackages = buildConfiguration.Output.GetPackages(releaseVersion).Where(p => p.Id == "acceptance-tests");

        foreach(var accProjectPackage in accProjectPackages)
        {
            var artifactUrl = UriCombine(EnvironmentVariable("ARTIFACTS_ROOT_URL"), accProjectPackage.File.GetFilename().ToString());
            Information("Artifact Url: {0}", artifactUrl);

            var downloadSettings = new DownloadFileSettings {
                Username = EnvironmentVariable("TEAMCITY_ARTIFACT_USER"),
                Password = EnvironmentVariable("TEAMCITY_ARTIFACT_PASSWORD")
            };

            var package = DownloadFile(artifactUrl, downloadSettings);
            Unzip(package, "./acceptance-tests/" + accProjectPackage.File.GetFilenameWithoutExtension());
        }
    });

Task("Validate-Deployment")
    .IsDependentOn("GetAcceptanceTestArtifacts")
    .Description("Runs tests against a freshly deployed release to validate it.")
    .Does(async () => {
        TeamCity.WriteStartBuildBlock("acceptance-tests");

        var testAssemblies = GetFiles("./acceptance-tests/**/*.dll");
        var testVerbosity = Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity.Normal;

        await buildConfiguration.DecryptAndProvideSecrets(environment);

        var settings = new DotNetCoreVSTestSettings
        {
            Framework = ".NETFramework,Version=v4.6.1",
            Parallel = false,
            Logger = "console;verbosity=" + testVerbosity.ToString().ToLower(),
            Platform = VSTestPlatform.Default
        };

        DotNetCoreVSTest(testAssemblies.Select(t => (FilePath)t.FullPath).ToArray(), settings);

        TeamCity.WriteEndBuildBlock("acceptance-tests");
    });

Task("Default")
    .Description("This is the default task which will run if no specific target is passed in.")
    .IsDependentOn("pullrequest")
    .Does(() => { 
        Warning("No 'Target' was passed in, so we ran the 'pullrequest' operation.");
    });

RunTarget(target);
