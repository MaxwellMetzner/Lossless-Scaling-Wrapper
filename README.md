# Lossless Scaling Wrapper

A small Windows launcher that starts **Lossless Scaling** first, then launches a game (typically via Steam), minimizes the Lossless Scaling window, and brings the game window to the foreground.

This is useful when you want Lossless Scaling running for a game, but you don’t want the Lossless Scaling UI to steal focus during launch.

## How it works

When invoked, the wrapper:

1. Loads environment variables from a local `.env` file (optional).
2. Starts `LosslessScaling.exe` (if found).
3. Starts the target game executable (passed as arguments).
4. Minimizes the Lossless Scaling window **without activating it**.
5. Restores + focuses the game window (and re-asserts focus shortly after).
6. Waits for the game to exit and returns the game’s exit code.

## Requirements

- Windows (uses Win32 APIs to manage windows)
- .NET 8
- Lossless Scaling installed (Steam version) or otherwise accessible on disk

## Usage

### Steam Launch Options

Set your game’s Steam launch options to point at the wrapper and pass the game command through:


"<PATH_TO_WRAPPER_EXE>" -- %command%


**Notes:**

- Steam inserts `--` before `%command%`. The wrapper strips that and treats the remaining arguments as:
  - `gameExe` followed by `gameExeArgs...`

### Direct command line

You can also run it directly:


Lossless-Scaling-Wrapper.exe -- "C:\Path\To\Game.exe" -arg1 -arg2


If no game executable is provided, the wrapper exits with code `1`.

## Configuration

### Lossless Scaling executable path

By default, the wrapper looks for Lossless Scaling here:

- `C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe`

Override it by setting the environment variable:

- `LOSSLESS_SCALING_PATH`

You can set it either via your system environment variables or via a local `.env` file.

### `.env` file

The wrapper looks for `.env` in:

1. The app base directory (next to the wrapper executable)
2. The current working directory

**Format:**

- `KEY=VALUE` per line
- Blank lines and lines starting with `#` are ignored
- Values can be wrapped in `'single'` or `"double"` quotes

**Example `.env`:**


# Path to LosslessScaling.exe
LOSSLESS_SCALING_PATH="C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe"


On first build, the project generates a `.env.example` file in the project directory.

## Build

From the repository root:


dotnet build


## Troubleshooting

- **Lossless Scaling doesn’t start**
  - Check `LOSSLESS_SCALING_PATH` and verify the file exists.
  - If the path is missing/invalid, the wrapper will still launch the game.

- **Game doesn’t get focus**
  - The wrapper waits for the game’s main window handle and then calls Win32 APIs to restore and foreground it.
  - Some games / launchers may create windows late or use a separate bootstrap process; in those cases focusing may be unreliable.

## Disclaimer

This project is not affiliated with Lossless Scaling or Valve/Steam.
