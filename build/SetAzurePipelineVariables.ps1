
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param
(
    [Parameter(Mandatory = $true)]
    [string]
    $CommitHash,
    [Parameter(Mandatory = $true)]
    [string]
    $BranchName,
    [parameter()]
    [switch]
    $RetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

$versionInfo = Get-VersionInfo -CommitHash $CommitHash -RetailRelease:$RetailRelease
$commonBuildOptions = Get-CommonBuildOptions -VersionInfo $versionInfo
$commonBuildOptionsString = [string]$commonBuildOptions

Write-Host "##vso[task.setvariable variable=PackageVersion]$($versionInfo.PackageVersion)"
Write-Host "##vso[task.setvariable variable=CommonBuildOptions]$commonBuildOptionsString"
