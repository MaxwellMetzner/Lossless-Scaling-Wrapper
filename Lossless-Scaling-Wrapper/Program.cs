using System.Diagnostics;
static class Program
{
    // Placeholder for now — replace later
    private const string LosslessScalingPath = @"C:\Program Files (x86)\Steam\steamapps\common\Lossless Scaling\LosslessScaling.exe";

    [STAThread]
    public static int Main(string[] args)
    {
        // Steam Launch Options: "<WRAPPER_EXE>" -- %command%
        // => args should be: ["--", "<gameExe>", "<arg1>", ...]
        var gameArgs = StripLeadingDoubleDash(args);

        if (gameArgs.Count == 0)
            return 1;

        // Start Lossless Scaling minimized (skip if placeholder)
        TryStartLosslessScalingMinimized();

        // Start game and wait so Steam tracks runtime
        var gameExe = gameArgs[0];
        var gameExeArgs = gameArgs.Skip(1).ToList();

        using var p = StartProcess(gameExe, gameExeArgs, workingDir: SafeWorkingDir(gameExe));
        p.WaitForExit();
        return p.ExitCode;
    }

    private static List<string> StripLeadingDoubleDash(string[] args)
    {
        if (args.Length > 0 && args[0] == "--")
            return [.. args.Skip(1)];
        return [.. args];
    }

    private static void TryStartLosslessScalingMinimized()
    {
        if (string.IsNullOrWhiteSpace(LosslessScalingPath) || LosslessScalingPath == @"<LOSSLESS_SCALING_EXE>")
            return;

        if (!File.Exists(LosslessScalingPath))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = LosslessScalingPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Minimized
        };

        try { Process.Start(psi); } catch { }
    }

    private static Process StartProcess(string exe, List<string> argList, string? workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? ""
        };

        foreach (var a in argList)
            psi.ArgumentList.Add(a);

        return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start game process.");
    }

    private static string? SafeWorkingDir(string exe)
    {
        try
        {
            var dir = Path.GetDirectoryName(exe);
            return string.IsNullOrEmpty(dir) ? null : dir;
        }
        catch { return null; }
    }
}
