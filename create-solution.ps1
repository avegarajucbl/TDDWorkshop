##########################################################################
# The is the Coolblue service baseline creation script for Powershell.
#
# This was downloaded from: https://github.com/coolblue-development/net-service-baseline
##########################################################################

<#

.SYNOPSIS
This is a Powershell script to create a service according to the Coolblue service baseline.

.DESCRIPTION
This Powershell script will create a C# solution for you, including projects. You can choose
to create a WebApi or a Windows Service, as a main starting project. When left unspecified, this 
script will create a WebApi project for you.

NOTE: This Powershell script assumes you have the dotnet CLI on the PATH.
NOTE: The creation of a Windows Service is not possible at this point in time.

.PARAMETER name
The descriptive name of your solution. Defaults to current directory name.
.PARAMETER type
The type of the main project in your solution. WebApi or WebService. DEFAULT: WebApi
.PARAMETER persistence
The persistence technology type to use. DEFAULT: Oracle.
.PARAMETER examples
A flag as to whether to include example code. DEFAULT false.
.PARAMETER declarative
A flag as to whether coding style is declarative. DEFAULT false.

.LINK
https://github.com/dotnet/templating/wiki/%22Runnable-Project%22-Templates

.LINK
https://github.com/coolblue-development/net-service-baseline

#>

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [string]$name,
    [ValidateSet("WebApi", "WindowsService")]
    [string]$type = "WebApi",
    [ValidateSet("Oracle", "MySql")]
    [string]$persistence = "Oracle",
    [ValidateSet("net461", "net462", "net471", "netcoreapp1.1", "netcoreapp2.0")]
    [string]$framework = "net461",
    [switch]$examples,
    [switch]$declarative
)
$templateVersion = "2.1.0"
"TEMPLATING (version $templateVersion)" > 'template.log'
"Type $type" >> 'template.log'
"Targeting $framework" >> 'template.log'
"Using $persistance persistence" >> 'template.log'
if($examples) {
	"Will generate examples" >> 'template.log'
} else {
	"Will NOT generate examples" >> 'template.log'
}
if($declarative) {
	"Will use declarative style" >> 'template.log'
}
# assumes in folder you want solution
# assumes dotnet CLi is on the PATH

function Parse-AppTarget {
    if($framework -eq "netcoreapp1.1"){ return "netstandard1.6"}
    if($framework -eq "netcoreapp2.0"){ return "netstandard2.0"}
    return $framework
}

function Parse-Persistence {
    if($persistence -eq "Oracle"){ return "coolblueoracle"}
    if($persistence -eq "MySql"){ return "coolbluemysql"}
    return $persistence
}

function Highlight([String] $text){
    Write-Host $text -ForegroundColor Yellow
}

# global vars
$root = Get-Location;
$src = "$root/src";
$test = "$root/test";
$slnName = Split-Path (Split-Path $root -Leaf) -Leaf

if([string]::IsNullOrEmpty($name))
{ $name = $slnName }

Highlight "Creating a $type solution $name targeting $framework in directory $root"

# create solution
dotnet new sln --name $name

# Create cake scripts
$entryPointType = "Api";
if($type -eq "WindowsService") {
	$entryPointType = "WindowsService";
}
$octoProjectName = $name

dotnet new coolbluecakebuild --name $name --octoProjectName $octoProjectName --framework $framework --entryPointType $entryPointType

# structure
if(!(Test-Path -Path $src )){
    New-Item $src -ItemType Directory
}
if(!(Test-Path -Path $src )){
    New-Item $src -ItemType Directory
}

# Domain project
if(!(Test-Path -Path "$src/$name" )){
    New-Item "$src/$name" -ItemType Directory
    cd "$src/$name"
    $domainFramework = Parse-AppTarget $framework
    $note = "Attempt to create domain project $name targeting $domainFramework"
    $cmd = "dotnet new coolbluedomain --framework $domainFramework"
    if($examples){
        $cmd = $cmd + " --examples" 
        $note = $note + " (with examples)"
    }
    if($declarative){
        $cmd = $cmd + " --declarative" 
        $note = $note + " (declarative style)"
    }
    Highlight $note
    Invoke-Expression $cmd
    $note >> 'template.log'
    cd $root
    Invoke-Expression "dotnet sln $name.sln add $src/$name/$name.csproj"
}

