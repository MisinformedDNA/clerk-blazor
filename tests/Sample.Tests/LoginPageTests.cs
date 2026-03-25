using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>
/// Playwright tests that verify the Login page (/login) loads correctly with
/// no JavaScript console errors, no Blazor error banner, and the expected
/// content — including the "Sign in with Clerk" button.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class LoginPageTests : PageTest
{
    private readonly List<string> _consoleErrors = [];

    private string LoginUrl => $"{BlazorDevServer.BaseUrl}/login";

    [SetUp]
    public void CaptureConsoleErrors()
    {
        _consoleErrors.Clear();

        // Capture any console.error messages emitted during the test.
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
                _consoleErrors.Add($"[error] {msg.Text}");
        };

        // Capture uncaught JavaScript exceptions (e.g. the old
        // "Missing publishableKey" crash from clerk-js auto-init).
        Page.PageError += (_, err) =>
            _consoleErrors.Add($"[uncaught] {err}");
    }

    /// <summary>
    /// Navigates to /login and asserts that the sign-in heading is visible
    /// and that no JavaScript errors or uncaught exceptions occurred during
    /// page load or Blazor initialisation.
    /// </summary>
    [Test]
    [Description("Login page must render its heading with no JS console errors.")]
    public async Task LoginPageLoads_WithExpectedContent_AndNoConsoleErrors()
    {
        await Page.GotoAsync(LoginUrl);

        // Wait for Blazor WASM to boot and render the heading.
        await Expect(Page.Locator("h1")).ToContainTextAsync("Sign in");

        Assert.That(
            _consoleErrors, Is.Empty,
            "Unexpected JavaScript errors on Login page:\n" +
            string.Join("\n", _consoleErrors));
    }

    /// <summary>
    /// The yellow "#blazor-error-ui" banner must remain hidden on the Login
    /// page, confirming that no unhandled Blazor rendering exceptions occurred.
    /// </summary>
    [Test]
    [Description("The Blazor error banner must not be visible on the Login page.")]
    public async Task LoginPageLoads_NoBlazorErrorBanner()
    {
        await Page.GotoAsync(LoginUrl);

        // Wait for the app to finish rendering before checking the banner.
        await Expect(Page.Locator("h1")).ToContainTextAsync("Sign in");

        await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// The document title should be "Sign in" once Blazor has rendered.
    /// </summary>
    [Test]
    [Description("The page title must be 'Sign in' once the Blazor app has loaded.")]
    public async Task LoginPageLoads_CorrectPageTitle()
    {
        await Page.GotoAsync(LoginUrl);
        await Expect(Page).ToHaveTitleAsync("Sign in");
    }

    /// <summary>
    /// The "Sign in with Clerk" button must be visible and enabled so the
    /// user can trigger the Clerk sign-in flow.
    /// </summary>
    [Test]
    [Description("The 'Sign in with Clerk' button must be visible and enabled.")]
    public async Task LoginPageLoads_SignInButtonVisibleAndEnabled()
    {
        await Page.GotoAsync(LoginUrl);

        var signInButton = Page.GetByRole(
            AriaRole.Button, new() { Name = "Sign in with Clerk" });

        await Expect(signInButton).ToBeVisibleAsync();
        await Expect(signInButton).ToBeEnabledAsync();
    }
}
