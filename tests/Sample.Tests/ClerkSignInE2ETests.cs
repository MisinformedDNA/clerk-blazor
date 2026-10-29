using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>
/// End-to-end Playwright test that drives the full Clerk sign-in flow using
/// real credentials against the live Clerk instance.
/// </summary>
[TestFixture]
[Explicit("Requires a real Clerk publishable key in src/Sample/wwwroot/appsettings.Development.json and test credentials.")]
[Parallelizable(ParallelScope.Self)]
public class ClerkSignInE2ETests : PageTest
{
    // Test credentials must be supplied explicitly; never ship fallback credentials.
    private static string TestEmail =>
        Environment.GetEnvironmentVariable("CLERK_TEST_EMAIL")
        ?? throw new InvalidOperationException("Set CLERK_TEST_EMAIL to run the real sign-in test.");

    private static string TestPassword =>
        Environment.GetEnvironmentVariable("CLERK_TEST_PASSWORD")
        ?? throw new InvalidOperationException("Set CLERK_TEST_PASSWORD to run the real sign-in test.");

    // 424242 is Clerk's documented magic verification code for test-mode accounts.
    private const string TestVerificationCode = "424242";

    private readonly List<string> _consoleLogs = [];
    private readonly List<string> _frameNavigations = [];
    private readonly List<string> _networkRequests = [];

    private string LoginUrl => $"{BlazorDevServer.BaseUrl}/sign-in";

    [SetUp]
    public void CaptureEvents()
    {
        _consoleLogs.Clear();
        _frameNavigations.Clear();
        _networkRequests.Clear();

        Page.Console += (_, msg) =>
            _consoleLogs.Add($"[{msg.Type}] {msg.Text}");

        Page.PageError += (_, err) =>
            _consoleLogs.Add($"[uncaught] {err}");

        Page.FrameNavigated += (_, frame) =>
        {
            if (frame == Page.MainFrame)
                _frameNavigations.Add($"[frame-nav] {frame.Url}");
        };

        Page.Request += (_, req) =>
        {
            // Capture navigations and cross-origin requests (potential sync redirects)
            if (req.IsNavigationRequest || !req.Url.StartsWith(BlazorDevServer.BaseUrl))
                _networkRequests.Add($"[{req.Method}] {req.Url[..Math.Min(120, req.Url.Length)]}");
        };
    }

    [TearDown]
    public async Task PrintDiagnostics()
    {
        await Page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), "clerk_e2e_final.png"), FullPage = true });

        if (_frameNavigations.Count > 0)
            TestContext.Out.WriteLine("Frame navigations:\n" + string.Join("\n", _frameNavigations));

        if (_networkRequests.Count > 0)
            TestContext.Out.WriteLine("Network requests:\n" + string.Join("\n", _networkRequests));

        var clerkLogs = _consoleLogs
            .Where(l => l.Contains("[Clerk.Blazor]") || l.StartsWith("[error]") || l.StartsWith("[warning]"))
            .ToList();
        if (clerkLogs.Count > 0)
            TestContext.Out.WriteLine("Relevant logs:\n" + string.Join("\n", clerkLogs));
    }

    [Test]
    [Description("Real Clerk sign-in: Sign out must appear without a full-page reload.")]
    public async Task SignIn_RealCredentials_SignOutAppearsWithoutPageReload()
    {
        // ── Navigate and wait for Blazor + Clerk to boot ──────────────────
        await Page.GotoAsync(LoginUrl);
        _frameNavigations.Clear();
        _networkRequests.Clear();

        await Expect(Page.Locator("h1")).ToContainTextAsync("Sign in");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.EvaluateAsync("() => { window.__wasmAlive = true; }");

        // ── Step 1: email / identifier ─────────────────────────────────────
        var identifierInput = Page.Locator(".cl-signIn-root input[name='identifier']");
        await identifierInput.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        await identifierInput.First.FillAsync(TestEmail);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).First.ClickAsync();

        // ── Step 2: password ───────────────────────────────────────────────
        var passwordInput = Page.Locator(".cl-signIn-root input[name='password']");
        await passwordInput.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await passwordInput.First.FillAsync(TestPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).First.ClickAsync();

        // ── Step 3: verification code ──────────────────────────────────────
        var firstCodeBox = Page.Locator("input[autocomplete='one-time-code']").First;
        await firstCodeBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await firstCodeBox.ClickAsync();
        await firstCodeBox.PressSequentiallyAsync(TestVerificationCode);

        await Page.WaitForTimeoutAsync(500);
        var continueAfterCode = Page.GetByRole(AriaRole.Button, new() { Name = "Continue" });
        if (await continueAfterCode.First.IsVisibleAsync())
            await continueAfterCode.First.ClickAsync();

        // ── Assertion: Sign out must appear without a page reload ──────────
        var signOutLink = Page.Locator("nav").GetByRole(AriaRole.Button, new() { Name = "Sign out" });
        await Expect(signOutLink.First).ToBeVisibleAsync(new() { Timeout = 20_000 });

        // ── Assertion: WASM runtime must still be alive ────────────────────
        var wasmAlive = await Page.EvaluateAsync<bool>("() => window.__wasmAlive === true");
        Assert.That(wasmAlive, Is.True,
            "Blazor WASM must remain alive after sign-in. " +
            "window.__wasmAlive was reset, indicating a full-page reload occurred.");
    }
}
