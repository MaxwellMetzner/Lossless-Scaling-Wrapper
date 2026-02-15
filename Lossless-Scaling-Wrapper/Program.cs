using System.Diagnostics;
using System.Runtime.InteropServices;

static class Program
{
    private const string DefaultLosslessScalingPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe";

    private const string EnvKey = "LOSSLESS_SCALING_PATH";
    private const string PlaceholderPath = "<LOSSLESS_SCALING_EXE>";

    [STAThread]
    public static int Main(string[] args)
    {
        // Load .env early so environment variables are available to the rest of the app.
        LoadDotEnv();

        // Steam Launch Options: "<WRAPPER_EXE>" -- %command%
        // => args should be: ["--", "<gameExe>", "<arg1>", ...]
        var gameArgs = StripLeadingDoubleDash(args);

        if (gameArgs.Length == 0)
            return 1;

        // Allow ourselves to set foreground later (best-effort).
        AllowSetForegroundWindow(-1);

        // Start Lossless Scaling first.
        var lsProc = StartLosslessScaling();

        // Start game.
        var gameExe = gameArgs[0];
        var gameExeArgs = gameArgs[1..];

        using var gameProc = StartProcess(gameExe, gameExeArgs, SafeWorkingDir(gameExe));

        // As soon as LS has a window, minimize it without activation.
        if (lsProc is not null)
            MinimizeNoActivate(lsProc);

        // Once the game has a window, bring it to front.
        BringToFront(gameProc);

        // Optional: re-assert focus in case LS steals it a moment later.
        Thread.Sleep(250);
        BringToFront(gameProc);

        gameProc.WaitForExit();
        return gameProc.ExitCode;
    }

    // ── Argument helpers ────────────────────────────────────────────────

    /// <summary>
    /// Strips a leading "--" separator that Steam inserts before %command% arguments.
    /// Returns the remaining arguments as a span-friendly array.
    /// </summary>
    private static string[] StripLeadingDoubleDash(string[] args)
    {
        if (args is ["--", ..])
            return args[1..];
        return args;
    }

    // ── .env loader ─────────────────────────────────────────────────────

    /// <summary>
    /// Loads key=value pairs from a <c>.env</c> file into the process environment.
    /// Searches the app base directory first, then the current working directory.
    /// Blank lines and lines starting with '#' are ignored.
    /// Values may optionally be wrapped in single or double quotes.
    /// </summary>
    private static void LoadDotEnv()
    {
        try
        {
            string? envPath = FindEnvFile();
            if (envPath is null)
                return;

            foreach (var line in File.ReadAllLines(envPath))
            {
                if (TryParseEnvLine(line, out var key, out var value))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch
        {
            // Silently ignore .env load errors to avoid breaking game launch.
        }
    }

    private static string? FindEnvFile()
    {
        ReadOnlySpan<string> candidates =
        [
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        ];

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Parses a single .env line into a key/value pair.
    /// Returns <c>false</c> for blank lines, comments, and malformed entries.
    /// </summary>
    private static bool TryParseEnvLine(string raw, out string key, out string value)
    {
        key = value = string.Empty;

        var line = raw.AsSpan().Trim();
        if (line.IsEmpty || line[0] == '#')
            return false;

        var eqIndex = line.IndexOf('=');
        if (eqIndex <= 0)
            return false;

        key = line[..eqIndex].Trim().ToString();
        if (key.Length == 0)
            return false;

        value = UnquoteValue(line[(eqIndex + 1)..].Trim());
        return true;
    }

    /// <summary>
    /// Strips matching surrounding quotes (single or double) from an .env value.
    /// </summary>
    private static string UnquoteValue(ReadOnlySpan<char> value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
             || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1].ToString();
        }

        return value.ToString();
    }

    // ── Lossless Scaling ────────────────────────────────────────────────

    private static string ResolveLosslessScalingPath()
        => Environment.GetEnvironmentVariable(EnvKey) ?? DefaultLosslessScalingPath;

    private static Process? StartLosslessScaling()
    {
        var path = ResolveLosslessScalingPath();

        if (string.IsNullOrWhiteSpace(path) || path == PlaceholderPath)
            return null;

        if (!File.Exists(path))
            return null;

        try
        {
            // UseShellExecute=false gives a more direct process handle on Windows.
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
            });

            return p;
        }
        catch
        {
            return null;
        }
    }

    // ── Process helpers ─────────────────────────────────────────────────

    private static Process StartProcess(string exe, ReadOnlySpan<string> argList, string? workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? string.Empty,
        };

        foreach (var arg in argList)
            psi.ArgumentList.Add(arg);

        return Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");
    }

    private static string? SafeWorkingDir(string exe)
    {
        try
        {
            var dir = Path.GetDirectoryName(exe);
            return string.IsNullOrEmpty(dir) ? null : dir;
        }
        catch
        {
            return null;
        }
    }

    // ── Win32 window helpers ────────────────────────────────────────────

    private const int SW_SHOWMINNOACTIVE = 7;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private static IntPtr WaitForMainWindow(Process p, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            p.Refresh();
            if (p.HasExited) return IntPtr.Zero;

            var h = p.MainWindowHandle;
            if (h != IntPtr.Zero) return h;

            Thread.Sleep(50);
        }
        return IntPtr.Zero;
    }

    private static void MinimizeNoActivate(Process p, int timeoutMs = 8000)
    {
        var h = WaitForMainWindow(p, timeoutMs);
        if (h != IntPtr.Zero)
            ShowWindow(h, SW_SHOWMINNOACTIVE);
    }

    private static void BringToFront(Process p, int timeoutMs = 20000)
    {
        var h = WaitForMainWindow(p, timeoutMs);
        if (h == IntPtr.Zero) return;

        ShowWindow(h, SW_RESTORE);
        SetForegroundWindow(h);
    }
}
