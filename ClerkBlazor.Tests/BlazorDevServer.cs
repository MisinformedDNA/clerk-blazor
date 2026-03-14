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

    /// <summary>Base URL of the running Blazor WASM dev server.</summary>
    public static string BaseUrl { get; } = "http://localhost:5107";

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

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments =
                    $"run --project \"{projectPath}\" " +
                    "--launch-profile http --no-hot-reload",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        _process.Start();

        // Poll until the server responds (up to 60 seconds).
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(BaseUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception)
            {
                // Server not ready yet; keep waiting.
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Blazor dev server did not become available on {BaseUrl} within 60 seconds.");
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
