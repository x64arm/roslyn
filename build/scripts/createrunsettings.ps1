[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$release = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $RepoRoot
    
    Write-Host "Repo Dir $RepoRoot"
    Write-Host "Binaries Dir $binariesDir"
    
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path (Join-Path $binariesDir "VSSetup") $buildConfiguration
    
    $optProfToolDir = Get-PackageDir "Roslyn.OptProf.RunSettings.Generator"
    $optProfToolExe = Join-Path $optProfToolDir "tools\roslyn.optprof.runsettings.generator.exe"
    $configFile = Join-Path $RepoRoot "build\config\optprof.json"
    $outputFolder = Join-Path $configDir "Insertion\RunSettings"
    $optProfArgs = "--configFile $configFile --outputFolder $outputFolder --testsUrl Tests/DevDiv/VS/1cb894ff0c2fbdcf8b8e0169da42d43d5f3f4743/763bc48b-4b9b-61e6-85db-6b084597e9d2 "
    
    # https://github.com/dotnet/roslyn/issues/31486
    $dest = Join-Path $RepoRoot ".vsts-ci.yml"
    try {
        Copy-Item (Join-Path $RepoRoot "azure-pipelines-official.yml") $dest
        Exec-Console $optProfToolExe $optProfArgs
    }
    finally {
        Remove-Item $dest
    }
        
    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
