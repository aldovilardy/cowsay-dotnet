<#
.SYNOPSIS
    Build MSI installers for cowsay-dotnet using WiX Toolset v4.

.DESCRIPTION
    Creates .msi Windows installer packages for each Windows RID.
    Requires pre-built binaries from build/publish.ps1 and WiX v4 CLI.

.PARAMETER Rid
    Build for a single RID only. If omitted, builds all Windows RIDs.

.EXAMPLE
    ./packaging/windows/installer/build-installer.ps1
    ./packaging/windows/installer/build-installer.ps1 -Rid win-x64
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

# RID to MSI platform mapping
$RidToPlatform = @{
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

$BuildOutput = Join-Path $RepoRoot "build" "output"
$WxsSource = Join-Path $ScriptDir "cowsay-dotnet.wxs"

foreach ($rid in $WindowsRids) {
    $Platform = $RidToPlatform[$rid]
    $RidOutput = Join-Path $ScriptDir $rid

    Write-Host "=== Building MSI for $rid ($Platform) ===" -ForegroundColor Cyan

    $CowsayBin = Join-Path $BuildOutput "cowsay" $rid
    $CowthinkBin = Join-Path $BuildOutput "cowthink" $rid

    if (-not (Test-Path $CowsayBin)) {
        Write-Error "Published binaries not found at $CowsayBin. Run 'build/publish.ps1 -Rid $rid' first."
        exit 1
    }

    # Create staging directory with binaries
    $StagingBin = Join-Path $RidOutput "staging" "bin"
    $StagingCows = Join-Path $RidOutput "staging" "cows"
    if (Test-Path (Join-Path $RidOutput "staging")) {
        Remove-Item -Recurse -Force (Join-Path $RidOutput "staging")
    }
    New-Item -ItemType Directory -Path $StagingBin -Force | Out-Null
    New-Item -ItemType Directory -Path $StagingCows -Force | Out-Null

    # Copy binaries
    Copy-Item (Join-Path $CowsayBin "cowsay.exe") $StagingBin
    if (Test-Path $CowthinkBin) {
        Copy-Item (Join-Path $CowthinkBin "cowthink.exe") $StagingBin
    }

    # Copy cow files
    $SourceCows = Join-Path $CowsayBin "assets" "cows"
    if (Test-Path $SourceCows) {
        Copy-Item -Path (Join-Path $SourceCows "*") -Destination $StagingCows -Recurse -Force
    } else {
        Copy-Item -Path (Join-Path $RepoRoot "assets" "cows" "*") -Destination $StagingCows -Recurse -Force
    }

    # Generate dynamic WiX fragment for cow files
    $CowFragmentPath = Join-Path $RidOutput "staging" "cowfiles.wxs"
    $CowComponents = @()
    $CowComponentRefs = @()
    $Counter = 0

    Get-ChildItem -Path $StagingCows -Filter "*.cow" | ForEach-Object {
        $Counter++
        $CompId = "CowFile_$Counter"
        $FileId = "cow_$($_.BaseName -replace '[^a-zA-Z0-9_]', '_')"
        $CowComponents += @"
      <Component Id="$CompId" Directory="COWSFOLDER" Guid="*">
        <File Id="$FileId" Source="$($_.FullName)" KeyPath="yes" />
      </Component>
"@
        $CowComponentRefs += "      <ComponentRef Id=`"$CompId`" />"
    }

    $CowFragment = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="CowFiles" Directory="COWSFOLDER">
$($CowComponents -join "`n")
    </ComponentGroup>
  </Fragment>
</Wix>
"@
    Set-Content -Path $CowFragmentPath -Value $CowFragment

    # Build MSI with WiX v4
    $MsiFile = Join-Path $RidOutput "${PackageName}-${Version}-${rid}.msi"
    New-Item -ItemType Directory -Path $RidOutput -Force | Out-Null

    wix build `
        $WxsSource `
        $CowFragmentPath `
        -bindpath "BinDir=$StagingBin" `
        -d "ProductVersion=$Version" `
        -arch $Platform `
        -o $MsiFile

    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX build failed for $rid (exit code $LASTEXITCODE)"
        exit $LASTEXITCODE
    }

    # Clean up staging
    Remove-Item -Recurse -Force (Join-Path $RidOutput "staging")

    Write-Host "  -> $MsiFile" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=== MSI build complete ===" -ForegroundColor Cyan