# Persistence project
if(!(Test-Path -Path "$src/$name.Persistence.$persistence" )){
    New-Item "$src/$name.Persistence.$persistence" -ItemType Directory
    cd "$src/$name.Persistence.$persistence"
    $pFramework = Parse-AppTarget $framework
    $pShortName = Parse-Persistence $persistence
    $note = "Attempt to create $persistence project $name.Persistence.$persistence targeting $pFramework"
    $cmd = "dotnet new $pShortName --framework $pFramework"
    if($examples){
        $cmd = $cmd + " --examples" 
        $note = $note + " (with examples)"
    }
    if($declarative){
        $cmd = $cmd + " --declarative" 
        $note = $note + " (declarative style)"
    }
    Highlight $note
    Invoke-Expression $cmd
    $note >> 'template.log'
    cd $root
    Invoke-Expression "dotnet add $src/$name.Persistence.$persistence/$name.Persistence.$persistence.csproj reference $src/$name/$name.csproj"   
    Invoke-Expression "dotnet sln $name.sln add $src/$name.Persistence.$persistence/$name.Persistence.$persistence.csproj"
}

# Unit test project
if(!(Test-Path -Path $test )){
    New-Item $test -ItemType Directory
}
if(!(Test-Path -Path "$test/$name.Tests.Unit" )){
    New-Item "$test/$name.Tests.Unit" -ItemType Directory
    cd "$test/$name.Tests.Unit"
    $note = "Attempt to create test project $name.Tests.Unit targeting $framework"
    $cmd = "dotnet new coolblueunittest --framework $framework"
    if($examples){
        $cmd = $cmd + " --examples" 
        $note = $note + " (with examples)"
    }
    if($declarative){
        $cmd = $cmd + " --declarative" 
        $note = $note + " (declarative style)"
    }
    Highlight $note
    Invoke-Expression $cmd
    $note >> 'template.log'
    cd $root
    Invoke-Expression "dotnet add $test/$name.Tests.Unit/$name.Tests.Unit.csproj reference $src/$name/$name.csproj"
    Invoke-Expression "dotnet sln $name.sln add $test/$name.Tests.Unit/$name.Tests.Unit.csproj"
}

if($type -eq "WebApi"){
    # WebApi project
    if(!(Test-Path -Path "$src/$name.Api" )){
        New-Item "$src/$name.Api" -ItemType Directory
        New-Item "$src/$name.Api/wwwroot" -ItemType Directory
        cd "$src/$name.Api"
        $note = "Attempt to create test project $name.Api targeting $framework"
        $cmd = "dotnet new coolbluewebapi --framework $framework --persistence $name.Persistence.$persistence --persistenceType $persistence"
        if($examples){
            $cmd = $cmd + " --examples" 
            $note = $note + " (with examples)"
        }
        if($declarative){
            $cmd = $cmd + " --declarative" 
            $note = $note + " (declarative style)"
        }
        Highlight $note
        Invoke-Expression $cmd
        $note >> 'template.log'
        cd $root
        Invoke-Expression "dotnet add $src/$name.Api/$name.Api.csproj reference $src/$name/$name.csproj"
        Invoke-Expression "dotnet add $src/$name.Api/$name.Api.csproj reference $src/$name.Persistence.$persistence/$name.Persistence.$persistence.csproj"    
        Invoke-Expression "dotnet sln $name.sln add $src/$name.Api/$name.Api.csproj"
    }
}

if($type -eq "WindowsService"){
    # WindowsService project
    if(!(Test-Path -Path "$src/$name.Service" )){
        New-Item "$src/$name.Service" -ItemType Directory
        cd "$src/$name.Service"
        $note = "Attempt to create test project $name.Service targeting $framework"
        $cmd = "dotnet new coolbluewindowsservice"
        if($examples){
            $cmd = $cmd + " --examples" 
            $note = $note + " (with examples)"
        }
        Highlight $note
        Invoke-Expression $cmd
        $note >> 'template.log'
        cd $root
        Invoke-Expression "dotnet add $src/$name.Api/$name.Service.csproj reference $src/$name/$name.csproj"
        Invoke-Expression "dotnet add $src/$name.Api/$name.Service.csproj reference $src/$name.Persistence.$persistence/$name.Persistence.$persistence.csproj"    
        Invoke-Expression "dotnet sln $name.sln add $src/$name.Service/$name.Service.csproj"
    }
}
