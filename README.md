# Lossless Scaling Wrapper

Small Windows launcher for starting Lossless Scaling before a game, then minimizing Lossless Scaling and returning focus to the game window.

It is mainly useful for Steam launch options when you want Lossless Scaling open, but do not want its window interrupting the game launch.

## Best results

This works best when the game already has a profile configured in Lossless Scaling. That lets Lossless Scaling apply the profile automatically as soon as it starts.

## What it does

1. Optionally loads environment variables from a local `.env` file.
2. Starts `LosslessScaling.exe` if it can find it.
3. Starts the game command passed to the wrapper.
4. Minimizes the Lossless Scaling window without activating it.
5. Brings the game window to the foreground.
6. Waits for the game to exit and returns the same exit code.

## Requirements

- Windows
- .NET 8
- Lossless Scaling installed and accessible on disk

## Usage

For Steam launch options:

```text
"<PATH_TO_WRAPPER_EXE>" -- %command%
```

You can also run it directly:

```text
Lossless-Scaling-Wrapper.exe -- "C:\Path\To\Game.exe" -arg1 -arg2
```

If no game executable is provided, the wrapper exits with code `1`.

## Configuration

By default, the wrapper looks for Lossless Scaling here:

```text
C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe
```

To override that path, set `LOSSLESS_SCALING_PATH` as a system environment variable or in a local `.env` file.

The wrapper checks for `.env` in these locations:

1. Next to the wrapper executable
2. The current working directory

Example:

```text
LOSSLESS_SCALING_PATH="C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe"
```

## Build

```text
dotnet build
```

## Notes

- If Lossless Scaling cannot be found, the wrapper still launches the game.
- Focus behavior is best-effort. Some games and launchers create their main window late, so foregrounding may not always be perfect.

## Disclaimer

This project is not affiliated with Lossless Scaling or Valve/Steam.
