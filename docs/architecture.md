# Architecture

This document describes the project structure, build artifact layout, version flow, and key design decisions behind the cowsay-dotnet build and packaging system.

For step-by-step release instructions, see [Release Guide](release-guide.md).
For detailed script parameters, see [Build Scripts Reference](build-scripts.md).

---

## Table of Contents

- [Project Structure](#project-structure)
- [Source Projects](#source-projects)
- [Test Projects](#test-projects)
- [Publishable Artifacts](#publishable-artifacts)
- [Runtime Identifiers (RIDs)](#runtime-identifiers-rids)
- [Package Format Matrix](#package-format-matrix)
- [Build Output Layout](#build-output-layout)
- [Version Flow](#version-flow)
- [Cross-Platform Build Strategy](#cross-platform-build-strategy)
  - [WSL Bridge Pattern](#wsl-bridge-pattern)
  - [Dual-Path Arch Packages](#dual-path-arch-packages)
  - [macOS Limitations](#macos-limitations)
- [Cowthink Code Sharing](#cowthink-code-sharing)
- [Cow File Handling](#cow-file-handling)
- [Single-File Publish](#single-file-publish)

---

## Project Structure

```
cowsay-dotnet/
  assets/
    cows/                          # 48 cow files (.cow)
  build/
    package-all.ps1                # Orchestrates all packaging from Windows
    publish.ps1                    # Compiles binaries for all RIDs
    install-packaging-tools.ps1    # Checks/installs packaging prerequisites
    wsl-build-helper.sh            # WSL staging helper (deb/rpm)
    output/                        # Published binaries (gitignored)
  docs/
    release-guide.md               # Release workflow documentation
    build-scripts.md               # Script reference documentation
    architecture.md                # This file
  packaging/
    linux/
      deb/                         # Debian .deb packaging
      rpm/                         # Red Hat .rpm packaging
      snap/                        # Snap packaging
      .pkg.tar.zst/                # Arch Linux packaging
    mac/
      brew/                        # Homebrew tarballs + formula
      pkg/                         # macOS .pkg installer
    windows/
      installer/                   # WiX MSI installer
      choco/                       # Chocolatey .nupkg
      winget/                      # WinGet manifest YAML
      powershell/                  # PowerShell Gallery module
  src/
    Cowsay.Cli/                    # Main console app (cowsay)
    Cowsay.Cowthink/               # Cowthink console app
    Cowsay.Application/            # Application layer (use cases)
    CowSay.Core/                   # Domain layer (models, templates)
    Cowsay.Infrastructure/         # Infrastructure (file resolution)
    Cowsay.PowerShell/             # PowerShell module
    Cowsay.Shared/                 # Shared utilities (currently unused)
  tests/
    Cowsay.Tests/                  # 71 integration/CLI tests
    Cowsay.Core.Tests/             # 68 core domain tests
    Cowsay.Application.Tests/      # 17 application layer tests
    Cowsay.Infrastructure.Tests/   # 33 infrastructure tests
  Directory.Build.props            # Central version + metadata
  LICENSE                          # Structured copyright notice
  LICENSE-GPL-3.0.txt              # Full GPL v3 license text
  README.md                        # User-facing documentation
  .gitattributes                   # Line ending normalization
  .gitignore                       # Ignores build/output/ etc.
```

---

## Source Projects

| Project | Type | Output | Purpose |
|---------|------|--------|---------|
| `Cowsay.Cli` | Console App | `cowsay` / `cowsay.exe` | Main CLI executable |
| `Cowsay.Cowthink` | Console App | `cowthink` / `cowthink.exe` | Cowthink CLI executable |
| `Cowsay.Application` | Class Library | DLL | Application layer: use cases, orchestration |
| `CowSay.Core` | Class Library | DLL | Domain layer: cow models, template engine, face factory |
| `Cowsay.Infrastructure` | Class Library | DLL | Infrastructure: cow file discovery, file I/O |
| `Cowsay.PowerShell` | Class Library | DLL | PowerShell cmdlets (`Invoke-Cowsay`, `Invoke-Cowthink`, `Get-CowList`) |
| `Cowsay.Shared` | Class Library | DLL | Shared utilities (exists in solution but not referenced by any project) |

### Dependency Graph

```
Cowsay.Cli ──────────> Cowsay.Application ──> CowSay.Core
                                          └──> Cowsay.Infrastructure ──> CowSay.Core

Cowsay.Cowthink ─────> (links Cowsay.Cli\Program.cs)
                  └──> Cowsay.Application (same deps as Cli)

Cowsay.PowerShell ───> Cowsay.Application ──> (same as above)
```

---

## Test Projects

| Project | Tests | Framework | Focus |
|---------|-------|-----------|-------|
| `Cowsay.Tests` | 71 | xUnit v3 + Shouldly | Integration tests, CLI behavior, end-to-end |
| `Cowsay.Core.Tests` | 68 | xUnit v3 + Shouldly | Template engine, face factory, cow models |
| `Cowsay.Application.Tests` | 17 | xUnit v3 + Shouldly + Moq | Use cases, service orchestration |
| `Cowsay.Infrastructure.Tests` | 33 | xUnit v3 + Shouldly | File resolution, cow file discovery |

**Total: 189 tests** | Line coverage: **81.6%**

### Test Conventions

- **Naming:** `MethodOrScenario_Condition_ExpectedResult` with underscores
- **Assertions:** Shouldly (`result.ShouldBe(expected)`, `Should.Throw<T>(...)`, etc.)
- **SUT pattern:** `private readonly Foo _sut = new(...);`
- **Namespaces:** File-scoped (`namespace Foo.Bar;`)
- **Global using:** `<Using Include="Xunit" />` in `.csproj`; `using Shouldly;` at top of each file

---

## Publishable Artifacts

Three projects produce publishable output:

| Artifact | Project | Publish Mode | Description |
|----------|---------|-------------|-------------|
| `cowsay` | `Cowsay.Cli` | Single-file, self-contained | Main executable |
| `cowthink` | `Cowsay.Cowthink` | Single-file, self-contained | Cowthink executable |
| PowerShell module | `Cowsay.PowerShell` | Standard (not single-file) | `Cowsay.PowerShell.dll` + deps |

All artifacts include the 48 cow files from `assets/cows/`.

---

## Runtime Identifiers (RIDs)

The project targets **.NET 10** (`net10.0`) and builds for 9 RIDs across 3 platforms:

| Platform | RIDs | Package Formats |
|----------|------|----------------|
| **Windows** | `win-x64`, `win-x86`, `win-arm`, `win-arm64` | MSI, Chocolatey, WinGet, PS Module |
| **macOS** | `osx-x64`, `osx-arm64` | Homebrew tarball, macOS .pkg |
| **Linux** | `linux-x64`, `linux-arm`, `linux-arm64` | deb, rpm, snap, Arch .pkg.tar.zst |

---

## Package Format Matrix

Complete matrix of what gets built for each RID × format combination:

| Format | `linux-x64` | `linux-arm` | `linux-arm64` | `osx-x64` | `osx-arm64` | `win-x64` | `win-x86` | `win-arm` | `win-arm64` |
|--------|:-----------:|:-----------:|:-------------:|:---------:|:-----------:|:---------:|:---------:|:---------:|:-----------:|
| `.deb` | x | x | x | | | | | | |
| `.rpm` | x | x | x | | | | | | |
| `.snap` | x | x | x | | | | | | |
| `.pkg.tar.zst` | x | x | x | | | | | | |
| Brew `.tar.gz` | | | | x | x | | | | |
| macOS `.pkg` | | | | x | x | | | | |
| `.msi` | | | | | | x | x | x | x |
| Choco `.nupkg` | | | | | | x | x | x | x |
| WinGet YAML | | | | | | x | x | x | x |
| PS Module | | | | | | x | x | x | x |

**Total: 34 package artifacts** across all formats and RIDs (excluding macOS .pkg which requires native macOS).

---

## Build Output Layout

After running `pwsh build/publish.ps1`, binaries are placed under `build/output/`:

```
build/output/
  cowsay/
    win-x64/
      cowsay.exe              # Single-file self-contained executable
      cows/                   # 48 .cow files
    linux-x64/
      cowsay                  # Single-file self-contained executable
      cows/
    osx-arm64/
      cowsay
      cows/
    ... (9 RIDs total)
  cowthink/
    win-x64/
      cowthink.exe
      cows/
    ... (9 RIDs total)
  powershell/
    win-x64/
      Cowsay.PowerShell.dll   # Module DLL
      Cowsay.PowerShell.psd1  # Module manifest
      Cowsay.PowerShell.psm1  # Module script
      Cowsay.Application.dll  # Dependencies
      CowSay.Core.dll
      Cowsay.Infrastructure.dll
      cows/
    ... (9 RIDs total)
```

After running `pwsh build/package-all.ps1`, packages are placed alongside their build scripts:

```
packaging/
  linux/
    deb/linux-x64/cowsay-dotnet_1.0.0_amd64.deb
    rpm/linux-x64/cowsay-dotnet-1.0.0-1.x86_64.rpm
    snap/linux-x64/cowsay-dotnet_1.0.0_amd64.snap
    .pkg.tar.zst/linux-x64/cowsay-dotnet-1.0.0-1-x86_64.pkg.tar.zst
  mac/
    brew/osx-arm64/cowsay-dotnet-1.0.0-osx-arm64.tar.gz
    brew/osx-arm64/cowsay-dotnet-1.0.0-osx-arm64.tar.gz.sha256
    pkg/osx-arm64/cowsay-dotnet-1.0.0-osx-arm64.pkg          # macOS only
  windows/
    installer/win-x64/cowsay-dotnet-1.0.0-win-x64.msi
    choco/win-x64/cowsay-dotnet.1.0.0.nupkg
    winget/win-x64/*.yaml                                      # 3 manifest files
    powershell/win-x64/Cowsay.PowerShell/                      # Staged module dir
```

---

## Version Flow

A single `<Version>` element in `Directory.Build.props` drives all version numbers throughout the system:

```
Directory.Build.props
  <Version>1.0.0</Version>
        |
        +---> dotnet publish -p:Version=1.0.0
        |       (all .exe and .dll assemblies)
        |
        +---> build/publish.ps1
        |       (reads XML, passes to dotnet)
        |
        +---> build/package-all.ps1
        |       (reads XML, passes to sub-scripts and FPM)
        |
        +---> packaging/linux/deb/build-deb.sh
        |       (grep -oP, writes to DEBIAN/control)
        |
        +---> packaging/linux/rpm/build-rpm.sh
        |       (grep -oP, passes --define _version to rpmbuild)
        |
        +---> packaging/linux/snap/build-snap.sh
        |       (grep -oP, writes to generated snapcraft.yaml)
        |
        +---> packaging/mac/brew/build-brew.sh
        |       (grep -oP, names the tarball)
        |
        +---> packaging/mac/pkg/build-pkg.sh
        |       (grep -oP, names the .pkg)
        |
        +---> packaging/windows/installer/build-installer.ps1
        |       (XML parse, passes -d Version= to wix)
        |
        +---> packaging/windows/choco/build-choco.ps1
        |       (XML parse, injects into .nuspec)
        |
        +---> packaging/windows/winget/build-winget.ps1
        |       (XML parse, writes into YAML manifests)
        |
        +---> packaging/windows/powershell/build-psmodule.ps1
                (XML parse, updates ModuleVersion in .psd1)
```

**No other file needs manual version updates.** The `ModuleVersion` in `Cowsay.PowerShell.psd1` is updated automatically by `build-psmodule.ps1`.

---

## Cross-Platform Build Strategy

The build system is designed to produce packages for **all three platforms from a single Windows machine** with WSL Ubuntu.

### Architecture Overview

```
Windows Host
  |
  +-- pwsh build/package-all.ps1          (orchestrator)
  |     |
  |     +-- pwsh build/publish.ps1        (dotnet publish for all 9 RIDs)
  |     |
  |     +-- [Windows formats]
  |     |     +-- build-installer.ps1     (WiX MSI)
  |     |     +-- build-choco.ps1         (Chocolatey)
  |     |     +-- build-winget.ps1        (WinGet manifests)
  |     |     +-- build-psmodule.ps1      (PowerShell module)
  |     |
  |     +-- [Linux/macOS formats via WSL]
  |           +-- wsl -e bash wsl-build-helper.sh deb ...
  |           +-- wsl -e bash wsl-build-helper.sh rpm ...
  |           +-- wsl -e bash build-snap.sh ...
  |           +-- wsl -e bash build-brew.sh ...
  |           +-- wsl -e bash -c "fpm ..."   (Arch via FPM)
  |
  +-- WSL Ubuntu 24.04
        +-- dpkg-deb, rpmbuild, tar, sha256sum
        +-- fpm (Ruby gem, for Arch packages)
        +-- snapcraft (optional)
```

### WSL Bridge Pattern

Files on the Windows filesystem mounted at `/mnt/c/` in WSL have `777` permissions. This causes `dpkg-deb` to reject `DEBIAN/control` directories (which must be `<= 0775`).

The `wsl-build-helper.sh` script solves this by:

1. Creating a temp directory in `/tmp/` (native Linux filesystem with correct permissions)
2. Copying binaries and packaging metadata from `/mnt/c/` into the temp dir
3. Building the package in the temp dir
4. Copying the result back to `/mnt/c/`
5. Cleaning up via `trap`

This pattern is only needed for `deb` and `rpm` builds invoked from `package-all.ps1`. The standalone `build-deb.sh` and `build-rpm.sh` scripts assume they're running on a native Linux filesystem.

### Dual-Path Arch Packages

Arch Linux `.pkg.tar.zst` packages can be built two ways:

| Path | When to Use | Tool | Location |
|------|------------|------|----------|
| **Native** | On an Arch Linux system | `makepkg` + `PKGBUILD` | `packaging/linux/.pkg.tar.zst/Makefile` |
| **Cross-platform** | From Windows via WSL Ubuntu | `fpm` (Ruby gem) | Inline in `package-all.ps1` |

The FPM path exists because:
- The Arch Linux WSL distro on the development machine is damaged
- `makepkg` is not available on Ubuntu
- FPM can produce `.pkg.tar.zst` format from a directory structure without requiring `pacman`

Both paths produce functionally equivalent packages.

### macOS Limitations

macOS `.pkg` installers require Apple's `pkgbuild` and `productbuild` tools, which are only available on macOS (part of Xcode Command Line Tools). These **cannot be built from Windows or WSL**.

`package-all.ps1` always skips macOS `.pkg` with a message. To build `.pkg` installers, run `packaging/mac/pkg/build-pkg.sh` on a Mac.

Homebrew tarballs (`.tar.gz`) **can** be built from WSL since they only require `tar` and `sha256sum`.

---

## Cowthink Code Sharing

`Cowsay.Cowthink` does not duplicate any code from `Cowsay.Cli`. Instead, it links the same `Program.cs` source file:

```xml
<!-- Cowsay.Cowthink.csproj -->
<ItemGroup>
  <Compile Include="..\Cowsay.Cli\Program.cs" Link="Program.cs" />
</ItemGroup>
```

At runtime, the program detects its own binary name:
- If the executable is named `cowthink` (or `cowthink.exe`), it enables "think" mode (thought bubbles instead of speech bubbles)
- Otherwise, it runs in standard "say" mode

This means both executables share identical build IDs, which causes harmless "duplicate build-ids" warnings from `rpmbuild`.

---

## Cow File Handling

### Source Location

All 48 cow files live in `assets/cows/` at the repository root. They are `.cow` files containing ASCII art templates with `$thoughts` and `$eyes`/`$tongue` placeholders.

### Distribution

Cow files are included in every published artifact:
- **CLI/Cowthink binaries:** Copied into a `cows/` subdirectory alongside the executable
- **PowerShell module:** Copied into `assets/cows/` within the module directory
- **All packages:** Include cow files at the appropriate system path (e.g., `/usr/local/share/cowsay-dotnet/cows/` for Linux)

### Runtime Resolution

`Cowsay.Infrastructure.CowFilesDirectoryResolver` resolves the cow files directory at runtime:
1. First checks `AppContext.BaseDirectory` (works for single-file publish)
2. Falls back to `Assembly.Location` with `#pragma warning disable IL3000` guard
3. Looks for a `cows/` subdirectory relative to the resolved base path

### Perl Logic in Cow Files

Three cow files contain original Perl logic from the upstream cowsay project:
- `three-eyes.cow`
- `udder.cow`
- `small.cow`

The cow file preprocessor in `CowSay.Core` handles these by stripping the Perl constructs during template processing.

---

## Single-File Publish

CLI and cowthink executables are published as self-contained, single-file applications:

```
PublishSingleFile=true
IncludeNativeLibrariesForSelfExtract=true
--self-contained true
```

Key characteristics:
- **No .NET runtime required** on the target machine
- **Single executable** (no supporting DLLs alongside it, except cow files)
- **No trimming** (`PublishTrimmed` is NOT used) -- all assemblies are bundled intact
- The executable self-extracts native libraries to a temp directory on first run

The PowerShell module is **not** published as single-file because `Import-Module` needs to load individual DLLs.

---

## See Also

- [Release Guide](release-guide.md) -- End-to-end release workflow and post-release publishing
- [Build Scripts Reference](build-scripts.md) -- Detailed parameters and usage for every script
- [README](../README.md) -- User-facing installation and usage documentation
