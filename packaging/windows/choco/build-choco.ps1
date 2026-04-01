<#
.SYNOPSIS
    Build Chocolatey packages for cowsay-dotnet.

.DESCRIPTION
    Creates .nupkg Chocolatey packages for each Windows RID.
    Requires pre-built binaries from build/publish.ps1 and choco CLI.

.PARAMETER Rid
    Build for a single RID only. If omitted, builds all Windows RIDs.

.EXAMPLE
    ./packaging/windows/choco/build-choco.ps1
    ./packaging/windows/choco/build-choco.ps1 -Rid win-x64
#>

[CmdletBinding()]
param(
    [string]$Rid
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir ".." ".." "..")
$PackageName = "cowsay-dotnet"

# Read version from Directory.Build.props
[xml]$Props = Get-Content (Join-Path $RepoRoot "Directory.Build.props")
$Version = $Props.Project.PropertyGroup.Version

$WindowsRids = @("win-x64", "win-x86", "win-arm", "win-arm64")

if ($Rid) {
    if ($Rid -notin $WindowsRids) {
        Write-Error "Unknown RID '$Rid'. Valid: $($WindowsRids -join ', ')"
        exit 1
    }
    $WindowsRids = @($Rid)
}

$BuildOutput = Join-Path $RepoRoot "build" "output"

foreach ($rid in $WindowsRids) {
    Write-Host "=== Building Chocolatey package for $rid ===" -ForegroundColor Cyan

    $RidOutput = Join-Path $ScriptDir $rid
    $CowsayBin = Join-Path $BuildOutput "cowsay" $rid
    $CowthinkBin = Join-Path $BuildOutput "cowthink" $rid

    if (-not (Test-Path $CowsayBin)) {
        Write-Error "Published binaries not found at $CowsayBin. Run 'build/publish.ps1 -Rid $rid' first."
        exit 1
    }

    # Create staging area
    $Staging = Join-Path $RidOutput "staging"
    if (Test-Path $Staging) { Remove-Item -Recurse -Force $Staging }
    New-Item -ItemType Directory -Path $Staging -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Staging "tools") -Force | Out-Null

    # Copy binaries to tools/
    Copy-Item (Join-Path $CowsayBin "cowsay.exe") (Join-Path $Staging "tools" "cowsay.exe")

    if (Test-Path $CowthinkBin) {
        Copy-Item (Join-Path $CowthinkBin "cowthink.exe") (Join-Path $Staging "tools" "cowthink.exe")
    }

    # Copy cow files
    $ToolsCowsDir = Join-Path $Staging "tools" "assets" "cows"
    New-Item -ItemType Directory -Path $ToolsCowsDir -Force | Out-Null
    $SourceCows = Join-Path $CowsayBin "assets" "cows"
    if (Test-Path $SourceCows) {
        Copy-Item -Path (Join-Path $SourceCows "*") -Destination $ToolsCowsDir -Recurse -Force
    } else {
        Copy-Item -Path (Join-Path $RepoRoot "assets" "cows" "*") -Destination $ToolsCowsDir -Recurse -Force
    }

    # Copy install/uninstall scripts
    Copy-Item (Join-Path $ScriptDir "tools" "chocolateyinstall.ps1") (Join-Path $Staging "tools" "chocolateyinstall.ps1")
    Copy-Item (Join-Path $ScriptDir "tools" "chocolateyuninstall.ps1") (Join-Path $Staging "tools" "chocolateyuninstall.ps1")

    # Generate nuspec with correct version
    $NuspecContent = Get-Content (Join-Path $ScriptDir "cowsay-dotnet.nuspec") -Raw
    $NuspecContent = $NuspecContent -replace '<version>.*?</version>', "<version>$Version</version>"
    $NuspecPath = Join-Path $Staging "cowsay-dotnet.nuspec"
    Set-Content -Path $NuspecPath -Value $NuspecContent

    # Build the Chocolatey package
    Push-Location $Staging
    choco pack $NuspecPath --outputdirectory (Resolve-Path $RidOutput)
    Pop-Location

    # Clean up staging
    Remove-Item -Recurse -Force $Staging

    Write-Host "  -> $RidOutput\" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=== Chocolatey build complete ===" -ForegroundColor Cyan
