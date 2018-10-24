using YamlDotNet.Serialization;
using Cake.Core.IO;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Runtime;

public class BuildConfiguration {
    [YamlMember(Alias = "solutionName")]
    public string SolutionName { get; set; }

    [YamlMember(Alias = "octopus")]
    public OctopusSettings Octopus { get; set; }

    [YamlMember(Alias = "output")]
    public OutputSettings Output { get; set; }

    [YamlMember(Alias = "test")]
    public TestSettings Test { get; set; }

    [YamlMember(Alias = "environment")]
    public EnvironmentVariableSettings EnvironmentVariableSettings { get; set; }

    public ICakeContext Context { get; set; }

    public void SetEnvironmentVariables() {
        foreach(var envVar in EnvironmentVariableSettings.Variables) {
            Environment.SetEnvironmentVariable(string.Format("{0}{1}", EnvironmentVariableSettings.Prefix, envVar.Key.ToUpper()), envVar.Value);
        }
    }

    public string GetEnvironmentVariable(string key) {
        return Environment.GetEnvironmentVariable(string.Format("{0}{1}", EnvironmentVariableSettings.Prefix, key.ToUpper()));
    }

    public async Task DecryptAndProvideSecrets(string environmentName) {
        Context.Information("Reading secrets for environment '{0}'.", environmentName);

        var secrets = EnvironmentVariableSettings.Secrets;
        var envKey = environmentName.ToLower();

        if(secrets == null || !secrets.ContainsKey(envKey))
            throw new Exception(string.Format("Invalid environment name '{0}' - no secrets defined for that environment.", envKey));

        var envSecrets = secrets[envKey];

        if(envSecrets.Any()) {
            var kmsClient = new KmsEncryptionClient();

            foreach(var kv in envSecrets) {
                var secretKey = kv.Key;
                var encryptedSecret = kv.Value;
                Context.Information("Decrypting '{0}' into environment...", kv.Key);

                try {
                    var decryptedSecret = await kmsClient.DecryptAsync(encryptedSecret);
                    Environment.SetEnvironmentVariable(secretKey, decryptedSecret);
                }
                catch(Exception ex) {
                    Context.Error("Error when decrypting secret '{0}'. Script will continue trying for other variables: {1}.", secretKey, ex.ToString());
                }
            }
        }
    }
}

public class EnvironmentVariableSettings {
    [YamlMember(Alias = "prefix")]
    public string Prefix { get; set; }

    [YamlMember(Alias = "vars")]
    public Dictionary<string, string> Variables { get; set; }

    [YamlMember(Alias = "secrets")]
    public Dictionary<string, Dictionary<string, string>> Secrets { get; set; }
}

public class OutputSettings {
    [YamlMember(Alias = "artifacts")]
    public string ArtifactsFolder { get; set; }
    [YamlMember(Alias = "publish")]
    public string PublishFolder { get; set; }
    [YamlMember(Alias = "projects")]
    public List<OutputProject> Projects { get; set; }
    [YamlMember(Alias = "packageSuffix")]
    public string PackageNameSuffix { get; set; }

    public IEnumerable<OutputProject> ForBuildMeta() {
        return Projects.Where(p => p.GenerateBuildMeta);
    }

    public IEnumerable<ReleasePackage> PackagesForRelease(string version) {
        return GetPackages(version, p => p.IncludeInOctopusRelease);
    }

    public IEnumerable<ReleasePackage> GetPackages(string version) {
        return GetPackages(version, p => true);
    }

    public IEnumerable<ReleasePackage> GetPackages(string version, Func<OutputProject, bool> projectPredicate)
    {
        var artifactsDir = new DirectoryPath(ArtifactsFolder);
        foreach(var proj in Projects.Where(projectPredicate)) {
            var packageName = proj.GetPackageName(PackageNameSuffix);
            var packageFile = artifactsDir.CombineWithFilePath(string.Format("{0}.{1}.nupkg", packageName, version));
                        
            yield return new ReleasePackage {
                Id = proj.Id,
                File = packageFile,
                Version = version,
                Name = packageName.ToString()
            };
        }
    }
}

