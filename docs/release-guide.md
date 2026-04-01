# Release Guide

This document covers the end-to-end workflow for releasing a new version of **cowsay-dotnet**: bumping the version, building all packages, creating a GitHub release, and publishing to package registries.

For detailed script parameters and usage, see [Build Scripts Reference](build-scripts.md).
For project structure and artifact layout, see [Architecture](architecture.md).

---

## Quick Start

The most common release workflow in ~10 commands:

```powershell
# 1. Update the version number (single source of truth)
#    Edit Directory.Build.props and change <Version>1.0.0</Version> to the new version.

# 2. Commit the version bump
git add Directory.Build.props
git commit -m "Bump version to 1.1.0"

# 3. Tag the release
git tag v1.1.0
git push origin main --tags

# 4. Build all packages for all platforms (from repo root, using pwsh)
pwsh build/package-all.ps1

# 5. Create a GitHub release and upload all artifacts
gh release create v1.1.0 --title "v1.1.0" --generate-notes `
    packaging/linux/deb/**/*.deb `
    packaging/linux/rpm/**/*.rpm `
    packaging/mac/brew/**/*.tar.gz `
    packaging/windows/installer/**/*.msi `
    packaging/windows/choco/**/*.nupkg

# 6. Post-release: publish to package registries (see sections below)
```

> **Important:** Always use `pwsh` (PowerShell 7), not `powershell.exe` (v5.1). The v5.1 shell has incompatibilities with the build scripts.

---

## Table of Contents

- [Version Management](#version-management)
- [Prerequisites](#prerequisites)
- [GitHub Release Workflow](#github-release-workflow)
  - [Step 1: Bump the Version](#step-1-bump-the-version)
  - [Step 2: Build All Packages](#step-2-build-all-packages)
  - [Step 3: Create the Git Tag](#step-3-create-the-git-tag)
  - [Step 4: Create a GitHub Release](#step-4-create-a-github-release)
  - [Step 5: Verify the Release](#step-5-verify-the-release)
- [Post-Release Publishing](#post-release-publishing)
  - [Chocolatey](#chocolatey)
  - [WinGet](#winget)
  - [PowerShell Gallery](#powershell-gallery)
  - [Homebrew Tap](#homebrew-tap)
  - [Snap Store](#snap-store)
- [Troubleshooting](#troubleshooting)

---

## Version Management

The project uses a **single source of truth** for versioning: the `<Version>` element in `Directory.Build.props` at the repository root.

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    ...
  </PropertyGroup>
</Project>
```

Every build and packaging script reads the version from this file:

- **PowerShell scripts** parse it as XML: `([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version`
- **Bash scripts** extract it with: `grep -oP '<Version>\K[^<]+' Directory.Build.props`

When you change the version in `Directory.Build.props`, all scripts automatically pick up the new value. No other files need manual version updates, with one exception:

- **`src/Cowsay.PowerShell/Cowsay.PowerShell.psd1`** contains a `ModuleVersion` field. The `build-psmodule.ps1` script updates this automatically during the build, so you do not need to edit it by hand.

### Versioning Convention

