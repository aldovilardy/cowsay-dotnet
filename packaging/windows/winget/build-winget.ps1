<#
.SYNOPSIS
    Generate WinGet manifest files for cowsay-dotnet.

.DESCRIPTION
    Creates WinGet-compatible YAML manifest files for submission to the
    winget-pkgs repository. One set of manifests per Windows RID.

    Requires pre-built MSI installers or published binaries from build/publish.ps1.

.PARAMETER Rid
    Generate for a single RID only. If omitted, generates for all Windows RIDs.

.PARAMETER InstallerBaseUrl
    Base URL where installers are hosted (e.g., GitHub Releases URL).
    Default: https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v{version}

.EXAMPLE
    ./packaging/windows/winget/build-winget.ps1
    ./packaging/windows/winget/build-winget.ps1 -Rid win-x64
#>

[CmdletBinding()]
param(
    [string]$Rid,
    [string]$InstallerBaseUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir ".." ".." "..")
$PackageName = "cowsay-dotnet"
$PackageIdentifier = "cowsay-dotnet.cowsay-dotnet"

# Read version from Directory.Build.props
[xml]$Props = Get-Content (Join-Path $RepoRoot "Directory.Build.props")
$Version = $Props.Project.PropertyGroup.Version

if (-not $InstallerBaseUrl) {
    $InstallerBaseUrl = "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v$Version"
}

# RID to WinGet architecture mapping
$RidToArch = @{
    "win-x64"   = "x64"
    "win-x86"   = "x86"
    "win-arm64" = "arm64"
}

$WindowsRids = @("win-x64", "win-x86", "win-arm64")

if ($Rid) {
    if ($Rid -notin $WindowsRids) {
        Write-Error "Unknown RID '$Rid'. Valid: $($WindowsRids -join ', ')"
        exit 1
    }
    $WindowsRids = @($Rid)
}

$InstallerDir = Join-Path $RepoRoot "packaging" "windows" "installer"

foreach ($rid in $WindowsRids) {
    $Arch = $RidToArch[$rid]
    $RidOutput = Join-Path $ScriptDir $rid

    Write-Host "=== Generating WinGet manifests for $rid ($Arch) ===" -ForegroundColor Cyan

    New-Item -ItemType Directory -Path $RidOutput -Force | Out-Null

    # Try to compute SHA256 from the MSI if it exists
    $MsiFile = Join-Path $InstallerDir $rid "${PackageName}-${Version}-${rid}.msi"
    $InstallerSha256 = "PLACEHOLDER_SHA256"
    if (Test-Path $MsiFile) {
        $InstallerSha256 = (Get-FileHash -Path $MsiFile -Algorithm SHA256).Hash
    } else {
        Write-Warning "MSI not found at $MsiFile -- using placeholder SHA256."
        Write-Warning "Build the MSI first, then re-run this script."
    }

    $InstallerUrl = "$InstallerBaseUrl/${PackageName}-${Version}-${rid}.msi"

    # Version manifest
    $VersionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
"@
    Set-Content -Path (Join-Path $RidOutput "$PackageIdentifier.yaml") -Value $VersionManifest

    # Installer manifest
    $InstallerManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
Platform:
  - Windows.Desktop
MinimumOSVersion: 10.0.17763.0
InstallerType: msi
Scope: machine
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
Commands:
  - cowsay
  - cowthink
Installers:
  - Architecture: $Arch
    InstallerUrl: $InstallerUrl
    InstallerSha256: $InstallerSha256
    ProductCode: "{7E3F8A2B-1C4D-4E5F-9A6B-8C7D0E1F2A3B}"
ManifestType: installer
ManifestVersion: 1.6.0
"@
    Set-Content -Path (Join-Path $RidOutput "$PackageIdentifier.installer.yaml") -Value $InstallerManifest

    # Locale manifest
    $LocaleManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.6.0.schema.json
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: en-US
Publisher: cowsay-dotnet contributors
PublisherUrl: https://github.com/cowsay-dotnet
PackageName: cowsay-dotnet
PackageUrl: https://github.com/cowsay-dotnet/cowsay-dotnet
License: GPL-3.0-or-later
LicenseUrl: https://github.com/cowsay-dotnet/cowsay-dotnet/blob/main/LICENSE
ShortDescription: A .NET reimplementation of the classic cowsay/cowthink utility
Description: |-
  cowsay-dotnet is a faithful .NET reimplementation of the classic Unix
  cowsay and cowthink programs. It generates ASCII art of a cow (or other
  characters) saying or thinking a given message.
  Self-contained — no .NET runtime required.
Tags:
  - cowsay
  - cowthink
  - ascii-art
  - cli
  - dotnet
ManifestType: defaultLocale
ManifestVersion: 1.6.0
"@
    Set-Content -Path (Join-Path $RidOutput "$PackageIdentifier.locale.en-US.yaml") -Value $LocaleManifest

    Write-Host "  -> $RidOutput\" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=== WinGet manifest generation complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Build MSI installers if not done already"
Write-Host "  2. Upload MSIs to GitHub Releases"
Write-Host "  3. Update SHA256 hashes in installer manifests"
Write-Host "  4. Submit manifests to microsoft/winget-pkgs via PR"
