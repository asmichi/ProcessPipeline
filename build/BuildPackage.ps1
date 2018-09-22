
# Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [switch]
    $RetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

function Exec {
    param(
        [parameter(Mandatory = $true)]
        [scriptblock]
        $cmd
    )

    & $cmd

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Command failed with exit code ${LASTEXITCODE}: $cmd"
    }
}

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
$slnFile = "$worktreeRoot\src\ProcessPipeline.sln"

$shortCommitHash = (git rev-parse --short=10 HEAD)
$commitCount = (git rev-list --count HEAD)
$baseVersion = Get-Content "$worktreeRoot\build\Version.txt"
$assemblyVersion = "$baseVersion.0"
$fileVersion = $assemblyVersion
$informationalVersion = "$fileVersion+g$shortCommitHash"
$packageVersion = if ($RetailRelease) { $baseVersion } else { "$baseVersion-pre.$commitCount+g$shortCommitHash" }

$commonBuildOptions = @("-nologo",
    "--verbosity:quiet",
    "-p:Platform=AnyCPU",
    "--configuration",
    "Release",
    "-p:Version=$assemblyVersion",
    "-p:PackageVersion=$packageVersion",
    "-p:FileVersion=$fileVersion",
    "-p:AssemblyVersion=$assemblyVersion",
    "-p:InformationalVersion=$informationalVersion"
)

Exec { dotnet restore --verbosity:quiet $slnFile }

# The ProjectDependency to TestChild in the sln will have ProcessPipeline.Test
# build TestChild with TargetFramework set to netcoreapp2.1 and fail...
# Build projects separately.
Exec { dotnet build $commonBuildOptions "$worktreeRoot\src\TestChild" }
Exec { dotnet test $commonBuildOptions "$worktreeRoot\src\ProcessPipeline.Test" }

Exec { dotnet build $commonBuildOptions --no-incremental "$worktreeRoot\src\ProcessPipeline" }

Exec {
    nuget pack `
        -Verbosity quiet -ForceEnglishOutput `
        -Version $packageVersion `
        -BasePath "$worktreeRoot\bin\ProcessPipeline\AnyCPU\Release" `
        -OutputDirectory "$worktreeRoot\bin\nupkg" `
        "$worktreeRoot\build\nuspec\Asmichi.ProcessPipeline.nuspec"
}
