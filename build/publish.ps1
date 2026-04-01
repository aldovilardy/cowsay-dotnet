<#
.SYNOPSIS
    Master publish script for cowsay-dotnet.
    Builds self-contained, single-file binaries for all target platforms.

.DESCRIPTION
    Publishes Cowsay.Cli (cowsay), Cowsay.Cowthink (cowthink), and
    Cowsay.PowerShell for every supported Runtime Identifier (RID).

    Published artifacts are placed under build/output/{project}/{rid}/.

.PARAMETER Rid
    Build for a single RID only (e.g., "win-x64"). If omitted, builds all RIDs.

.PARAMETER Projects
    Comma-separated list of projects to publish: cli, cowthink, powershell, or all.
    Default: all.

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER OutputRoot
    Root output directory. Default: build/output (relative to repo root).

.EXAMPLE
    ./build/publish.ps1
    # Publishes all projects for all RIDs.

.EXAMPLE
    ./build/publish.ps1 -Rid win-x64
    # Publishes all projects for win-x64 only.

.EXAMPLE
    ./build/publish.ps1 -Rid linux-x64 -Projects cli,cowthink
    # Publishes only CLI executables for linux-x64.
#>

[CmdletBinding()]
param(
    [string]$Rid,

    [ValidateSet("all", "cli", "cowthink", "powershell")]
    [string[]]$Projects = @("all"),

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Resolve paths ───────────────────────────────────────────────────────────
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputRoot) {
    $OutputRoot = Join-Path $RepoRoot "build" "output"
}

# ── Read version from Directory.Build.props ─────────────────────────────────
$PropsFile = Join-Path $RepoRoot "Directory.Build.props"
if (-not (Test-Path $PropsFile)) {
    Write-Error "Directory.Build.props not found at $PropsFile"
    exit 1
}
[xml]$Props = Get-Content $PropsFile
$Version = $Props.Project.PropertyGroup.Version
Write-Host "=== cowsay-dotnet publish v$Version ===" -ForegroundColor Cyan

# ── Define RIDs ─────────────────────────────────────────────────────────────
$WindowsRids = @("win-x64", "win-x86", "win-arm64")
$MacRids     = @("osx-x64", "osx-arm64")
$LinuxRids   = @("linux-x64", "linux-arm", "linux-arm64")
$AllRids     = $WindowsRids + $MacRids + $LinuxRids

if ($Rid) {
    if ($Rid -notin $AllRids) {
        Write-Error "Unknown RID '$Rid'. Valid RIDs: $($AllRids -join ', ')"
        exit 1
    }
    $TargetRids = @($Rid)
} else {
    $TargetRids = $AllRids
}

# ── Define projects ─────────────────────────────────────────────────────────
$ProjectMap = @{
    cli        = @{
        Name    = "cowsay"
        CsProj  = Join-Path $RepoRoot "src" "Cowsay.Cli" "Cowsay.Cli.csproj"
        SingleFile = $true
    }
    cowthink   = @{
        Name    = "cowthink"
        CsProj  = Join-Path $RepoRoot "src" "Cowsay.Cowthink" "Cowsay.Cowthink.csproj"
        SingleFile = $true
    }
    powershell = @{
        Name    = "powershell"
        CsProj  = Join-Path $RepoRoot "src" "Cowsay.PowerShell" "Cowsay.PowerShell.csproj"
        SingleFile = $false
    }
}

if ("all" -in $Projects) {
    $SelectedProjects = @("cli", "cowthink", "powershell")
} else {
    $SelectedProjects = $Projects
}

# ── Publish ─────────────────────────────────────────────────────────────────
$AssetsDir = Join-Path $RepoRoot "assets" "cows"
$TotalSteps = $SelectedProjects.Count * $TargetRids.Count
$CurrentStep = 0

foreach ($projKey in $SelectedProjects) {
    $proj = $ProjectMap[$projKey]

    if (-not (Test-Path $proj.CsProj)) {
        Write-Warning "Project not found: $($proj.CsProj) -- skipping."
        continue
    }

    foreach ($rid in $TargetRids) {
        $CurrentStep++
        $pct = [math]::Round(($CurrentStep / $TotalSteps) * 100)
        Write-Host "[$pct%] Publishing $($proj.Name) for $rid..." -ForegroundColor Yellow

        $outDir = Join-Path $OutputRoot $proj.Name $rid

        # Clean previous output
        if (Test-Path $outDir) {
            Remove-Item -Recurse -Force $outDir
        }

        # Build dotnet publish arguments
        $publishArgs = @(
            "publish"
            $proj.CsProj
            "-c", $Configuration
            "-r", $rid
            "--self-contained", "true"
            "-o", $outDir
            "-p:Version=$Version"
        )

        if ($proj.SingleFile) {
            $publishArgs += "-p:PublishSingleFile=true"
            $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
        }

        # Run dotnet publish
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "dotnet publish failed for $($proj.Name) / $rid (exit code $LASTEXITCODE)"
            exit $LASTEXITCODE
        }

        # Ensure cow files are present in output
        $outCowsDir = Join-Path $outDir "assets" "cows"
        if (-not (Test-Path $outCowsDir)) {
            Write-Host "  Copying cow files to output..." -ForegroundColor DarkGray
            New-Item -ItemType Directory -Path $outCowsDir -Force | Out-Null
            Copy-Item -Path (Join-Path $AssetsDir "*") -Destination $outCowsDir -Recurse -Force
        }

        Write-Host "  -> $outDir" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Publish complete! ===" -ForegroundColor Cyan
Write-Host "Output: $OutputRoot"
Write-Host "Version: $Version"
Write-Host "RIDs: $($TargetRids -join ', ')"
Write-Host "Projects: $($SelectedProjects -join ', ')"