public class OutputProject {

    public OutputProject() {
        IncludeInOctopusRelease = true;
    }

    [YamlMember(Alias = "path")]
    public string Path { get; set; }
    [YamlMember(Alias = "id")]
    public string Id { get; set; }
    [YamlMember(Alias = "generate-build-meta")]
    public bool GenerateBuildMeta { get; set; }

    [YamlMember(Alias = "octopus")]
    public bool IncludeInOctopusRelease { get; set; }
    [YamlMember(Alias = "octo-package-name")]
    public string OctoPackageName { get; set; }

    public string GetPackageName(string suffix) {
        var simplePackageName = new FilePath(Path).GetFilenameWithoutExtension();

        if(!string.IsNullOrEmpty(OctoPackageName))
        {
            simplePackageName = OctoPackageName;
        }

        if(string.IsNullOrWhiteSpace(suffix))
        {
            return simplePackageName.ToString();
        }
        
        return string.Format("{0}.{1}", simplePackageName, suffix);
    }
}

public class OctopusSettings {
    [YamlMember(Alias = "projectName")]
    public string ProjectName { get; set; }
    [YamlMember(Alias = "packageName")]
    public string PackageName { get; set; }
}

public class TestSettings {
    [YamlMember(Alias = "unit")]
    public string UnitProjectPattern { get; set; }
    [YamlMember(Alias = "integration")]
    public string IntegrationProjectPattern { get; set; }
    [YamlMember(Alias = "acceptance")]
    public TestProjectSettings Acceptance { get; set; }
}

public class TestProjectSettings {
    [YamlMember(Alias = "glob")]
    public string Glob { get; set; }

    [YamlMember(Alias = "packageName")]
    public string PackageName { get; set; }
}

public class ReleasePackage {
    public string Id { get; set; }
    public string Version { get; set; }
    public string Name { get; set; }
    public FilePath File { get; set; }

    public override string ToString() {
        return string.Format("ReleasePkg {{Name:{0}, Id:{1}, Version:{2}, File:{3}}}", Name, Id, Version, File.ToString());
    }
}

public static string UriCombine(this string uri1, string uri2) => $"{uri1.TrimEnd('/')}/{uri2.TrimStart('/')}";

void Test(string testDllGlob, Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity dotNetCoreVerbosity, string configuration)
{
    var testAssemblies = GetFiles(testDllGlob);
    var testVerbosity = Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity.Normal;

    foreach(var testProject in testAssemblies)
    {
        Information("Testing '{0}'...", testProject.FullPath);
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration,
            Verbosity = testVerbosity,
            NoBuild = true,
            NoRestore = true,
            Logger = "console;verbosity=" + testVerbosity.ToString().ToLower()
        };

        DotNetCoreTest(testProject.FullPath, settings);
    }
}

public class KmsEncryptionClient
{
    private AmazonKeyManagementServiceClient _client;

    public KmsEncryptionClient()
    {
        _client = new AmazonKeyManagementServiceClient();
    }
    public async Task<string> DecryptAsync(string encryptedValue)
    {
        var ciphertestStream = new MemoryStream(Convert.FromBase64String(encryptedValue)) { Position = 0 };

        var decryptRequest = new DecryptRequest { 
            CiphertextBlob = ciphertestStream 
        };

        var response = await _client.DecryptAsync(decryptRequest);

        var buffer = new byte[response.Plaintext.Length];

        var bytesRead = response.Plaintext.Read(buffer, 0, (int)response.Plaintext.Length);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    public async Task<string> EncryptAsync(string value, string key)
    {
        var plaintextData = new MemoryStream(Encoding.UTF8.GetBytes(value))
        {
            Position = 0
        };

        var encryptRequest = new EncryptRequest
        {
            KeyId = key,
            Plaintext = plaintextData
        };

        var response = await _client.EncryptAsync(encryptRequest);

        var buffer = new byte[response.CiphertextBlob.Length];

        response.CiphertextBlob.Read(buffer, 0, (int)response.CiphertextBlob.Length);

        return Convert.ToBase64String(buffer);
    }
}