using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace ClerkBlazor.Tests;

/// <summary>
/// Assembly-level fixture that builds and starts the Blazor WASM dev server
/// once before any test runs, then shuts it down when all tests are done.
/// </summary>
[SetUpFixture]
public class BlazorDevServer
{
    private static Process? _process;

    /// <summary>Maximum number of characters retained in the in-memory log buffer.</summary>
    private const int MaxLogBufferLength = 64_000;

    /// <summary>How long to wait for the dev server to become reachable before giving up.</summary>
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Base URL of the running Blazor WASM dev server.
    /// Populated by <see cref="StartAsync"/> once the server reports the URL it is listening on.
    /// </summary>
    public static string BaseUrl { get; private set; } = "http://localhost:5107";

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        // Install Playwright browser binaries if they are not already present.
        var playwrightExit = Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);
        if (playwrightExit != 0)
            throw new InvalidOperationException("playwright install exited with code " + playwrightExit);

        // Resolve the ClerkBlazor project path relative to the test output directory.
        // Test binaries land in   ClerkBlazor.Tests/bin/<Config>/<TFM>/
        // Repo root is 4 levels up.
        var testDir = TestContext.CurrentContext.TestDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        var projectPath = Path.Combine(repoRoot, "ClerkBlazor", "ClerkBlazor.csproj");

        if (!File.Exists(projectPath))
            throw new FileNotFoundException(
                $"ClerkBlazor project not found at expected path '{projectPath}'. " +
                "Ensure the test is run from within the repository.");

        // Build the project first so `--no-build` makes the run fast.
        var buildInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Debug --nologo -v quiet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (var buildProcess = Process.Start(buildInfo)!)
        {
            await buildProcess.WaitForExitAsync().ConfigureAwait(false);
            if (buildProcess.ExitCode != 0)
            {
                var err = await buildProcess.StandardError.ReadToEndAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"dotnet build failed (exit {buildProcess.ExitCode}):\n{err}");
            }
        }

        var logBuffer = new StringBuilder();
        var logLock = new object();
        void AppendLog(string prefix, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            lock (logLock)
            {
                if (logBuffer.Length < MaxLogBufferLength)
                    logBuffer.AppendLine($"{prefix}{line}");
            }
        }

        var port = GetFreeTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}";

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                // --no-build because we already built above; this makes startup much faster.
                Arguments =
                    $"run --project \"{projectPath}\" " +
                    "--no-hot-reload --no-build --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.StartInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        _process.OutputDataReceived += (_, e) => AppendLog("OUT: ", e.Data);
        _process.ErrorDataReceived += (_, e) => AppendLog("ERR: ", e.Data);

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var startupCts = new CancellationTokenSource(StartupTimeout);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        while (!startupCts.IsCancellationRequested)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Blazor dev server exited with code {_process.ExitCode} before becoming ready.{Environment.NewLine}{logBuffer}");
            }

            try
            {
                using var response = await http.GetAsync(BaseUrl, startupCts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;

                AppendLog("HTTP: ", $"Received {(int)response.StatusCode} from {BaseUrl}.");
            }
            catch (OperationCanceledException) when (startupCts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AppendLog("HTTP: ", ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), startupCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (startupCts.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException(
            $"Blazor dev server did not become reachable on {BaseUrl} within {StartupTimeout.TotalSeconds:0}s.{Environment.NewLine}{logBuffer}");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    [OneTimeTearDown]
    public void Stop()
    {
        try
        {
            _process?.Kill(entireProcessTree: true);
            _process?.WaitForExit(5000);
        }
        catch { /* best-effort */ }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }
}