The project follows [Semantic Versioning](https://semver.org/):

- **Major** (`X.0.0`): Breaking changes to CLI behavior, cowfile format, or PowerShell cmdlet signatures
- **Minor** (`0.X.0`): New features (new flags, new cowfiles, new cmdlets)
- **Patch** (`0.0.X`): Bug fixes, documentation, packaging improvements

---

## Prerequisites

### Developer Machine

The build system is designed to run entirely from a **Windows machine with WSL Ubuntu**. You need:

| Component | Purpose | Install |
|-----------|---------|---------|
| **PowerShell 7** (`pwsh`) | All `.ps1` scripts | `winget install Microsoft.PowerShell` |
| **.NET 10 SDK** | `dotnet publish` | `winget install Microsoft.DotNet.SDK.10` |
| **Git** | Tagging and release | `winget install Git.Git` |
| **GitHub CLI** (`gh`) | Creating releases | `winget install GitHub.cli` |
| **WSL Ubuntu 24.04** | Linux package builds | `wsl --install -d Ubuntu` |

### Packaging Tools

Additional tools are required for specific package formats. Run the setup checker to see what's installed and what's missing:

```powershell
pwsh build/install-packaging-tools.ps1
```

This prints a status report and can optionally install missing WSL tools (requires `sudo` password).

| Tool | Platform | Required For | Install Command |
|------|----------|-------------|-----------------|
| `dpkg-deb` | WSL | `.deb` packages | `sudo apt install dpkg` |
| `rpmbuild` | WSL | `.rpm` packages | `sudo apt install rpm` |
| `fpm` | WSL | Arch `.pkg.tar.zst` | `sudo gem install fpm` |
| `snapcraft` | WSL | `.snap` packages | `sudo snap install snapcraft --classic` |
| `wix` | Windows | `.msi` installers | `dotnet tool install --global wix` |
| `choco` | Windows | `.nupkg` packages | [chocolatey.org/install](https://chocolatey.org/install) |
| `wingetcreate` | Windows | WinGet manifests | `winget install wingetcreate` (optional) |

> **Note:** `snapcraft` and `fpm` require additional setup (Ruby/gem for fpm, snap daemon for snapcraft). The `install-packaging-tools.ps1` script handles these dependencies.

---

## GitHub Release Workflow

### Step 1: Bump the Version

Edit `Directory.Build.props` and update the `<Version>` element:

```xml
<Version>1.1.0</Version>
```

Run the tests to make sure everything still passes:

```powershell
dotnet test
```

Commit the version bump:

```powershell
git add Directory.Build.props
git commit -m "Bump version to 1.1.0"
```

### Step 2: Build All Packages

From the repository root, run the orchestrator:

```powershell
pwsh build/package-all.ps1
```

This will:

1. **Publish binaries** for all 9 RIDs (via `build/publish.ps1`)
2. **Build all package formats** — deb, rpm, arch (via FPM), brew tarball, msi, choco, winget manifests, PS module
3. **Print a summary table** showing Built/Skipped/Failed status per format and RID

To build only specific formats or RIDs:

```powershell
# Only deb and rpm for linux-x64
pwsh build/package-all.ps1 -Rid linux-x64 -Formats deb,rpm

# All formats, skip the publish step (binaries already built)
pwsh build/package-all.ps1 -SkipPublish
```

See [Build Scripts Reference](build-scripts.md) for full parameter documentation.

> **Skipped by design:** macOS `.pkg` installers cannot be built on Windows/WSL. They require native macOS tools (`pkgbuild`, `productbuild`). Build these on a Mac using `packaging/mac/pkg/build-pkg.sh`.

### Step 3: Create the Git Tag

Tag the commit and push:

```powershell
git tag v1.1.0
git push origin main --tags
```

### Step 4: Create a GitHub Release

Use the GitHub CLI to create a release and attach all artifacts:

```powershell
# Collect all built artifacts
$debs    = Get-ChildItem packaging/linux/deb/*/*.deb
$rpms    = Get-ChildItem packaging/linux/rpm/*/*.rpm
$archs   = Get-ChildItem packaging/linux/.pkg.tar.zst/*/*.pkg.tar.zst
$tarballs = Get-ChildItem packaging/mac/brew/*/*.tar.gz
$shasums = Get-ChildItem packaging/mac/brew/*/*.sha256
$msis    = Get-ChildItem packaging/windows/installer/*/*.msi
$chocos  = Get-ChildItem packaging/windows/choco/*/*.nupkg

$allFiles = @($debs + $rpms + $archs + $tarballs + $shasums + $msis + $chocos) |
    ForEach-Object { $_.FullName }

# Create the release
gh release create v1.1.0 --title "v1.1.0" --generate-notes @allFiles
```

Alternatively, use a simpler glob approach (bash-style via Git Bash or WSL):

```bash
gh release create v1.1.0 --title "v1.1.0" --generate-notes \
    packaging/linux/deb/*/*.deb \
    packaging/linux/rpm/*/*.rpm \
    packaging/linux/.pkg.tar.zst/*/*.pkg.tar.zst \
    packaging/mac/brew/*/*.tar.gz \
    packaging/mac/brew/*/*.sha256 \
    packaging/windows/installer/*/*.msi \
    packaging/windows/choco/*/*.nupkg
```

### Step 5: Verify the Release

After creating the release:

1. Visit the release page on GitHub and confirm all assets are attached
2. Download one artifact from each platform and test it:
   ```bash
   # Linux (deb)
   sudo dpkg -i cowsay-dotnet_1.1.0_amd64.deb
   cowsay "Release test"

   # Windows (msi) - run as admin
   msiexec /i cowsay-dotnet-1.1.0-win-x64.msi
   cowsay "Release test"
   ```
3. Verify the version string: `cowsay --version` should print `1.1.0`

---

## Post-Release Publishing

After the GitHub release is live with all artifacts attached, publish to each package registry.

### Chocolatey

```powershell
# Find the .nupkg for the primary architecture
$nupkg = Get-ChildItem packaging/windows/choco/win-x64/*.nupkg | Select-Object -First 1

# Push to Chocolatey community repository
choco push $nupkg.FullName --source https://push.chocolatey.org/ --api-key YOUR_API_KEY
```

**First-time setup:**
- Create an account at [community.chocolatey.org](https://community.chocolatey.org/)
- Get your API key from your account page
- Packages go through a moderation review (typically 1-3 days)

**Notes:**
- Chocolatey packages are architecture-specific. Push the `win-x64` package as the primary; you may optionally push other architectures as separate packages (e.g., `cowsay-dotnet.win-arm64`).
- The `.nuspec` and install scripts under `packaging/windows/choco/` are already configured.

### WinGet

The `build-winget.ps1` script generates WinGet manifest YAML files. To submit them:

**Option A: Manual PR** (recommended for first submission)

```powershell
# 1. Fork https://github.com/microsoft/winget-pkgs
# 2. Copy the generated manifests
$version = "1.1.0"
$src  = "packaging/windows/winget/win-x64"
$dest = "path/to/winget-pkgs/manifests/c/cowsay-dotnet/cowsay-dotnet/$version"
mkdir -p $dest
Copy-Item "$src/*.yaml" $dest

# 3. Create a PR to microsoft/winget-pkgs
```

**Option B: Using wingetcreate**

```powershell
# Update and submit in one step (requires GitHub token)
wingetcreate update cowsay-dotnet.cowsay-dotnet `
    --version 1.1.0 `
    --urls "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v1.1.0/cowsay-dotnet-1.1.0-win-x64.msi" `
    --submit --token YOUR_GITHUB_PAT
```

**Important:** Before submitting, ensure the MSI installer URLs in the manifests point to actual GitHub release assets. The `build-winget.ps1` script computes SHA256 hashes from locally built MSIs, but the download URLs assume the standard release URL pattern.

### PowerShell Gallery

```powershell
# Publish the staged module (win-x64 is the typical choice for PSGallery)
pwsh packaging/windows/powershell/build-psmodule.ps1 -Rid win-x64 -Publish -NuGetApiKey YOUR_API_KEY
```

Or publish manually:

```powershell
$modulePath = "packaging/windows/powershell/win-x64/Cowsay.PowerShell"
Publish-Module -Path $modulePath -NuGetApiKey YOUR_API_KEY
```

**First-time setup:**
- Create an account at [powershellgallery.com](https://www.powershellgallery.com/)
- Generate an API key from your account settings
- The module manifest (`Cowsay.PowerShell.psd1`) must pass `Test-ModuleManifest` validation

### Homebrew Tap

Homebrew uses a separate "tap" repository containing your formula. After releasing:

1. **Get the SHA256 hash** from the `.sha256` sidecar file:
   ```bash
   cat packaging/mac/brew/osx-arm64/cowsay-dotnet-1.1.0-osx-arm64.tar.gz.sha256
   ```

2. **Update the formula** in your tap repository (`homebrew-cowsay-dotnet`):
   ```ruby
   class CowsayDotnet < Formula
     version "1.1.0"

     if Hardware::CPU.arm?
       url "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v1.1.0/cowsay-dotnet-1.1.0-osx-arm64.tar.gz"
       sha256 "HASH_FROM_SHA256_FILE"
     else
       url "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v1.1.0/cowsay-dotnet-1.1.0-osx-x64.tar.gz"
       sha256 "HASH_FROM_SHA256_FILE"
     end
   end
   ```

3. **Commit and push** the tap repository:
   ```bash
   cd homebrew-cowsay-dotnet
   git add Formula/cowsay-dotnet.rb
   git commit -m "Update cowsay-dotnet to 1.1.0"
   git push
   ```

**First-time setup:**
- Create a GitHub repository named `homebrew-cowsay-dotnet` (the `homebrew-` prefix is a Homebrew convention)
- Users install via: `brew tap cowsay-dotnet/cowsay-dotnet && brew install cowsay-dotnet`

### Snap Store

```bash
# Log in to the Snap Store (first time only)
snapcraft login

# Upload and release to the stable channel
snapcraft upload packaging/linux/snap/linux-x64/cowsay-dotnet_1.1.0_amd64.snap --release=stable
```

**First-time setup:**
- Create an account at [snapcraft.io](https://snapcraft.io/)
- Register the snap name: `snapcraft register cowsay-dotnet`
- Snap packages must pass automated review; `confinement: strict` packages get published faster

**Architecture-specific uploads:**
```bash
# Upload each architecture separately
snapcraft upload packaging/linux/snap/linux-x64/cowsay-dotnet_1.1.0_amd64.snap --release=stable
snapcraft upload packaging/linux/snap/linux-arm/cowsay-dotnet_1.1.0_armhf.snap --release=stable
snapcraft upload packaging/linux/snap/linux-arm64/cowsay-dotnet_1.1.0_arm64.snap --release=stable
```

---

## Troubleshooting

### WSL `/mnt/c/` Permission Issues

**Problem:** `dpkg-deb` rejects `DEBIAN/control` directories when permissions exceed `0775`. Files on the Windows filesystem mounted in WSL have `777` permissions.

**Solution:** The `wsl-build-helper.sh` script stages all package contents inside `/tmp/` (native Linux filesystem with proper permissions), builds there, then copies results back. If you're running the standalone `build-deb.sh` directly inside WSL, run it from within the Linux filesystem (e.g., `~/projects/cowsay-dotnet/`) rather than from `/mnt/c/`.

### PowerShell 5.1 Incompatibility

**Problem:** Running scripts with `powershell.exe` (v5.1) produces mysterious errors like "positional parameter 'output'".

**Solution:** Always use `pwsh` (PowerShell 7). All scripts are designed for and tested with PowerShell 7.

```powershell
# Correct
pwsh build/package-all.ps1

# Wrong - will fail
powershell build/package-all.ps1
```

### RPM "Duplicate build-ids" Warning

**Problem:** `rpmbuild` emits warnings about duplicate build-ids when building both `cowsay` and `cowthink`.

**Explanation:** This is harmless. Both executables are built from the same source code (`Cowsay.Cowthink` links `Cowsay.Cli\Program.cs`), so they share identical build identifiers. The resulting RPM package is fully functional.

### WiX Duplicate ComponentGroup

**Problem:** `wix build` fails with an error about duplicate `ComponentGroup` definitions for `CowFiles`.

**Solution:** This was caused by an empty `<ComponentGroup Id="CowFiles" />` placeholder in `cowsay-dotnet.wxs` that conflicted with the dynamically-generated `cowfiles.wxs` fragment. The placeholder has been removed. If you encounter this error, verify that `cowsay-dotnet.wxs` does not contain a `CowFiles` ComponentGroup (the `build-installer.ps1` script generates it dynamically).

### PowerShell Array Gotcha

**Problem:** Scripts fail with errors when only a single RID matches (e.g., `.Count` throws on a string).

**Explanation:** When `Sort-Object -Unique` or a filter returns a single item, PowerShell returns a bare string instead of an array. Calling `.Count` on a string throws.

**Solution:** Wrap all such calls in `@(...)` to force array context. This has been fixed in all scripts.

### Arch Linux WSL is Damaged

**Problem:** The Arch Linux WSL distribution on this machine is non-functional.

**Solution:** Do not use it. The `package-all.ps1` script builds Arch `.pkg.tar.zst` packages using FPM in Ubuntu WSL instead of `makepkg`. The native Makefile + PKGBUILD path exists for use on actual Arch Linux systems.

### `shasum` vs `sha256sum`

**Problem:** `shasum -a 256` is macOS-only; it doesn't exist on Linux/WSL.

**Solution:** The `build-brew.sh` script auto-detects the available tool using `command -v sha256sum` and falls back to `shasum -a 256` on macOS.

### Snap Build Requires snapd

**Problem:** `snapcraft` requires the snap daemon (`snapd`), which may not work in all WSL configurations.

**Solution:** `package-all.ps1` will skip snap builds if `snapcraft` is not available. To build snaps, ensure your WSL Ubuntu has systemd enabled (`/etc/wsl.conf` should contain `[boot]\nsystemd=true`) and install snapcraft: `sudo snap install snapcraft --classic`.

---

## See Also

- [Build Scripts Reference](build-scripts.md) -- Detailed parameters and usage for every script
- [Architecture](architecture.md) -- Project structure, artifact layout, and design decisions
- [README](../README.md) -- User-facing installation and usage documentation
