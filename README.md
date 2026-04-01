# cowsay-dotnet

A faithful .NET reimplementation of the classic Unix **cowsay** and **cowthink** programs, originally created by [Tony Monroe](https://github.com/tnalpgge/rank-amateur-cowsay) and maintained by the [cowsay-org](https://github.com/cowsay-org/cowsay) project led by Andrew Janke.

cowsay-dotnet generates ASCII art of a cow (or other characters) saying or thinking a given message. It operates much as the original cowsay does, and is written in the same spirit of silliness -- but in modern C# targeting .NET 10.

```
 --------------- 
< Hello, World! >
 --------------- 

        \   ^__^
         \  (oo)\_______
            (__)\       )\/\
                ||----w |
                ||     ||
```

## Features

- Full compatibility with the original cowsay man page specification
- **cowsay** and **cowthink** commands (speech and thought bubbles)
- All 8 preset cow modes: `-b` Borg, `-d` Dead, `-g` Greedy, `-p` Paranoid, `-s` Stoned, `-t` Tired, `-w` Wired, `-y` Youthful
- Custom eyes (`-e`) and tongue (`-T`) support
- Configurable word wrap width (`-W`) and no-wrap mode (`-n`)
- **48 classic cow files** included (all original cowfiles from the cowsay distribution)
- `COWPATH` environment variable support for custom cow file directories
- **PowerShell module** with `Get-Cowsay` and `Get-Cowthink` cmdlets
- Standard input piping support
- Cross-platform: Windows, macOS, and Linux
- Self-contained single-file binaries -- no .NET runtime required to run

## Installation

cowsay-dotnet is available as self-contained, single-file binaries for Windows, macOS, and Linux. Choose the installation method that suits your platform.

### Linux

#### Debian / Ubuntu (`.deb`)

```bash
sudo dpkg -i cowsay-dotnet_1.0.0_amd64.deb
```

#### Fedora / RHEL / CentOS (`.rpm`)

```bash
sudo rpm -i cowsay-dotnet-1.0.0-1.x86_64.rpm
```

#### Snap

```bash
sudo snap install cowsay-dotnet
```

#### Arch Linux (`.pkg.tar.zst`)

```bash
sudo pacman -U cowsay-dotnet-1.0.0-1-x86_64.pkg.tar.zst
```

### macOS

#### Homebrew

```bash
brew tap cowsay-dotnet/tap
brew install cowsay-dotnet
```

#### macOS Installer (`.pkg`)

Download and run the `.pkg` installer from the [Releases](https://github.com/cowsay-dotnet/cowsay-dotnet/releases) page.

### Windows

#### MSI Installer

Download and run the `.msi` installer from the [Releases](https://github.com/cowsay-dotnet/cowsay-dotnet/releases) page. The installer adds `cowsay` and `cowthink` to your system `PATH`.

#### Chocolatey

```powershell
choco install cowsay-dotnet
```

#### WinGet

```powershell
winget install cowsay-dotnet.cowsay-dotnet
```

#### PowerShell Gallery

```powershell
Install-Module -Name Cowsay.PowerShell
```

### From Source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/cowsay-dotnet/cowsay-dotnet.git
cd cowsay-dotnet
dotnet build
dotnet run --project src/Cowsay.Cli -- "Hello from source!"
```

To publish self-contained binaries:

```powershell
# Publish for your current platform
pwsh build/publish.ps1 -Rid win-x64          # Windows x64
pwsh build/publish.ps1 -Rid linux-x64        # Linux x64
pwsh build/publish.ps1 -Rid osx-arm64        # macOS Apple Silicon

# Publish all platforms
pwsh build/publish.ps1
```

## Usage

### Basic Usage

```bash
cowsay "Hello, World!"
```

```
 --------------- 
< Hello, World! >
 --------------- 

        \   ^__^
         \  (oo)\_______
            (__)\       )\/\
                ||----w |
                ||     ||
```

### Cowthink (Thought Bubbles)

```bash
cowthink "Moo..."
```

```
 -------- 
( Moo... )
 -------- 

        o   ^__^
         o  (oo)\_______
            (__)\       )\/\
                ||----w |
                ||     ||
```

### Choose a Different Cow File

```bash
cowsay -f tux "Linux is fun!"
```

```
 --------------- 
< Linux is fun! >
 --------------- 

   \
    \
        .--.
       |o_o |
       |:_/ |
      //   \ \
     (|     | )
    /'\_   _/`\
    \___)=(___/
```

### Preset Modes

```bash
cowsay -d "I'm not feeling well..."     # Dead mode (xx eyes)
cowsay -b "Resistance is futile"        # Borg mode (== eyes)
cowsay -g "Show me the money"           # Greedy mode ($$ eyes)
cowsay -s "Far out, man..."             # Stoned mode (** eyes)
```

```
 ------------------------- 
< I'm not feeling well... >
 ------------------------- 

        \   ^__^
         \  (xx)\_______
            (__)\       )\/\
             U  ||----w |
                ||     ||
