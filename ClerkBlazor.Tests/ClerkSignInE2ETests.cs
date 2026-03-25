using Microsoft.Playwright;

namespace ClerkBlazor.Tests;

/// <summary>
/// End-to-end Playwright test that drives the full Clerk sign-in flow using
/// real credentials against the live Clerk instance.
///
/// This test validates the primary requirement: clicking "Sign in with Clerk"
/// from the sign-in page opens the Clerk popup, the user can authenticate with
/// real credentials (email + password), and the "Sign out" link appears in-place
/// without a full-page reload -- confirming the Blazor WASM runtime remained alive
/// and the auth state was updated reactively through the JS to .NET interop.
///
/// Credentials note: test@test.com / password are rotated periodically.
/// Update TestPassword when credentials change.
/// IMPORTANT: the password must NOT appear in the HaveIBeenPwned breach database;
/// if Clerk shows "Password compromised" the test will be marked inconclusive.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ClerkSignInE2ETests : PageTest
{
    private const string TestEmail = "a042c1b7a9+clerk_test@clerkcookie.com";
    private const string TestPassword = "lkddsjsdlkjdslj";

    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _consoleLogs = [];

    private string LoginUrl => $"{BlazorDevServer.BaseUrl}/login";

    [SetUp]
    public void CaptureConsoleMessages()
    {
        _consoleLogs.Clear();
        _consoleErrors.Clear();

        Page.Console += (_, msg) =>
        {
            var entry = $"[{msg.Type}] {msg.Text}";
            _consoleLogs.Add(entry);
            if (msg.Type is "error" or "warning")
                _consoleErrors.Add(entry);
        };

        Page.PageError += (_, err) =>
        {
            _consoleLogs.Add($"[uncaught] {err}");
            _consoleErrors.Add($"[uncaught] {err}");
        };
    }

    /// <summary>
    /// Navigates to /login, waits for Clerk to fully initialize, triggers the
    /// Clerk sign-in modal with real credentials, and asserts that:
    /// <list type="bullet">
    ///   <item>The "Sign out" link becomes visible in the navigation without
    ///   any explicit page navigation from the test.</item>
    ///   <item>The Blazor WASM runtime was NOT reloaded -- a JS marker set
    ///   before sign-in must still be present after sign-in completes.</item>
    /// </list>
    ///
    /// The Clerk SignIn component (openSignIn()) injects its UI directly
    /// into the page DOM (not inside an iframe), so standard Playwright locators
    /// interact with it directly.
    /// </summary>
    [Test]
    [Description("Real Clerk sign-in: Sign out must appear without a full-page reload.")]
    public async Task SignIn_RealCredentials_SignOutAppearsWithoutPageReload()
    {
        // Navigate to the sign-in page and wait for Blazor to boot.
        await Page.GotoAsync(LoginUrl);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Sign in");

        // Wait until clerkInterop.initialize() has fully completed (CDN script
        // loaded + _clerk.load() resolved).  This prevents clicking Sign in
        // before Clerk is ready, which would throw "not initialized".
        await Page.WaitForFunctionAsync(
            "() => window.__clerkInteropReady === true",
            options: new PageWaitForFunctionOptions { Timeout = 30_000 });

        // Inject a WASM-alive marker.  A full-page reload resets all JS state,
        // so if this marker is gone after sign-in we know a reload occurred.
        await Page.EvaluateAsync("() => { window.__wasmAlive = true; }");

        // Open the Clerk sign-in modal.
        var signInWithClerkBtn = Page.GetByRole(
            AriaRole.Button, new() { Name = "Sign in with Clerk" });

        await Expect(signInWithClerkBtn).ToBeVisibleAsync();
        await signInWithClerkBtn.ClickAsync();

        // The Clerk modal renders in the page DOM.
        // Wait for the email input using the selectors used by Clerk v5.
        var identifierInput = Page.Locator("input[name='identifier']")
            .Or(Page.Locator("input#identifier-field"))
            .Or(Page.GetByLabel("Email address"));

        await identifierInput.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });
        await identifierInput.First.FillAsync(TestEmail);

        // Click "Continue" to advance to the password step.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" })
            .First.ClickAsync();

        // Wait for the password input.
        var passwordInput = Page.Locator("input[name='password']")
            .Or(Page.Locator("input#password-field"))
            .Or(Page.GetByLabel("Password"));

        await passwordInput.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000
        });
        await passwordInput.First.FillAsync(TestPassword);

        // Submit the password.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" })
            .First.ClickAsync();

        // Wait for the form to process, then take a diagnostic screenshot.
        await Page.WaitForTimeoutAsync(2_000);
        await Page.ScreenshotAsync(new()
        {
            Path = "/tmp/clerk_e2e_after_submit.png",
            FullPage = true
        });

        // ── Detect known Clerk security blocks ────────────────────────────
        // If the password appears in the HaveIBeenPwned breach database,
        // Clerk shows a "Password compromised" error and blocks sign-in.
        // This is a credential management issue, not a code bug.
        var passwordCompromisedHeading = Page.GetByRole(
            AriaRole.Heading, new() { Name = "Password compromised" });

        if (await passwordCompromisedHeading.IsVisibleAsync())
        {
            Assert.Inconclusive(
                "Clerk blocked sign-in because the test password appears in a known " +
                "data-breach database (HaveIBeenPwned). " +
                "Please reset the test account password to one that is not compromised, " +
                "then update the TestPassword constant in ClerkSignInE2ETests.cs.");
            return;
        }

        // ── Assertion: Sign out link must appear ───────────────────────────
        // The Clerk addListener fires OnAuthStateChanged, Blazor updates
        // CascadingAuthenticationState, AuthorizeView switches to Authorized,
        // and NavMenu / MainLayout both render the "Sign out" link.
        var signOutLink = Page.GetByRole(AriaRole.Link, new() { Name = "Sign out" });

        try
        {
            await Expect(signOutLink.First).ToBeVisibleAsync(new() { Timeout = 20_000 });
        }
        catch
        {
            // Capture a final state screenshot and print diagnostics before re-throwing.
            await Page.ScreenshotAsync(new()
            {
                Path = "/tmp/clerk_e2e_timeout.png",
                FullPage = true
            });

            var clerkLogs = _consoleLogs
                .Where(l => l.Contains("[clerkInterop]") || l.StartsWith("[error]") || l.StartsWith("[warning]") || l.StartsWith("[uncaught]"))
                .ToList();

            if (clerkLogs.Count > 0)
                TestContext.Out.WriteLine("Clerk interop / error logs:\n" + string.Join("\n", clerkLogs));

            throw;
        }

        // ── Assertion: no full-page reload ────────────────────────────────
        var wasmAlive = await Page.EvaluateAsync<bool>(
            "() => window.__wasmAlive === true");

        Assert.That(wasmAlive, Is.True,
            "Blazor WASM must remain alive after sign-in. " +
            "window.__wasmAlive was reset, indicating a full-page reload occurred.");
    }
}
