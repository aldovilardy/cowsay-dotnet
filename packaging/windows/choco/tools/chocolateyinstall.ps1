$ErrorActionPreference = 'Stop'

$PackageName = 'cowsay-dotnet'
$InstallDir  = Join-Path $env:ChocolateyInstall "lib\$PackageName\tools"

# The tools directory contains the pre-built binaries and cow files.
# Chocolatey automatically creates shims for .exe files in the tools directory.

# Ensure cow files directory exists
$CowsDir = Join-Path $InstallDir "assets" "cows"
if (-not (Test-Path $CowsDir)) {
    Write-Warning "Cow art files directory not found at $CowsDir"
}

# Set COWPATH environment variable so cowsay can find its cow files
$EnvTarget = [System.EnvironmentVariableTarget]::Machine
$CurrentCowPath = [System.Environment]::GetEnvironmentVariable("COWPATH", $EnvTarget)
if (-not $CurrentCowPath) {
    [System.Environment]::SetEnvironmentVariable("COWPATH", $CowsDir, $EnvTarget)
    Write-Host "Set COWPATH=$CowsDir"
}

Write-Host "$PackageName has been installed."
Write-Host "Run 'cowsay Hello!' or 'cowthink Hello!' to get started."
