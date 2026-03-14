using System.Diagnostics;
using System.Net.Http;

namespace ClerkBlazor.Tests;

/// <summary>
/// Assembly-level fixture that builds and starts the Blazor WASM dev server
/// once before any test runs, then shuts it down when all tests are done.
/// </summary>
[SetUpFixture]
public class BlazorDevServer
{
    private static Process? _process;

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

        // The actual listening URL is read from the server's stdout.
        // ASP.NET Core emits: "Now listening on: http://localhost:XXXX"
        var urlTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                // --no-build because we already built above; this makes startup much faster.
                Arguments =
                    $"run --project \"{projectPath}\" " +
                    "--launch-profile http --no-hot-reload --no-build",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            // "Now listening on: http://localhost:5107" (or whatever port was free)
            const string marker = "Now listening on: ";
            var idx = e.Data.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var url = e.Data[(idx + marker.Length)..].Trim();
                urlTcs.TrySetResult(url);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait for the server to report its URL (up to 120 seconds).
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        cts.Token.Register(() =>
            urlTcs.TrySetException(new TimeoutException(
                "Blazor dev server did not report its URL within 120 seconds.")));

        BaseUrl = await urlTcs.Task.ConfigureAwait(false);

        // Extra sanity-check: confirm the server actually responds on that URL.
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(BaseUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception)
            {
                // Brief delay and retry.
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Blazor dev server started but did not respond on {BaseUrl} within 10 seconds.");
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
