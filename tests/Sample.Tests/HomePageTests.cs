using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>
/// Playwright tests that verify the Home page (/) loads correctly with no
/// JavaScript console errors, no Blazor error banner, and the expected content.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomePageTests : PageTest
{
    private readonly List<string> _consoleErrors = [];

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
    /// Navigates to the root URL and asserts that the main heading is visible
    /// and that no JavaScript errors or uncaught exceptions were thrown during
    /// page load or Blazor initialisation.
    /// </summary>
    [Test]
    [Description("Home page must render its heading with no JS console errors.")]
    public async Task HomePageLoads_WithExpectedContent_AndNoConsoleErrors()
    {
        await Page.GotoAsync(BlazorDevServer.BaseUrl);

        // Wait for Blazor WASM to boot and render the heading.
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        Assert.That(
            _consoleErrors, Is.Empty,
            "Unexpected JavaScript errors on Home page:\n" +
            string.Join("\n", _consoleErrors));
    }

    /// <summary>
    /// The yellow "#blazor-error-ui" banner must remain hidden, confirming
    /// that no unhandled Blazor rendering exceptions occurred.
    /// </summary>
    [Test]
    [Description("The Blazor error banner must not be visible on the Home page.")]
    public async Task HomePageLoads_NoBlazorErrorBanner()
    {
        await Page.GotoAsync(BlazorDevServer.BaseUrl);

        // Wait for the app to finish rendering before checking the banner.
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// The document title should be "Home" once Blazor has rendered.
    /// </summary>
    [Test]
    [Description("The page title must be 'Home' once the Blazor app has loaded.")]
    public async Task HomePageLoads_CorrectPageTitle()
    {
        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        await Expect(Page).ToHaveTitleAsync("Home");
    }

    /// <summary>
    /// A "Sign in" navigation link must be present, confirming the auth-aware
    /// layout rendered successfully.
    /// </summary>
    [Test]
    [Description("The navigation bar must contain a 'Sign in' link.")]
    public async Task HomePageLoads_SignInLinkVisible()
    {
        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
    }
}
