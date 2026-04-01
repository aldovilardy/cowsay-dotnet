# Build Scripts Reference

Detailed documentation for every build and packaging script in the cowsay-dotnet project.

For the release workflow that ties these scripts together, see [Release Guide](release-guide.md).
For project structure and artifact layout, see [Architecture](architecture.md).

---

## Table of Contents

**Core Build Scripts**
- [publish.ps1](#publishps1) -- Compile and publish binaries for all RIDs
- [package-all.ps1](#package-allps1) -- Orchestrate all packaging from Windows
- [install-packaging-tools.ps1](#install-packaging-toolsps1) -- Check and install prerequisites
- [wsl-build-helper.sh](#wsl-build-helpersh) -- WSL staging helper for deb/rpm

**Linux Packaging**
- [build-deb.sh](#build-debsh) -- Debian/Ubuntu .deb packages
- [build-rpm.sh](#build-rpmsh) -- Red Hat/Fedora .rpm packages
- [build-snap.sh](#build-snapsh) -- Snap packages
- [Makefile (Arch)](#makefile-arch) -- Arch Linux .pkg.tar.zst packages

**macOS Packaging**
- [build-brew.sh](#build-brewsh) -- Homebrew tarballs
- [build-pkg.sh](#build-pkgsh) -- macOS .pkg installers

**Windows Packaging**
- [build-installer.ps1](#build-installerps1) -- WiX MSI installers
- [build-choco.ps1](#build-chocops1) -- Chocolatey .nupkg packages
- [build-winget.ps1](#build-wingetps1) -- WinGet manifest YAML files
- [build-psmodule.ps1](#build-psmoduleps1) -- PowerShell Gallery module

---

## Core Build Scripts

### publish.ps1

**Path:** `build/publish.ps1`

Compiles and publishes self-contained, single-file binaries for all projects and RIDs. This is the first step in any packaging workflow.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all 9 RIDs)_ | Single RID to build (e.g., `linux-x64`) |
| `-Projects` | string[] | `all` | Which projects to publish: `all`, `cli`, `cowthink`, `powershell` |
| `-Configuration` | string | `Release` | Build configuration (`Debug` or `Release`) |
| `-OutputRoot` | string | `build/output/` | Root directory for published binaries |

#### Prerequisites

- .NET 10 SDK (`dotnet` on PATH)
- `Directory.Build.props` at repo root

#### RIDs

When `-Rid` is omitted, all 9 RIDs are built:

| Platform | RIDs |
|----------|------|
| Windows | `win-x64`, `win-x86`, `win-arm`, `win-arm64` |
| macOS | `osx-x64`, `osx-arm64` |
| Linux | `linux-x64`, `linux-arm`, `linux-arm64` |

#### Usage

```powershell
# Publish all projects for all RIDs
pwsh build/publish.ps1

# Publish only cowsay CLI for linux-x64
pwsh build/publish.ps1 -Rid linux-x64 -Projects cli

# Publish cowsay + cowthink for all Windows RIDs
pwsh build/publish.ps1 -Projects cli,cowthink -Rid win-x64

# Publish to a custom output directory
pwsh build/publish.ps1 -OutputRoot ./my-output/
```

#### Output

```
build/output/
  cowsay/
    win-x64/        # cowsay.exe + cows/
    linux-x64/      # cowsay + cows/
    osx-arm64/      # cowsay + cows/
    ...
  cowthink/
    win-x64/        # cowthink.exe + cows/
    ...
  powershell/
    win-x64/        # Cowsay.PowerShell.dll + deps + cows/
    ...
```

#### Notes

- CLI and cowthink projects are published as **single-file** executables (`PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, `--self-contained true`)
- The PowerShell module is **not** single-file (it must be loadable by `Import-Module`)
- Cow files from `assets/cows/` are copied into each output directory automatically
- Previous output for a given RID is cleaned before republishing
- Version is read from `Directory.Build.props` and passed as `-p:Version=`

---

### package-all.ps1

**Path:** `build/package-all.ps1`

The master orchestrator. Builds all package formats for all platforms from a single Windows machine, using WSL Ubuntu for Linux formats.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all applicable)_ | Single RID to build |
| `-Formats` | string[] | `@("all")` | Which formats to build (see table below) |
| `-SkipPublish` | switch | `$false` | Skip the `publish.ps1` step (use pre-built binaries) |

#### Format Options

| Value | Description |
|-------|-------------|
| `all` | Build everything (default) |
| `deb` | Debian .deb packages |
| `rpm` | Red Hat .rpm packages |
| `snap` | Snap packages |
| `arch` | Arch Linux .pkg.tar.zst (via FPM) |
| `brew-tarball` | Homebrew tarballs + SHA256 |
| `msi` | Windows MSI installers |
| `choco` | Chocolatey .nupkg |
| `winget` | WinGet manifest YAML |
| `psmodule` | PowerShell Gallery module |

#### Prerequisites

- PowerShell 7 (`pwsh`)
- WSL Ubuntu (for Linux/brew formats)
- Format-specific tools (see [Prerequisites](release-guide.md#prerequisites))

#### Usage

```powershell
# Build everything
pwsh build/package-all.ps1

# Only Linux packages
pwsh build/package-all.ps1 -Formats deb,rpm

# Only win-x64 MSI, skip publish step
pwsh build/package-all.ps1 -Rid win-x64 -Formats msi -SkipPublish

# All formats for a single RID
pwsh build/package-all.ps1 -Rid linux-x64
```

#### Behavior

1. **Publish phase** -- Calls `build/publish.ps1` for the required RIDs (skipped with `-SkipPublish` or if binaries already exist)
2. **Package phase** -- For each format × RID combination:
   - Linux formats (`deb`, `rpm`): Dispatched via WSL using `wsl-build-helper.sh`
   - `snap`: Dispatched via WSL calling `build-snap.sh` directly
   - `arch`: Built inline using FPM via a bash heredoc into WSL
   - `brew-tarball`: Dispatched via WSL calling `build-brew.sh`
   - Windows formats (`msi`, `choco`, `winget`, `psmodule`): Called directly as PowerShell scripts
3. **Summary phase** -- Prints a color-coded table (green=Built, yellow=Skipped, red=Failed)

#### Skipped Formats

- **macOS `.pkg`** is always skipped (requires native macOS `pkgbuild`/`productbuild`)
- Any format whose tool is not installed is skipped with a warning
- The script exits with code `1` if any format failed

---

### install-packaging-tools.ps1

**Path:** `build/install-packaging-tools.ps1`

Checks for all required packaging tools and optionally installs missing ones.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-SkipWsl` | switch | `$false` | Skip WSL tool checks |
| `-SkipWindows` | switch | `$false` | Skip Windows tool checks |

#### Prerequisites

- WSL Ubuntu (unless `-SkipWsl`)
- `sudo` access in WSL (for installing missing tools)

#### Usage

```powershell
# Full check -- WSL + Windows
pwsh build/install-packaging-tools.ps1

# Only check Windows tools
pwsh build/install-packaging-tools.ps1 -SkipWsl

# Only check WSL tools
pwsh build/install-packaging-tools.ps1 -SkipWindows
```

#### Tool Status Report

The script prints a table like:

```
WSL Tools:
  [OK]   dpkg-deb
  [OK]   rpmbuild
  [MISS] fpm
  [MISS] snapcraft
  [OK]   tar
  [OK]   sha256sum

Windows Tools:
  [OK]   wix (v4.0.5)
  [OK]   choco (v2.3.0)
  [MISS] wingetcreate (optional)
```

For missing WSL tools, it prompts `Install missing WSL tools? [y/N]` and runs the appropriate `apt`/`gem`/`snap` commands. Windows tools are never auto-installed; manual install commands are printed instead.

---

### wsl-build-helper.sh

**Path:** `build/wsl-build-helper.sh`

Internal helper script called by `package-all.ps1`. Solves the WSL `/mnt/c/` permissions problem by staging package contents in `/tmp/` (native Linux filesystem).

**This script is not intended for standalone use.** Use the platform-native scripts (`build-deb.sh`, `build-rpm.sh`) when working directly in Linux.

#### Arguments

| Position | Required | Description |
|----------|----------|-------------|
| `$1` | Yes | Format: `deb` or `rpm` |
| `$2` | Yes | RID: e.g., `linux-x64` |
| `$3` | Yes | Repo root path (WSL path, e.g., `/mnt/c/Projects/cowsay-dotnet`) |

#### Behavior

1. Creates a temp directory in `/tmp/cowsay-pkg-XXXXXX`
2. Copies binaries and packaging files from the repo (on `/mnt/c/`) into the temp dir
3. Builds the package with correct permissions
4. Copies the result back to the repo's packaging output directory
5. Cleans up the temp directory (via `trap`)

#### Why This Exists

Files on the Windows filesystem mounted at `/mnt/c/` in WSL have `777` permissions. `dpkg-deb` rejects `DEBIAN/control` directories with permissions exceeding `0775`. By staging in `/tmp/`, the script gets proper Linux filesystem permissions.

---

## Linux Packaging

### build-deb.sh

**Path:** `packaging/linux/deb/build-deb.sh`

Builds Debian `.deb` packages. Standalone script for use on native Linux or WSL.

#### Arguments

| Position | Required | Default | Description |
|----------|----------|---------|-------------|
| `$1` | No | _(all 3 Linux RIDs)_ | RID to build |

#### Prerequisites

- `dpkg-deb`
- Pre-built binaries in `build/output/cowsay/<RID>/` (and optionally `build/output/cowthink/<RID>/`)

#### Usage

```bash
# Build for all Linux RIDs
./packaging/linux/deb/build-deb.sh

# Build for a specific RID
./packaging/linux/deb/build-deb.sh linux-x64

# Full workflow: publish first, then package
pwsh build/publish.ps1 -Rid linux-x64 -Projects cli,cowthink
./packaging/linux/deb/build-deb.sh linux-x64
```

#### Output

```
packaging/linux/deb/
  linux-x64/
    cowsay-dotnet_1.0.0_amd64.deb
  linux-arm/
    cowsay-dotnet_1.0.0_armhf.deb
  linux-arm64/
    cowsay-dotnet_1.0.0_arm64.deb
```

#### Architecture Mapping

| RID | Debian Architecture |
|-----|-------------------|
| `linux-x64` | `amd64` |
| `linux-arm` | `armhf` |
| `linux-arm64` | `arm64` |

#### Package Contents

The `.deb` installs:
- `/usr/local/bin/cowsay` -- main executable
- `/usr/local/bin/cowthink` -- cowthink executable (if built)
- `/usr/local/share/cowsay-dotnet/cows/` -- 48 cow files
- A `postinst` script that `chmod +x` the binaries

#### Notes

- Reads version from `Directory.Build.props`
- Computes `Installed-Size` for the control file
- Creates a staging directory under `<RID>/staging/`, cleaned up after build
- Falls back to `assets/cows/` at the repo root if cow files are not in the binary output directory

---

### build-rpm.sh

**Path:** `packaging/linux/rpm/build-rpm.sh`

Builds Red Hat `.rpm` packages using `rpmbuild` and the spec file at `packaging/linux/rpm/cowsay-dotnet.spec`.

#### Arguments

| Position | Required | Default | Description |
|----------|----------|---------|-------------|
| `$1` | No | _(all 3 Linux RIDs)_ | RID to build |

#### Prerequisites

- `rpmbuild` (install via `sudo apt install rpm` on Ubuntu)
- Pre-built binaries in `build/output/`
- Spec file at `packaging/linux/rpm/cowsay-dotnet.spec`

#### Usage

```bash
# Build for all Linux RIDs
./packaging/linux/rpm/build-rpm.sh

# Build for a specific RID
./packaging/linux/rpm/build-rpm.sh linux-x64
```

#### Output

```
packaging/linux/rpm/
  linux-x64/
    cowsay-dotnet-1.0.0-1.x86_64.rpm
  linux-arm/
    cowsay-dotnet-1.0.0-1.armv7hl.rpm
  linux-arm64/
    cowsay-dotnet-1.0.0-1.aarch64.rpm
```

#### Architecture Mapping

| RID | RPM Architecture |
|-----|-----------------|
| `linux-x64` | `x86_64` |
| `linux-arm` | `armv7hl` |
| `linux-arm64` | `aarch64` |

#### Notes

- Sets up a complete `rpmbuild` directory tree (`BUILD`, `RPMS`, `SOURCES`, `SPECS`, `SRPMS`) under `<RID>/rpmbuild/`
- Passes `_version` and `_topdir` as `--define` flags to `rpmbuild`
- Uses `--target` to set the architecture
- The "duplicate build-ids" warning for cowsay/cowthink is harmless (see [Troubleshooting](release-guide.md#troubleshooting))
- Cleans up the rpmbuild tree after copying the resulting RPM
- Falls back to repo-root `assets/cows/` for cow files

---

### build-snap.sh

**Path:** `packaging/linux/snap/build-snap.sh`

Builds Snap packages using `snapcraft`.

#### Arguments

| Position | Required | Default | Description |
|----------|----------|---------|-------------|
| `$1` | No | _(all 3 Linux RIDs)_ | RID to build |

#### Prerequisites

- `snapcraft` (`sudo snap install snapcraft --classic`)
- `snapd` (with systemd enabled in WSL)
- Pre-built binaries in `build/output/`

#### Usage

```bash
# Build for all Linux RIDs
./packaging/linux/snap/build-snap.sh

# Build for a specific RID
./packaging/linux/snap/build-snap.sh linux-x64
```

#### Output

```
packaging/linux/snap/
  linux-x64/
    cowsay-dotnet_1.0.0_amd64.snap
  linux-arm/
    cowsay-dotnet_1.0.0_armhf.snap
  linux-arm64/
    cowsay-dotnet_1.0.0_arm64.snap
```

#### Architecture Mapping

| RID | Snap Architecture |
|-----|-----------------|
| `linux-x64` | `amd64` |
| `linux-arm` | `armhf` |
| `linux-arm64` | `arm64` |

#### Notes

- **Dynamically generates `snapcraft.yaml`** per RID with the correct version and architecture
- Uses `base: core22`, `confinement: strict`, `plugin: dump`
- Exposes two apps: `cowsay` and `cowthink`, each with `home` and `removable-media` plugs
- Runs `snapcraft --build-for=<arch>` from a per-RID staging directory
- Requires a functional `snapd` daemon, which may need systemd enabled in WSL

---

### Makefile (Arch)

**Path:** `packaging/linux/.pkg.tar.zst/Makefile`

Builds Arch Linux `.pkg.tar.zst` packages using `makepkg` and the `PKGBUILD` file.

> **Note:** This is the **native Arch Linux build path**. When building from Windows via `package-all.ps1`, Arch packages are built using **FPM in Ubuntu WSL** instead, since the Arch WSL distro is damaged. The Makefile is provided for use on actual Arch Linux systems.

#### Targets

| Target | Description |
|--------|-------------|
| `all` (default) | Build for all 3 Linux RIDs |
| `linux-x64` | Build for x86_64 only |
| `linux-arm` | Build for armv7h only |
| `linux-arm64` | Build for aarch64 only |
| `clean` | Remove all build artifacts |

#### Prerequisites

- `makepkg` (from `pacman` -- Arch Linux only)
- Pre-built binaries in `build/output/`

#### Usage

```bash
# On an Arch Linux system:
make -C packaging/linux/.pkg.tar.zst

# Build only x64
make -C packaging/linux/.pkg.tar.zst linux-x64

# Clean up
make -C packaging/linux/.pkg.tar.zst clean
```

#### Output

```
packaging/linux/.pkg.tar.zst/
  linux-x64/
    cowsay-dotnet-1.0.0-1-x86_64.pkg.tar.zst
  linux-arm/
    cowsay-dotnet-1.0.0-1-armv7h.pkg.tar.zst
  linux-arm64/
    cowsay-dotnet-1.0.0-1-aarch64.pkg.tar.zst
```

#### FPM Alternative (used by package-all.ps1)

When `package-all.ps1` builds Arch packages, it runs FPM via WSL Ubuntu:

```bash
fpm -s dir -t pacman \
    -n cowsay-dotnet -v 1.0.0 \
    --license "GPL-3.0-or-later" \
    --description "A .NET reimplementation of cowsay" \
    --architecture x86_64 \
    ./usr/=/usr/
```

This produces equivalent `.pkg.tar.zst` files without requiring an Arch Linux environment.

---

## macOS Packaging

### build-brew.sh

**Path:** `packaging/mac/brew/build-brew.sh`

Creates tarballs suitable for use as Homebrew formula download URLs, plus SHA256 checksum sidecar files.

#### Arguments

| Position | Required | Default | Description |
|----------|----------|---------|-------------|
| `$1` | No | _(both macOS RIDs)_ | RID to build |

#### Prerequisites

- Pre-built binaries in `build/output/`
- `tar`
- `sha256sum` (Linux/WSL) or `shasum` (macOS) -- auto-detected

#### Usage

```bash
# Build tarballs for both macOS architectures
./packaging/mac/brew/build-brew.sh

# Build for a specific RID
./packaging/mac/brew/build-brew.sh osx-arm64

# Works from WSL too (called by package-all.ps1)
```

#### Output

```
packaging/mac/brew/
  osx-x64/
    cowsay-dotnet-1.0.0-osx-x64.tar.gz
    cowsay-dotnet-1.0.0-osx-x64.tar.gz.sha256
  osx-arm64/
    cowsay-dotnet-1.0.0-osx-arm64.tar.gz
    cowsay-dotnet-1.0.0-osx-arm64.tar.gz.sha256
```

The `.sha256` file contains just the hash string, for easy copy-paste into a Homebrew formula.

#### Notes

- Creates a directory-structured tarball with top-level directory `cowsay-dotnet-<version>/`
- The tarball contains `bin/cowsay`, `bin/cowthink`, and `share/cowsay-dotnet/cows/*.cow`
- Auto-detects `sha256sum` vs `shasum -a 256` (cross-platform compatibility)
- Prints "Next steps" instructions for updating your Homebrew tap formula
- Works on both native macOS and Linux/WSL (used by `package-all.ps1` via WSL)

#### Related File

`packaging/mac/brew/cowsay-dotnet.rb` -- The Homebrew formula template. Update the `version`, `url`, and `sha256` fields after building new tarballs.

---

### build-pkg.sh

**Path:** `packaging/mac/pkg/build-pkg.sh`

Builds macOS `.pkg` installers using Apple's native packaging tools.

> **Requires macOS.** This script cannot run on Windows or WSL. The `package-all.ps1` orchestrator always skips this format.

#### Arguments

| Position | Required | Default | Description |
|----------|----------|---------|-------------|
| `$1` | No | _(both macOS RIDs)_ | RID to build |

#### Prerequisites

- macOS with Xcode Command Line Tools (`pkgbuild`, `productbuild`)
- Pre-built binaries in `build/output/`
- `LICENSE` file at repo root

#### Usage

```bash
# On macOS: build for both architectures
./packaging/mac/pkg/build-pkg.sh

# Build for a specific architecture
./packaging/mac/pkg/build-pkg.sh osx-arm64
```

#### Output

```
packaging/mac/pkg/
  osx-x64/
    cowsay-dotnet-1.0.0-osx-x64.pkg
  osx-arm64/
    cowsay-dotnet-1.0.0-osx-arm64.pkg
```

#### Package Contents

The `.pkg` installs to:
- `/usr/local/bin/cowsay`
- `/usr/local/bin/cowthink`
- `/usr/local/share/cowsay-dotnet/cows/`

#### Notes

- Two-stage build: a component `.pkg` via `pkgbuild`, then a product `.pkg` via `productbuild`
- Dynamically generates a `Distribution.xml` with welcome text and license display
- Package identifier: `com.cowsay-dotnet.pkg`
- Cleans up intermediate artifacts (component package, staging dir, Distribution.xml)

---

## Windows Packaging

### build-installer.ps1

**Path:** `packaging/windows/installer/build-installer.ps1`

Builds Windows MSI installers using WiX Toolset v4.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all 4 Windows RIDs)_ | Single RID to build |

#### Prerequisites

- WiX Toolset v4 CLI (`dotnet tool install --global wix`)
- Pre-built binaries in `build/output/`
- WiX source: `packaging/windows/installer/cowsay-dotnet.wxs`

#### Usage

```powershell
# Build MSIs for all Windows architectures
pwsh packaging/windows/installer/build-installer.ps1

# Build for a specific RID
pwsh packaging/windows/installer/build-installer.ps1 -Rid win-x64
```

#### Output

```
packaging/windows/installer/
  win-x64/
    cowsay-dotnet-1.0.0-win-x64.msi
  win-x86/
    cowsay-dotnet-1.0.0-win-x86.msi
  win-arm/
    cowsay-dotnet-1.0.0-win-arm.msi
  win-arm64/
    cowsay-dotnet-1.0.0-win-arm64.msi
```

#### Notes

- **Dynamically generates `cowfiles.wxs`** -- a WiX fragment that enumerates all `.cow` files with auto-generated component and file IDs
- Passes `-arch` matching the RID (`x64`, `x86`, `arm`, `arm64`) and binds `BinDir` to the staging directory
- Reads version from `Directory.Build.props`
- The MSI installs cowsay/cowthink to `Program Files\cowsay-dotnet\` and adds it to the system PATH

#### Related File

`packaging/windows/installer/cowsay-dotnet.wxs` -- The WiX v4 source file. Defines the product, directory structure, features, and PATH environment variable entry. The `CowFiles` ComponentGroup is generated dynamically by the build script (do not add a placeholder in the `.wxs` file).

---

### build-choco.ps1

**Path:** `packaging/windows/choco/build-choco.ps1`

Builds Chocolatey `.nupkg` packages.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all 4 Windows RIDs)_ | Single RID to build |

#### Prerequisites

- `choco` CLI ([chocolatey.org/install](https://chocolatey.org/install))
- Pre-built binaries in `build/output/`

#### Usage

```powershell
# Build for all Windows architectures
pwsh packaging/windows/choco/build-choco.ps1

# Build for a specific RID
pwsh packaging/windows/choco/build-choco.ps1 -Rid win-x64
```

#### Output

```
packaging/windows/choco/
  win-x64/
    cowsay-dotnet.1.0.0.nupkg
  win-x86/
    cowsay-dotnet.1.0.0.nupkg
  ...
```

#### Notes

- Reads the template `.nuspec` from `packaging/windows/choco/cowsay-dotnet.nuspec` and injects the current version via regex replacement
- Copies binaries and cow files into a `tools/` directory (Chocolatey convention)
- Runs `choco pack` to produce the `.nupkg`
- Cleans up the staging directory after build

#### Related Files

| File | Purpose |
|------|---------|
| `cowsay-dotnet.nuspec` | Package metadata template (version is injected at build time) |
| `tools/chocolateyinstall.ps1` | Runs during `choco install` -- copies binaries to the Chocolatey tools directory |
| `tools/chocolateyuninstall.ps1` | Runs during `choco uninstall` -- cleans up installed files |

---

### build-winget.ps1

**Path:** `packaging/windows/winget/build-winget.ps1`

Generates WinGet manifest YAML files for submission to the [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) repository.

> **This script does not build an installer.** It generates manifest files that reference MSI installers hosted on GitHub Releases.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all 4 Windows RIDs)_ | Single RID to generate for |
| `-InstallerBaseUrl` | string | `https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v<version>` | Base URL for installer downloads |

#### Prerequisites

- Ideally, pre-built MSI at `packaging/windows/installer/<RID>/` (for SHA256 computation)
- No external tools strictly required to generate manifests

#### Usage

```powershell
# Generate manifests for all Windows architectures
pwsh packaging/windows/winget/build-winget.ps1

# Generate for a specific RID with a custom URL
pwsh packaging/windows/winget/build-winget.ps1 -Rid win-x64 `
    -InstallerBaseUrl "https://example.com/releases/v1.0.0"
```

#### Output

Three YAML files per RID:

```
packaging/windows/winget/
  win-x64/
    cowsay-dotnet.cowsay-dotnet.yaml                    # Version manifest
    cowsay-dotnet.cowsay-dotnet.installer.yaml           # Installer manifest
    cowsay-dotnet.cowsay-dotnet.locale.en-US.yaml        # Locale manifest
```

#### Notes

- Uses WinGet manifest schema version `1.6.0`
- Package identifier: `cowsay-dotnet.cowsay-dotnet`
- If the MSI file is not found locally, inserts `PLACEHOLDER_SHA256` and prints a warning
- Hardcodes `MinimumOSVersion: 10.0.17763.0` (Windows 10 1809)
- Prints "Next steps" instructions for submitting to `microsoft/winget-pkgs`

---

### build-psmodule.ps1

**Path:** `packaging/windows/powershell/build-psmodule.ps1`

Stages and optionally publishes the `Cowsay.PowerShell` module to the PowerShell Gallery.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Rid` | string | _(all 4 Windows RIDs)_ | Single RID to stage |
| `-Publish` | switch | `$false` | Publish the module to PSGallery |
| `-NuGetApiKey` | string | _(required if `-Publish`)_ | PSGallery API key |

#### Prerequisites

- Pre-built PowerShell module in `build/output/powershell/<RID>/`
- PowerShell 7 (`pwsh`)
- PSGallery API key (only if publishing)

#### Usage

```powershell
# Stage the module for all Windows architectures
pwsh packaging/windows/powershell/build-psmodule.ps1

# Stage for a specific RID
pwsh packaging/windows/powershell/build-psmodule.ps1 -Rid win-x64

# Stage and publish to PSGallery
pwsh packaging/windows/powershell/build-psmodule.ps1 -Rid win-x64 `
    -Publish -NuGetApiKey "your-api-key-here"
```

#### Output

```
packaging/windows/powershell/
  win-x64/
    Cowsay.PowerShell/
      Cowsay.PowerShell.dll
      Cowsay.PowerShell.psd1
      Cowsay.PowerShell.psm1
      assets/cows/*.cow
      (dependency DLLs)
```

#### Notes

- **Updates `ModuleVersion`** in the `.psd1` file to match the version from `Directory.Build.props`
- Runs `Test-ModuleManifest` to validate the manifest and prints exported cmdlets
- Falls back to `src/Cowsay.PowerShell/` for `.psd1` and `.psm1` if not found in the build output
- Without `-Publish`, prints a reminder command for manual publishing

---

## Script Summary Table

| Script | Format | Platform | Tool | RIDs |
|--------|--------|----------|------|------|
| `build/publish.ps1` | Binaries | Any (via dotnet) | `dotnet publish` | All 9 |
| `build/package-all.ps1` | All formats | Windows + WSL | Orchestrator | All 9 |
| `build/install-packaging-tools.ps1` | N/A | Windows + WSL | Checker | N/A |
| `build/wsl-build-helper.sh` | deb, rpm | WSL (internal) | `dpkg-deb`, `rpmbuild` | 3 Linux |
| `packaging/linux/deb/build-deb.sh` | `.deb` | Linux/WSL | `dpkg-deb` | 3 Linux |
| `packaging/linux/rpm/build-rpm.sh` | `.rpm` | Linux/WSL | `rpmbuild` | 3 Linux |
| `packaging/linux/snap/build-snap.sh` | `.snap` | Linux | `snapcraft` | 3 Linux |
| `packaging/linux/.pkg.tar.zst/Makefile` | `.pkg.tar.zst` | Arch Linux | `makepkg` | 3 Linux |
| `packaging/mac/brew/build-brew.sh` | `.tar.gz` | macOS/Linux/WSL | `tar` | 2 macOS |
| `packaging/mac/pkg/build-pkg.sh` | `.pkg` | macOS only | `pkgbuild` | 2 macOS |
| `packaging/windows/installer/build-installer.ps1` | `.msi` | Windows | `wix` | 4 Windows |
| `packaging/windows/choco/build-choco.ps1` | `.nupkg` | Windows | `choco` | 4 Windows |
| `packaging/windows/winget/build-winget.ps1` | YAML | Windows | None | 4 Windows |
| `packaging/windows/powershell/build-psmodule.ps1` | PS Module | Windows | PowerShell | 4 Windows |

---

## See Also

- [Release Guide](release-guide.md) -- End-to-end release workflow and post-release publishing
- [Architecture](architecture.md) -- Project structure, artifact layout, and design decisions
- [README](../README.md) -- User-facing installation and usage documentation
