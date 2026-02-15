using System.Diagnostics;

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

        TryStartLosslessScalingMinimized();

        // Start game and wait so Steam tracks the play session.
        var gameExe = gameArgs[0];
        var gameExeArgs = gameArgs[1..];

        using var process = StartProcess(gameExe, gameExeArgs, SafeWorkingDir(gameExe));
        process.WaitForExit();
        return process.ExitCode;
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

    /// <summary>
    /// Launches Lossless Scaling minimized if a valid path is configured.
    /// Silently skips when the path is a placeholder, missing, or empty.
    /// </summary>
    private static void TryStartLosslessScalingMinimized()
    {
        var path = ResolveLosslessScalingPath();

        if (string.IsNullOrWhiteSpace(path) || path == PlaceholderPath)
            return;

        if (!File.Exists(path))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized,
            });
        }
        catch
        {
            // Non-critical — don't block game launch.
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
}
