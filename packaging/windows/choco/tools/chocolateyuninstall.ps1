$ErrorActionPreference = 'Stop'

$PackageName = 'cowsay-dotnet'

# Remove COWPATH environment variable
$EnvTarget = [System.EnvironmentVariableTarget]::Machine
$CurrentCowPath = [System.Environment]::GetEnvironmentVariable("COWPATH", $EnvTarget)
$InstallDir = Join-Path $env:ChocolateyInstall "lib\$PackageName\tools"
$CowsDir = Join-Path $InstallDir "assets" "cows"

if ($CurrentCowPath -eq $CowsDir) {
    [System.Environment]::SetEnvironmentVariable("COWPATH", $null, $EnvTarget)
    Write-Host "Removed COWPATH environment variable."
}

Write-Host "$PackageName has been uninstalled."