```

### Custom Eyes and Tongue

```bash
cowsay -e "OO" -T "U " "Custom face!"
```

### Piping from Standard Input

```bash
echo "Piped message" | cowsay
fortune | cowsay -f dragon
```

### No-Wrap Mode

```bash
cat long-file.txt | cowsay -n
```

### List Available Cow Files

```bash
cowsay -l
```

### Set Custom Wrap Width

```bash
cowsay -W 60 "This message will wrap at 60 columns instead of the default 40."
```

### COWPATH

Set the `COWPATH` environment variable to add custom cow file directories:

```bash
export COWPATH=/my/custom/cows:/another/cow/dir
cowsay -f my-custom-cow "Hello!"
```

### Command-Line Options

| Option | Long Form | Description |
|--------|-----------|-------------|
| `-f` | `--file` | Cowfile name or path |
| `-e` | `--eyes` | Custom eye string |
| `-T` | `--tongue` | Custom tongue string |
| `-W` | `--width` | Word wrap column width (default: 40) |
| `-n` | `--no-wrap` | Disable word wrapping; read from stdin |
| `-l` | `--list` | List available cowfiles |
| `-b` | | Borg mode (`==` eyes) |
| `-d` | | Dead mode (`xx` eyes, `U` tongue) |
| `-g` | | Greedy mode (`$$` eyes) |
| `-p` | | Paranoid mode (`@@` eyes) |
| `-s` | | Stoned mode (`**` eyes, `U` tongue) |
| `-t` | | Tired mode (`--` eyes) |
| `-w` | | Wired mode (`OO` eyes) |
| `-y` | | Youthful mode (`..` eyes) |

## PowerShell Module

The `Cowsay.PowerShell` module provides `Get-Cowsay` and `Get-Cowthink` cmdlets for use in PowerShell 7+.

### Installation

```powershell
Install-Module -Name Cowsay.PowerShell
```

### Usage

```powershell
Get-Cowsay -Message "Hello from PowerShell!"
Get-Cowthink -Message "Thinking..." -Cow tux
Get-Cowsay -Message "I'm dead" -Dead
Get-Cowsay -List
```

### Parameters

| Parameter | Description |
|-----------|-------------|
| `-Message` | The message to display |
| `-Cow` | Cowfile name (default: `default`) |
| `-Eyes` | Custom eye string |
| `-Tongue` | Custom tongue string |
| `-Width` | Word wrap column width (default: 40) |
| `-NoWrap` | Disable word wrapping |
| `-List` | List available cowfiles |
| `-Borg`, `-Dead`, `-Greedy`, `-Paranoid`, `-Stoned`, `-Tired`, `-Wired`, `-Youthful` | Preset cow modes |

## Available Cow Files

cowsay-dotnet ships with **48 classic cow files**:

```
beavis.zen    bong          bud-frogs     bunny         cheese
cower         daemon        default       dragon        dragon-and-cow
elephant      elephant-in-snake           eyes          flaming-sheep
ghostbusters  head-in       hellokitty    kiss          kitty
koala         kosh          luke-koala    mech-and-cow  meow
milk          moofasa       moose         mutilated     ren
satanic       sheep         skeleton      small         sodomized
squirrel      stegosaurus   stimpy        supermilker   surgery
telebears     three-eyes    turkey        turtle        tux
udder         vader         vader-koala   www
```

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

The test suite includes **189 tests** across 4 test projects, covering the core domain, application layer, infrastructure, and integration tests. Tests use **xUnit v3** with **Shouldly** assertions and **Moq** for mocking.

### Publish

```powershell
# Publish all projects for a specific RID
pwsh build/publish.ps1 -Rid win-x64

