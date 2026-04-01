<#
.SYNOPSIS
    Master cross-platform packaging script for cowsay-dotnet.
    Builds all package formats from a single Windows machine using WSL Ubuntu.

.DESCRIPTION
    Orchestrates the creation of packages for every supported platform by:
      - Running Windows packaging natively (MSI, Chocolatey, WinGet, PS Module)
      - Running Linux packaging via WSL Ubuntu (deb, rpm, snap, Arch, Homebrew tarballs)
      - Skipping macOS .pkg (requires macOS — prints a message)

    Reuses the existing per-format build scripts under packaging/.

    Prerequisites: Run build/install-packaging-tools.ps1 first for one-time setup.

.PARAMETER Rid
    Build for a single RID only (e.g., "linux-x64", "win-x64").
    If omitted, builds all applicable RIDs for each selected format.

.PARAMETER Formats
    Comma-separated list of formats to build. Default: all.
    Valid values: deb, rpm, snap, arch, brew-tarball, msi, choco, winget, psmodule, all

.PARAMETER SkipPublish
    Skip the dotnet publish step (assumes binaries already exist in build/output/).

.EXAMPLE
    pwsh -Command "& './build/package-all.ps1'"
    # Build everything for all RIDs.

.EXAMPLE
    pwsh -Command "& './build/package-all.ps1' -Rid linux-x64 -Formats deb,rpm"
    # Build .deb and .rpm for linux-x64 only.

.EXAMPLE
    pwsh -Command "& './build/package-all.ps1' -Formats msi -Rid win-x64 -SkipPublish"
    # Build MSI for win-x64, skip dotnet publish.
#>

