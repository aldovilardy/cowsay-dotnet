# cowsay-dotnet
Cowsay is a configurable talking cow, written in C#. It operates much as the figlet program does, and is written in the same spirit of silliness.

## Usage

```powershell
dotnet run --project src/Cowsay.Cli -- [options] [message ...]
```

### Options

- `-f`, `--file`: Cowfile name or path.
- `-e`, `--eyes`: Custom eyes.
- `-T`, `--tongue`: Custom tongue (exactly 2 characters).
- `-W`, `--width`: Wrap width (default 40).
- `-n`, `--no-wrap`: Disable word wrapping and read message from standard input.
- `-l`, `--list`: List available cowfiles.
- `-b`, `-d`, `-g`, `-p`, `-s`, `-t`, `-w`, `-y`: Preset cow modes.

### Notes

- If no message arguments are provided, the CLI reads from standard input.
- If `-n` is used, the message must come from standard input.
- `COWPATH` is supported and searched when `-f` receives a cow name.
- If the executable is invoked as `cowthink`, output is rendered in thought mode.
