# Lossless Scaling Wrapper

A lightweight Windows launcher that ensures **Lossless Scaling** starts before your game, then minimizes it and restores focus to the game window.

This is primarily intended for use with **Steam launch options**, allowing Lossless Scaling to run automatically without interrupting gameplay.

---

## ⚠️ Required Setup (Steam Launch Options)

After downloading the `.exe`, **this step is mandatory for every game you want to use with the wrapper**.

For each game:

1. Open Steam
2. Go to your Library
3. Right-click the game → Properties
4. Under General, find Launch Options
5. Enter the following:

```text
"<Path to wrapper exe> ... \Lossless-Scaling-Wrapper.exe" -- %command%
```

### Important

- The quotes (" ") MUST wrap the full path to the wrapper executable
- Replace the path above with your actual `.exe` location
- The `-- %command%` portion MUST remain exactly as written
- This must be configured per game — there is NO global setting in Steam

---

## Best Results

For optimal behavior:

- Configure a profile for the game inside Lossless Scaling
- Lossless Scaling will automatically apply the profile on launch

---

## What It Does

1. Optionally loads environment variables from a `.env` file  
2. Starts `LosslessScaling.exe` (if found)  
3. Launches the game via Steam (`%command%`)  
4. Minimizes the Lossless Scaling window (without stealing focus)  
5. Brings the game window to the foreground  
6. Waits for the game to exit and returns its exit code  

---

## Requirements

- Windows  
- .NET 8  
- Lossless Scaling installed locally  

---

## Direct Usage (Optional)

You can run the wrapper manually:

```text
Lossless-Scaling-Wrapper.exe -- "C:\Path\To\Game.exe" -arg1 -arg2
```

If no game is provided, the wrapper exits with code `1`.

---

## Configuration

Default Lossless Scaling path:

```text
C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe
```

Override using environment variable:

```text
LOSSLESS_SCALING_PATH="C:\Custom\Path\LosslessScaling.exe"
```

`.env` lookup order:

1. Same directory as the wrapper `.exe`
2. Current working directory

---

## Build

```text
dotnet build
```

---

## Notes

- If Lossless Scaling is not found, the game still launches  
- Window focus behavior is best-effort and may vary by game/launcher  

---

## Disclaimer

Not affiliated with Lossless Scaling or Valve.
