<#
.SYNOPSIS
    One-time setup script: installs all packaging tool prerequisites.

.DESCRIPTION
    Installs or verifies the tools needed by build/package-all.ps1:

    WSL Ubuntu:
      - dpkg-deb   (for .deb)          -- usually pre-installed
      - rpmbuild   (for .rpm)          -- from 'rpm' apt package
      - fpm        (for Arch .pkg.tar.zst) -- Ruby gem
      - snapcraft  (for .snap)         -- snap package
      - tar, sha256sum                 -- usually pre-installed

    Windows:
      - wix        (for .msi)          -- .NET global tool
      - choco      (for Chocolatey)    -- chocolatey.org
      - wingetcreate (for WinGet)      -- optional

    Does NOT install Windows tools automatically (just checks and prints instructions).
    DOES install WSL Ubuntu tools with your permission.

.PARAMETER SkipWsl
    Skip WSL Ubuntu tool installation (just check Windows tools).

.PARAMETER SkipWindows
    Skip Windows tool checks (just install WSL tools).

.EXAMPLE
    pwsh -File build/install-packaging-tools.ps1
    pwsh -File build/install-packaging-tools.ps1 -SkipWindows
#>

[CmdletBinding()]
param(
    [switch]$SkipWsl,
    [switch]$SkipWindows
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Tool, [bool]$Found, [string]$Detail = "")
    if ($Found) {
        Write-Host "  [OK]   $Tool" -ForegroundColor Green -NoNewline
    } else {
        Write-Host "  [MISS] $Tool" -ForegroundColor Red -NoNewline
    }
    if ($Detail) { Write-Host " -- $Detail" -ForegroundColor DarkGray }
    else { Write-Host "" }
}

