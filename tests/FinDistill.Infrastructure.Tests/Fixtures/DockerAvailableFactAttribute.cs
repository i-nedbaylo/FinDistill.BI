namespace FinDistill.Infrastructure.Tests.Fixtures;

/// <summary>
/// Skips a test when Docker is not available.
/// Integration tests with Testcontainers require a running Docker daemon.
/// </summary>
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> DockerReady = new(IsDockerResponding);

    public DockerAvailableFactAttribute()
    {
        if (!DockerReady.Value)
        {
            Skip = "Docker is not available. Start Docker Desktop and re-run tests.";
        }
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

            var exited = process.WaitForExit(10_000);
            if (!exited)
            {
                try { process.Kill(); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
