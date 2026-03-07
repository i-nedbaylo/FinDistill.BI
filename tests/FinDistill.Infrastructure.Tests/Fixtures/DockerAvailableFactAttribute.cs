namespace FinDistill.Infrastructure.Tests.Fixtures;

/// <summary>
/// Skips a test when Docker is not available.
/// Attempts to start Docker Desktop automatically if installed but not running.
/// Integration tests with Testcontainers require a running Docker daemon.
/// </summary>
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> DockerReady = new(EnsureDockerRunning);

    public DockerAvailableFactAttribute()
    {
        if (!DockerReady.Value)
        {
            Skip = "Docker is not available. Skipping integration test.";
        }
    }

    private static bool EnsureDockerRunning()
    {
        if (IsDockerResponding())
            return true;

        TryStartDockerDesktop();

        // Wait up to 60 seconds for Docker daemon to become responsive
        const int maxAttempts = 30;
        for (var i = 0; i < maxAttempts; i++)
        {
            Thread.Sleep(2000);
            if (IsDockerResponding())
                return true;
        }

        return false;
    }

    private static bool IsDockerResponding()
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            process.WaitForExit(10_000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryStartDockerDesktop()
    {
        // Try common Docker Desktop paths on Windows
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Docker", "Docker Desktop.exe")
        };

        var dockerPath = candidates.FirstOrDefault(File.Exists);
        if (dockerPath is null)
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dockerPath,
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch
        {
            // Docker Desktop not installed or cannot be started — will be skipped
        }
    }
}