[CmdletBinding()]
param(
    [string]$Rid,

    [string[]]$Formats = @("all"),

    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ═══════════════════════════════════════════════════════════════════════════════
# Constants
# ═══════════════════════════════════════════════════════════════════════════════
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BuildOutput = Join-Path $RepoRoot "build" "output"

$LinuxRids   = @("linux-x64", "linux-arm", "linux-arm64")
$MacRids     = @("osx-x64", "osx-arm64")
$WindowsRids = @("win-x64", "win-x86", "win-arm64")
$AllRids     = $LinuxRids + $MacRids + $WindowsRids

$ValidFormats = @("deb", "rpm", "snap", "arch", "brew-tarball", "msi", "choco", "winget", "psmodule")

$LinuxFormats  = @("deb", "rpm", "snap", "arch")
$MacFormats    = @("brew-tarball")
$WinFormats    = @("msi", "choco", "winget", "psmodule")

# Read version
[xml]$Props = Get-Content (Join-Path $RepoRoot "Directory.Build.props")
$Version = $Props.Project.PropertyGroup.Version

# ═══════════════════════════════════════════════════════════════════════════════
# Resolve formats
# ═══════════════════════════════════════════════════════════════════════════════
if ("all" -in $Formats) {
    $SelectedFormats = $ValidFormats
} else {
    $SelectedFormats = @()
    foreach ($f in $Formats) {
        $f = $f.Trim().ToLower()
        if ($f -notin $ValidFormats) {
            Write-Error "Unknown format '$f'. Valid: $($ValidFormats -join ', '), all"
            exit 1
        }
        $SelectedFormats += $f
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# Resolve RIDs per format
# ═══════════════════════════════════════════════════════════════════════════════
function Get-RidsForFormat {
    param([string]$Format)

    if ($Format -in $LinuxFormats)  { $pool = $LinuxRids }
    elseif ($Format -in $MacFormats) { $pool = $MacRids }
    elseif ($Format -in $WinFormats) { $pool = $WindowsRids }
    else { return @() }

    if ($Rid) {
        if ($Rid -in $pool) { return @($Rid) }
        else { return @() }  # RID doesn't apply to this format
    }
    return $pool
}

# ═══════════════════════════════════════════════════════════════════════════════
# WSL helper — convert Windows path to WSL path
# ═══════════════════════════════════════════════════════════════════════════════
function ConvertTo-WslPath {
    param([string]$WinPath)
    $result = wsl -e wslpath "$WinPath" 2>$null
    if (-not $result) {
        Write-Error "Failed to convert path to WSL: $WinPath"
        exit 1
    }
    return $result.Trim()
}

# ═══════════════════════════════════════════════════════════════════════════════
# Tool availability checks
# ═══════════════════════════════════════════════════════════════════════════════
$WslAvailable = $false
try {
    # Use a direct command test — wsl --list outputs UTF-16 which PowerShell struggles with
    $wslTest = wsl -e bash -c "echo ok" 2>$null
    if ($wslTest -and $wslTest.Trim() -eq "ok") { $WslAvailable = $true }
} catch {}

function Test-WslTool {
    param([string]$Tool)
    if (-not $WslAvailable) { return $false }
    $result = wsl -e bash -c "command -v $Tool" 2>$null
    return [bool]$result
}

function Test-WindowsTool {
    param([string]$Tool)
    try {
        $result = Get-Command $Tool -ErrorAction SilentlyContinue
        return [bool]$result
    } catch { return $false }
}

# ═══════════════════════════════════════════════════════════════════════════════
# Results tracking
# ═══════════════════════════════════════════════════════════════════════════════
$Results = [System.Collections.ArrayList]::new()

function Add-Result {
    param(
        [string]$Format,
        [string]$RidValue,
        [string]$Status,   # Built, Skipped, Failed
        [string]$Detail = ""
    )
    [void]$Results.Add([PSCustomObject]@{
        Format = $Format
        RID    = $RidValue
        Status = $Status
        Detail = $Detail
    })
}

# ═══════════════════════════════════════════════════════════════════════════════
# Banner
# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         cowsay-dotnet  package-all  v$Version                   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Formats : $($SelectedFormats -join ', ')" -ForegroundColor White
Write-Host "  RID     : $(if ($Rid) { $Rid } else { 'all (auto per format)' })" -ForegroundColor White
Write-Host "  WSL     : $(if ($WslAvailable) { 'Available' } else { 'NOT available' })" -ForegroundColor $(if ($WslAvailable) { 'Green' } else { 'Red' })
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════════
# Step 1: Publish binaries
# ═══════════════════════════════════════════════════════════════════════════════
if (-not $SkipPublish) {
    Write-Host "--- Step 1: Publishing binaries ---" -ForegroundColor Yellow

    # Determine which RIDs we need
    $NeededRids = @()
    foreach ($fmt in $SelectedFormats) {
        $NeededRids += Get-RidsForFormat $fmt
    }
    $NeededRids = @($NeededRids | Sort-Object -Unique)

    if ($NeededRids.Count -eq 0) {
        Write-Host "  No applicable RIDs for selected formats. Nothing to publish." -ForegroundColor DarkGray
    } else {
        foreach ($r in $NeededRids) {
            $cowsayBin = Join-Path $BuildOutput "cowsay" $r
            if (Test-Path $cowsayBin) {
                Write-Host "  [SKIP] $r -- binaries already exist" -ForegroundColor DarkGray
            } else {
                Write-Host "  [BUILD] Publishing for $r..." -ForegroundColor Yellow
                $publishScript = Join-Path $RepoRoot "build" "publish.ps1"
                & pwsh -NoProfile -Command "& '$publishScript' -Rid $r -Projects cli,cowthink"
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "Publish failed for $r"
                    exit $LASTEXITCODE
                }
            }
        }
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# Step 2: Build packages
# ═══════════════════════════════════════════════════════════════════════════════
Write-Host "--- Step 2: Building packages ---" -ForegroundColor Yellow
Write-Host ""

$WslRepoRoot = $null
if ($WslAvailable) {
    $WslRepoRoot = ConvertTo-WslPath $RepoRoot
}

# ─── DEB ────────────────────────────────────────────────────────────────────
if ("deb" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "deb")
    if ($rids.Count -eq 0) {
        Add-Result "deb" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not $WslAvailable) {
        foreach ($r in $rids) { Add-Result "deb" $r "Skipped" "WSL not available" }
    } elseif (-not (Test-WslTool "dpkg-deb")) {
        foreach ($r in $rids) { Add-Result "deb" $r "Skipped" "dpkg-deb not found in WSL" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[deb] Building for $r..." -ForegroundColor Magenta
            $helper = "$WslRepoRoot/build/wsl-build-helper.sh"
            wsl -e bash -c "chmod +x '$helper' && '$helper' deb '$r' '$WslRepoRoot'"
            if ($LASTEXITCODE -eq 0) {
                $debDir = Join-Path $RepoRoot "packaging" "linux" "deb" $r
                $debFile = Get-ChildItem -Path $debDir -Filter "*.deb" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "deb" $r "Built" $(if ($debFile) { $debFile.FullName } else { $debDir })
            } else {
                Add-Result "deb" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── RPM ────────────────────────────────────────────────────────────────────
if ("rpm" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "rpm")
    if ($rids.Count -eq 0) {
        Add-Result "rpm" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not $WslAvailable) {
        foreach ($r in $rids) { Add-Result "rpm" $r "Skipped" "WSL not available" }
    } elseif (-not (Test-WslTool "rpmbuild")) {
        foreach ($r in $rids) { Add-Result "rpm" $r "Skipped" "rpmbuild not found in WSL (run install-packaging-tools.ps1)" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[rpm] Building for $r..." -ForegroundColor Magenta
            $helper = "$WslRepoRoot/build/wsl-build-helper.sh"
            wsl -e bash -c "chmod +x '$helper' && '$helper' rpm '$r' '$WslRepoRoot'"
            if ($LASTEXITCODE -eq 0) {
                $rpmDir = Join-Path $RepoRoot "packaging" "linux" "rpm" $r
                $rpmFile = Get-ChildItem -Path $rpmDir -Filter "*.rpm" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "rpm" $r "Built" $(if ($rpmFile) { $rpmFile.FullName } else { $rpmDir })
            } else {
                Add-Result "rpm" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── SNAP ───────────────────────────────────────────────────────────────────
if ("snap" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "snap")
    if ($rids.Count -eq 0) {
        Add-Result "snap" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not $WslAvailable) {
        foreach ($r in $rids) { Add-Result "snap" $r "Skipped" "WSL not available" }
    } elseif (-not (Test-WslTool "snapcraft")) {
        foreach ($r in $rids) { Add-Result "snap" $r "Skipped" "snapcraft not found in WSL (run install-packaging-tools.ps1)" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[snap] Building for $r..." -ForegroundColor Magenta
            $script = "$WslRepoRoot/packaging/linux/snap/build-snap.sh"
            wsl -e bash -c "chmod +x '$script' && '$script' '$r'"
            if ($LASTEXITCODE -eq 0) {
                $snapDir = Join-Path $RepoRoot "packaging" "linux" "snap" $r
                $snapFile = Get-ChildItem -Path $snapDir -Filter "*.snap" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "snap" $r "Built" $(if ($snapFile) { $snapFile.FullName } else { $snapDir })
            } else {
                Add-Result "snap" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── ARCH (.pkg.tar.zst via FPM) ───────────────────────────────────────────
if ("arch" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "arch")

    # RID to Arch architecture mapping
    $RidToArchArch = @{
        "linux-x64"   = "x86_64"
        "linux-arm"   = "armv7h"
        "linux-arm64" = "aarch64"
    }

    if ($rids.Count -eq 0) {
        Add-Result "arch" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not $WslAvailable) {
        foreach ($r in $rids) { Add-Result "arch" $r "Skipped" "WSL not available" }
    } elseif (-not (Test-WslTool "fpm")) {
        foreach ($r in $rids) { Add-Result "arch" $r "Skipped" "fpm not found in WSL (run install-packaging-tools.ps1)" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[arch] Building .pkg.tar.zst for $r via FPM..." -ForegroundColor Magenta
            $arch = $RidToArchArch[$r]
            $archOutput = Join-Path $RepoRoot "packaging" "linux" ".pkg.tar.zst" $r
            $wslArchOutput = ConvertTo-WslPath $archOutput

            # Ensure output directory exists
            if (-not (Test-Path $archOutput)) {
                New-Item -ItemType Directory -Path $archOutput -Force | Out-Null
            }

            $wslBuildOutput = ConvertTo-WslPath $BuildOutput
            $wslAssets = ConvertTo-WslPath (Join-Path $RepoRoot "assets" "cows")
            $wslLicense = ConvertTo-WslPath (Join-Path $RepoRoot "LICENSE")

            # Build using FPM: create a pacman package from directory contents
            $fpmCmd = @(
                "set -e"
                ""
                "COWSAY_BIN='$wslBuildOutput/cowsay/$r'"
                "COWTHINK_BIN='$wslBuildOutput/cowthink/$r'"
                "OUTPUT_DIR='$wslArchOutput'"
                "STAGING=`$(mktemp -d)"
                ""
                "# Create directory structure"
                "mkdir -p `"`$STAGING/usr/bin`""
                "mkdir -p `"`$STAGING/usr/share/cowsay-dotnet/cows`""
                "mkdir -p `"`$STAGING/usr/share/licenses/cowsay-dotnet`""
                ""
                "# Copy binaries"
                "cp `"`$COWSAY_BIN/cowsay`" `"`$STAGING/usr/bin/cowsay`""
                "chmod 755 `"`$STAGING/usr/bin/cowsay`""
                ""
                "if [ -f `"`$COWTHINK_BIN/cowthink`" ]; then"
                "    cp `"`$COWTHINK_BIN/cowthink`" `"`$STAGING/usr/bin/cowthink`""
                "    chmod 755 `"`$STAGING/usr/bin/cowthink`""
                "fi"
                ""
                "# Copy cow files"
                "if [ -d `"`$COWSAY_BIN/assets/cows`" ]; then"
                "    cp -r `"`$COWSAY_BIN/assets/cows/`"* `"`$STAGING/usr/share/cowsay-dotnet/cows/`""
                "else"
                "    cp -r '$wslAssets/'* `"`$STAGING/usr/share/cowsay-dotnet/cows/`""
                "fi"
                ""
                "# Copy license"
                "if [ -f '$wslLicense' ]; then"
                "    cp '$wslLicense' `"`$STAGING/usr/share/licenses/cowsay-dotnet/LICENSE`""
                "fi"
                ""
                "# Build with FPM"
                "fpm -s dir -t pacman \"
                "    -n cowsay-dotnet \"
                "    -v '$Version' \"
                "    -a '$arch' \"
                "    --description 'A .NET reimplementation of the classic cowsay/cowthink utility' \"
                "    --url 'https://github.com/cowsay-dotnet/cowsay-dotnet' \"
                "    --license 'GPL-3.0-or-later' \"
                "    --maintainer 'cowsay-dotnet contributors' \"
                "    -p `"`$OUTPUT_DIR/cowsay-dotnet-${Version}-1-${arch}.pkg.tar.zst`" \"
                "    -C `"`$STAGING`" \"
                "    ."
                ""
                "# Clean up"
                "rm -rf `"`$STAGING`""
            ) -join "`n"

            wsl -e bash -c $fpmCmd
            if ($LASTEXITCODE -eq 0) {
                $pkgFile = Get-ChildItem -Path $archOutput -Filter "*.pkg.tar.zst" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "arch" $r "Built" $(if ($pkgFile) { $pkgFile.FullName } else { $archOutput })
            } else {
                Add-Result "arch" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── BREW TARBALL ───────────────────────────────────────────────────────────
if ("brew-tarball" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "brew-tarball")
    if ($rids.Count -eq 0) {
        Add-Result "brew-tarball" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not $WslAvailable) {
        foreach ($r in $rids) { Add-Result "brew-tarball" $r "Skipped" "WSL not available" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[brew-tarball] Building for $r..." -ForegroundColor Magenta
            $script = "$WslRepoRoot/packaging/mac/brew/build-brew.sh"
            wsl -e bash -c "chmod +x '$script' && '$script' '$r'"
            if ($LASTEXITCODE -eq 0) {
                $brewDir = Join-Path $RepoRoot "packaging" "mac" "brew" $r
                $tarFile = Get-ChildItem -Path $brewDir -Filter "*.tar.gz" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "brew-tarball" $r "Built" $(if ($tarFile) { $tarFile.FullName } else { $brewDir })
            } else {
                Add-Result "brew-tarball" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── MSI ────────────────────────────────────────────────────────────────────
if ("msi" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "msi")
    if ($rids.Count -eq 0) {
        Add-Result "msi" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not (Test-WindowsTool "wix")) {
        # Also check as dotnet tool
        $wixFound = $false
        try {
            $wixList = dotnet tool list -g 2>$null
            if ($wixList -match "wix") { $wixFound = $true }
        } catch {}
        if (-not $wixFound) {
            foreach ($r in $rids) { Add-Result "msi" $r "Skipped" "WiX not found (dotnet tool install --global wix)" }
        }
    } else { $wixFound = $true }

    if (("msi" -in $SelectedFormats) -and ($rids.Count -gt 0) -and $wixFound) {
        foreach ($r in $rids) {
            Write-Host "[msi] Building for $r..." -ForegroundColor Magenta
            $script = Join-Path $RepoRoot "packaging" "windows" "installer" "build-installer.ps1"
            & pwsh -NoProfile -Command "& '$script' -Rid $r"
            if ($LASTEXITCODE -eq 0) {
                $msiDir = Join-Path $RepoRoot "packaging" "windows" "installer" $r
                $msiFile = Get-ChildItem -Path $msiDir -Filter "*.msi" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "msi" $r "Built" $(if ($msiFile) { $msiFile.FullName } else { $msiDir })
            } else {
                Add-Result "msi" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── CHOCOLATEY ─────────────────────────────────────────────────────────────
if ("choco" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "choco")
    if ($rids.Count -eq 0) {
        Add-Result "choco" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } elseif (-not (Test-WindowsTool "choco")) {
        foreach ($r in $rids) { Add-Result "choco" $r "Skipped" "choco not found (https://chocolatey.org/install)" }
    } else {
        foreach ($r in $rids) {
            Write-Host "[choco] Building for $r..." -ForegroundColor Magenta
            $script = Join-Path $RepoRoot "packaging" "windows" "choco" "build-choco.ps1"
            & pwsh -NoProfile -Command "& '$script' -Rid $r"
            if ($LASTEXITCODE -eq 0) {
                $chocoDir = Join-Path $RepoRoot "packaging" "windows" "choco" $r
                $nupkgFile = Get-ChildItem -Path $chocoDir -Filter "*.nupkg" -ErrorAction SilentlyContinue | Select-Object -First 1
                Add-Result "choco" $r "Built" $(if ($nupkgFile) { $nupkgFile.FullName } else { $chocoDir })
            } else {
                Add-Result "choco" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── WINGET ─────────────────────────────────────────────────────────────────
if ("winget" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "winget")
    if ($rids.Count -eq 0) {
        Add-Result "winget" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } else {
        foreach ($r in $rids) {
            Write-Host "[winget] Generating manifests for $r..." -ForegroundColor Magenta
            $script = Join-Path $RepoRoot "packaging" "windows" "winget" "build-winget.ps1"
            & pwsh -NoProfile -Command "& '$script' -Rid $r"
            if ($LASTEXITCODE -eq 0) {
                $wingetDir = Join-Path $RepoRoot "packaging" "windows" "winget" $r
                Add-Result "winget" $r "Built" $wingetDir
            } else {
                Add-Result "winget" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── POWERSHELL MODULE ──────────────────────────────────────────────────────
if ("psmodule" -in $SelectedFormats) {
    $rids = @(Get-RidsForFormat "psmodule")
    if ($rids.Count -eq 0) {
        Add-Result "psmodule" $(if ($Rid) { $Rid } else { "-" }) "Skipped" "RID not applicable"
    } else {
        foreach ($r in $rids) {
            Write-Host "[psmodule] Staging for $r..." -ForegroundColor Magenta
            $script = Join-Path $RepoRoot "packaging" "windows" "powershell" "build-psmodule.ps1"
            & pwsh -NoProfile -Command "& '$script' -Rid $r"
            if ($LASTEXITCODE -eq 0) {
                $psDir = Join-Path $RepoRoot "packaging" "windows" "powershell" $r
                Add-Result "psmodule" $r "Built" $psDir
            } else {
                Add-Result "psmodule" $r "Failed" "Exit code $LASTEXITCODE"
            }
            Write-Host ""
        }
    }
}

# ─── macOS .pkg (always skipped) ───────────────────────────────────────────
# Inform the user if they selected 'all' or if macOS RIDs were involved
if ("all" -in $Formats -or $Rid -in $MacRids) {
    Write-Host "[macos-pkg] SKIPPED -- requires macOS (pkgbuild/productbuild)" -ForegroundColor DarkGray
    Write-Host "  Use packaging/mac/pkg/build-pkg.sh on a Mac instead." -ForegroundColor DarkGray
    Write-Host ""
}

# ═══════════════════════════════════════════════════════════════════════════════
# Step 3: Summary
# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                      Build Summary                           ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$builtCount   = @($Results | Where-Object { $_.Status -eq "Built" }).Count
$skippedCount = @($Results | Where-Object { $_.Status -eq "Skipped" }).Count
$failedCount  = @($Results | Where-Object { $_.Status -eq "Failed" }).Count

# Print table header
Write-Host ("{0,-15} {1,-15} {2,-10} {3}" -f "FORMAT", "RID", "STATUS", "DETAIL") -ForegroundColor White
Write-Host ("{0,-15} {1,-15} {2,-10} {3}" -f "------", "---", "------", "------") -ForegroundColor DarkGray

foreach ($r in $Results) {
    $color = switch ($r.Status) {
        "Built"   { "Green" }
        "Skipped" { "DarkGray" }
        "Failed"  { "Red" }
        default   { "White" }
    }
    Write-Host ("{0,-15} {1,-15} {2,-10} {3}" -f $r.Format, $r.RID, $r.Status, $r.Detail) -ForegroundColor $color
}

Write-Host ""
Write-Host "  Built: $builtCount  |  Skipped: $skippedCount  |  Failed: $failedCount" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } elseif ($builtCount -gt 0) { "Green" } else { "Yellow" })
Write-Host ""

if ($failedCount -gt 0) {
    exit 1
}