# ═══════════════════════════════════════════════════════════════════════════════
# WSL Ubuntu tools
# ═══════════════════════════════════════════════════════════════════════════════
if (-not $SkipWsl) {
    Write-Host ""
    Write-Host "=== WSL Ubuntu Tools ===" -ForegroundColor Cyan
    Write-Host ""

    # Check WSL is available — use a direct command test instead of --list
    # because wsl --list outputs UTF-16 which PowerShell struggles with
    $wslAvailable = $false
    try {
        $wslTest = wsl -e bash -c "echo ok" 2>$null
        if ($wslTest -and $wslTest.Trim() -eq "ok") {
            $wslAvailable = $true
            Write-Status "WSL Ubuntu" $true
        } else {
            Write-Status "WSL Ubuntu" $false "WSL command test failed"
        }
    } catch {
        Write-Status "WSL Ubuntu" $false "WSL not available"
    }

    if ($wslAvailable) {
        # Check existing tools
        $dpkgFound   = (wsl -e bash -c "command -v dpkg-deb" 2>$null)
        $rpmFound    = (wsl -e bash -c "command -v rpmbuild" 2>$null)
        $fpmFound    = (wsl -e bash -c "command -v fpm" 2>$null)
        $snapFound   = (wsl -e bash -c "command -v snapcraft" 2>$null)
        $tarFound    = (wsl -e bash -c "command -v tar" 2>$null)
        $sha256Found = (wsl -e bash -c "command -v sha256sum" 2>$null)

        Write-Status "dpkg-deb"   ([bool]$dpkgFound)   "Required for .deb"
        Write-Status "rpmbuild"   ([bool]$rpmFound)     "Required for .rpm"
        Write-Status "fpm"        ([bool]$fpmFound)     "Required for Arch .pkg.tar.zst"
        Write-Status "snapcraft"  ([bool]$snapFound)    "Required for .snap"
        Write-Status "tar"        ([bool]$tarFound)     "Required for Homebrew tarballs"
        Write-Status "sha256sum"  ([bool]$sha256Found)  "Required for Homebrew tarballs"

        # Collect what needs installing
        $aptPackages = @()
        if (-not $rpmFound)  { $aptPackages += "rpm" }

        $needRuby = (-not $fpmFound)
        if ($needRuby) {
            # Check if Ruby is already installed
            $rubyFound = (wsl -e bash -c "command -v ruby" 2>$null)
            if (-not $rubyFound) {
                $aptPackages += "ruby"
                $aptPackages += "ruby-dev"
                $aptPackages += "gcc"
                $aptPackages += "make"
            }
        }

        $needSnapcraft = (-not $snapFound)

        $needsInstall = ($aptPackages.Count -gt 0) -or $needRuby -or $needSnapcraft

        if ($needsInstall) {
            Write-Host ""
            Write-Host "Some tools need to be installed in WSL Ubuntu." -ForegroundColor Yellow
            Write-Host "The following commands will be run:" -ForegroundColor Yellow
            Write-Host ""

            if ($aptPackages.Count -gt 0) {
                Write-Host "  sudo apt update && sudo apt install -y $($aptPackages -join ' ')" -ForegroundColor DarkGray
            }
            if ($needRuby) {
                Write-Host "  sudo gem install fpm" -ForegroundColor DarkGray
            }
            if ($needSnapcraft) {
                Write-Host "  sudo snap install snapcraft --classic" -ForegroundColor DarkGray
            }

            Write-Host ""
            $confirm = Read-Host "Proceed? (y/N)"
            if ($confirm -eq "y" -or $confirm -eq "Y") {
                if ($aptPackages.Count -gt 0) {
                    Write-Host "Installing apt packages: $($aptPackages -join ', ')..." -ForegroundColor Yellow
                    wsl -e bash -c "sudo apt update && sudo apt install -y $($aptPackages -join ' ')"
                    if ($LASTEXITCODE -ne 0) {
                        Write-Error "apt install failed (exit code $LASTEXITCODE)"
                    }
                }

                if ($needRuby) {
                    Write-Host "Installing FPM gem..." -ForegroundColor Yellow
                    wsl -e bash -c "sudo gem install fpm"
                    if ($LASTEXITCODE -ne 0) {
                        Write-Error "gem install fpm failed (exit code $LASTEXITCODE)"
                    }
                }

                if ($needSnapcraft) {
                    Write-Host "Installing snapcraft..." -ForegroundColor Yellow
                    wsl -e bash -c "sudo snap install snapcraft --classic"
                    if ($LASTEXITCODE -ne 0) {
                        Write-Warning "snapcraft install failed. Snap packages will be skipped."
                    }
                }

                Write-Host ""
                Write-Host "WSL tool installation complete!" -ForegroundColor Green
            } else {
                Write-Host "Skipped. You can install manually later." -ForegroundColor DarkGray
            }
        } else {
            Write-Host ""
            Write-Host "All WSL Ubuntu tools are already installed." -ForegroundColor Green
        }
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# Windows tools
# ═══════════════════════════════════════════════════════════════════════════════
if (-not $SkipWindows) {
    Write-Host ""
    Write-Host "=== Windows Tools ===" -ForegroundColor Cyan
    Write-Host ""

    # WiX v4
    $wixFound = $false
    try {
        $wixList = dotnet tool list -g 2>$null
        if ($wixList -match "wix") { $wixFound = $true }
    } catch {}
    Write-Status "wix" $wixFound "Required for .msi (dotnet tool install --global wix)"

    # Chocolatey
    $chocoFound = $false
    try {
        $chocoVer = choco --version 2>$null
        if ($chocoVer) { $chocoFound = $true }
    } catch {}
    Write-Status "choco" $chocoFound "Required for Chocolatey .nupkg (https://chocolatey.org/install)"

    # wingetcreate
    $wingetCreateFound = $false
    try {
        $wcVer = wingetcreate --version 2>$null
        if ($wcVer) { $wingetCreateFound = $true }
    } catch {}
    Write-Status "wingetcreate" $wingetCreateFound "Optional for WinGet (winget install wingetcreate)"

    if (-not $wixFound -or -not $chocoFound) {
        Write-Host ""
        Write-Host "Missing Windows tools must be installed manually:" -ForegroundColor Yellow
        if (-not $wixFound) {
            Write-Host "  dotnet tool install --global wix" -ForegroundColor DarkGray
        }
        if (-not $chocoFound) {
            Write-Host "  See https://chocolatey.org/install" -ForegroundColor DarkGray
        }
        if (-not $wingetCreateFound) {
            Write-Host "  winget install wingetcreate  (optional)" -ForegroundColor DarkGray
        }
    } else {
        Write-Host ""
        Write-Host "All Windows tools are installed." -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Setup check complete ===" -ForegroundColor Cyan
