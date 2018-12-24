# Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
. $worktreeRoot\Build\Common.ps1

Set-Location $workTreeRoot

$Command = @("make", "-C", "src/ProcessPipeline.Native", "-s", "-j", $env:NUMBER_OF_PROCESSORS)

Exec { wsl $Command CONFIGURATION=Debug }
Exec { wsl $Command CONFIGURATION=Release }