# Publish all projects for all 9 supported RIDs
pwsh build/publish.ps1
```

#### Supported Runtime Identifiers (RIDs)

| Platform | RIDs |
|----------|------|
| Windows | `win-x64`, `win-x86`, `win-arm`, `win-arm64` |
| macOS | `osx-x64`, `osx-arm64` |
| Linux | `linux-x64`, `linux-arm`, `linux-arm64` |

### Package All Formats

The cross-platform packaging orchestrator builds all package formats from a single Windows machine using WSL Ubuntu for Linux formats:

```powershell
# Install packaging tool prerequisites (one-time)
pwsh build/install-packaging-tools.ps1

# Build all package formats for all RIDs
pwsh build/package-all.ps1

# Build specific formats for a specific RID
pwsh build/package-all.ps1 -Rid linux-x64 -Formats deb,rpm
pwsh build/package-all.ps1 -Rid win-x64 -Formats msi,choco
```

## Project Architecture

cowsay-dotnet follows **Clean Architecture** principles:

```
src/
  CowSay.Core/            # Domain: models, interfaces, services
  Cowsay.Application/      # Use cases: GenerateCowSay handler, DTOs
  Cowsay.Infrastructure/   # File system: FileCowRepository, CowFilesDirectoryResolver
  Cowsay.Cli/              # CLI entry point (cowsay executable)
  Cowsay.Cowthink/         # CLI entry point (cowthink executable, shares Program.cs)
  Cowsay.PowerShell/       # PowerShell module (Get-Cowsay / Get-Cowthink cmdlets)

tests/
  Cowsay.Core.Tests/       # 68 tests - domain logic
  Cowsay.Application.Tests/# 17 tests - use case handlers
  Cowsay.Infrastructure.Tests/ # 33 tests - file system operations
  Cowsay.Tests/            # 71 tests - CLI integration tests

packaging/
  linux/                   # deb, rpm, snap, .pkg.tar.zst
  mac/                     # Homebrew formula, macOS .pkg
  windows/                 # MSI (WiX), Chocolatey, WinGet, PowerShell module

build/
  publish.ps1              # Multi-RID publish script
  package-all.ps1          # Cross-platform packaging orchestrator
  install-packaging-tools.ps1  # One-time tool setup
  wsl-build-helper.sh      # WSL helper for Linux package builds
```

## License

cowsay-dotnet is licensed under the **GNU General Public License v3.0 or later** (GPL-3.0-or-later).

```
Copyright 1999-2002 Tony Monroe
Copyright 2016-2024 Andrew Janke
Copyright 2022-2024 Cowsay contributors on GitHub
Copyright 2026 Aldo Vilardy
```

See [LICENSE](LICENSE) for the full license notice and [LICENSE-GPL-3.0.txt](LICENSE-GPL-3.0.txt) for the complete GPL v3 text.

## Acknowledgments

- **[Tony Monroe](https://github.com/tnalpgge/rank-amateur-cowsay)** -- Creator of the original cowsay (1999-2002). The cow files and the cowsay concept that started it all.
- **[Andrew Janke](https://github.com/cowsay-org/cowsay)** -- Maintainer of the cowsay-org fork (2016-2024), which modernized cowsay and clarified its licensing under GPL-3.0-or-later.
- **[Cowsay contributors on GitHub](https://github.com/cowsay-org/cowsay/graphs/contributors)** -- Community contributors to the cowsay-org project.
