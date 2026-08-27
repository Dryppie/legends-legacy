using System.Diagnostics;

namespace LegendsLegacy.Balance;

public static class GitCommitReader
{
    public static string? TryRead(string repositoryRoot)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short=12 HEAD",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5_000))
                return null;

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
