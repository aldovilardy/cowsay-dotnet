<#
.SYNOPSIS
    Build and optionally publish the cowsay-dotnet PowerShell module.

.DESCRIPTION
    Stages the Cowsay.PowerShell module into a distribution-ready layout
    for each Windows RID, suitable for publishing to the PowerShell Gallery.

    The module includes:
      - Cowsay.PowerShell.psd1 (manifest)
      - Cowsay.PowerShell.psm1 (script module for COWPATH management)
      - Cowsay.PowerShell.dll + dependency DLLs (binary cmdlets)
      - assets/cows/*.cow (cow art files)

.PARAMETER Rid
    Build for a single RID only. If omitted, builds all Windows RIDs.

.PARAMETER Publish
    If specified, publishes the module to PSGallery. Requires -NuGetApiKey.

.PARAMETER NuGetApiKey
    API key for publishing to PSGallery.

.EXAMPLE
    ./packaging/windows/powershell/build-psmodule.ps1
    ./packaging/windows/powershell/build-psmodule.ps1 -Rid win-x64
    ./packaging/windows/powershell/build-psmodule.ps1 -Publish -NuGetApiKey "your-key"
#>

[CmdletBinding()]
param(
    [string]$Rid,
    [switch]$Publish,
    [string]$NuGetApiKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir ".." ".." "..")
$ModuleName = "Cowsay.PowerShell"

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
    Write-Host "=== Staging PowerShell module for $rid ===" -ForegroundColor Cyan

    $PsOutput = Join-Path $BuildOutput "powershell" $rid
    $RidOutput = Join-Path $ScriptDir $rid

    if (-not (Test-Path $PsOutput)) {
        Write-Error "Published PowerShell module not found at $PsOutput. Run 'build/publish.ps1 -Rid $rid -Projects powershell' first."
        exit 1
    }

    # Create distribution-ready module layout
    $ModuleDir = Join-Path $RidOutput $ModuleName
    if (Test-Path $ModuleDir) { Remove-Item -Recurse -Force $ModuleDir }
    New-Item -ItemType Directory -Path $ModuleDir -Force | Out-Null

    # Copy all DLLs (module + dependencies)
    Get-ChildItem -Path $PsOutput -Filter "*.dll" | ForEach-Object {
        Copy-Item $_.FullName $ModuleDir
    }

    # Copy module manifest and script module
    $PsdFile = Join-Path $PsOutput "$ModuleName.psd1"
    $PsmFile = Join-Path $PsOutput "$ModuleName.psm1"
    $PsSourceDir = Join-Path $RepoRoot "src" $ModuleName

    if (Test-Path $PsdFile) {
        Copy-Item $PsdFile $ModuleDir
    } elseif (Test-Path (Join-Path $PsSourceDir "$ModuleName.psd1")) {
        Copy-Item (Join-Path $PsSourceDir "$ModuleName.psd1") $ModuleDir
    }

    if (Test-Path $PsmFile) {
        Copy-Item $PsmFile $ModuleDir
    } elseif (Test-Path (Join-Path $PsSourceDir "$ModuleName.psm1")) {
        Copy-Item (Join-Path $PsSourceDir "$ModuleName.psm1") $ModuleDir
    }

    # Copy cow files
    $CowsDir = Join-Path $ModuleDir "assets" "cows"
    New-Item -ItemType Directory -Path $CowsDir -Force | Out-Null
    $SourceCows = Join-Path $PsOutput "assets" "cows"
    if (Test-Path $SourceCows) {
        Copy-Item -Path (Join-Path $SourceCows "*") -Destination $CowsDir -Recurse -Force
    } else {
        Copy-Item -Path (Join-Path $RepoRoot "assets" "cows" "*") -Destination $CowsDir -Recurse -Force
    }

    # Update module version in .psd1
    $PsdPath = Join-Path $ModuleDir "$ModuleName.psd1"
    if (Test-Path $PsdPath) {
        $PsdContent = Get-Content $PsdPath -Raw
        $PsdContent = $PsdContent -replace "ModuleVersion\s*=\s*'[^']*'", "ModuleVersion = '$Version'"
        Set-Content -Path $PsdPath -Value $PsdContent
    }

    # Validate the module manifest
    try {
        $ManifestInfo = Test-ModuleManifest -Path $PsdPath -ErrorAction Stop
        Write-Host "  Module: $($ManifestInfo.Name) v$($ManifestInfo.Version)" -ForegroundColor Green
        Write-Host "  Exported cmdlets: $($ManifestInfo.ExportedCmdlets.Keys -join ', ')" -ForegroundColor Green
    } catch {
        Write-Warning "Module manifest validation failed: $_"
    }

    Write-Host "  -> $ModuleDir" -ForegroundColor Green
    Write-Host ""

    # Publish if requested
    if ($Publish) {
        if (-not $NuGetApiKey) {
            Write-Error "NuGetApiKey is required for publishing. Use -NuGetApiKey parameter."
            exit 1
        }

        Write-Host "Publishing $ModuleName v$Version to PSGallery..." -ForegroundColor Yellow
        Publish-Module -Path $ModuleDir -NuGetApiKey $NuGetApiKey -Repository PSGallery -Verbose
        Write-Host "  Published successfully!" -ForegroundColor Green
    }
}

Write-Host "=== PowerShell module staging complete ===" -ForegroundColor Cyan
if (-not $Publish) {
    Write-Host ""
    Write-Host "To publish, run:"
    Write-Host "  ./build-psmodule.ps1 -Publish -NuGetApiKey 'your-api-key'"
}
