using Microsoft.Playwright;

namespace ClerkBlazor.Tests;

/// <summary>
/// Playwright end-to-end tests that validate the sign-in flow described in the
/// issue: clicking "Sign in" opens the Clerk popup, and after successful
/// authentication the page does NOT reload — the Blazor WASM runtime stays
/// alive and the UI updates reactively to show the authenticated view.
///
/// Because the tests run without real Clerk credentials, <c>clerkInterop.js</c>
/// is replaced at the network layer with a minimal mock.  The mock stores the
/// .NET callback registered via <c>onAuthChange</c> and fires it immediately
/// when <c>openSignIn</c> is called, simulating a successful sign-in without
/// contacting Clerk's CDN or API.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SignInFlowTests : PageTest
{
    private readonly List<string> _consoleErrors = [];

    /// <summary>
    /// Minimal JavaScript that replaces the real <c>clerkInterop.js</c>.
    /// The mock exposes the same public API as the real module but operates
    /// entirely in memory so tests remain hermetic and fast.
    /// </summary>
    private const string MockClerkInteropJs = """
        window.clerkInterop = (function () {
            var _dotNetRef = null;

            return {
                initialize: function () { return Promise.resolve(true); },

                openSignIn: function () {
                    if (!_dotNetRef) return;
                    var userJson = JSON.stringify({
                        id: 'user_playwright',
                        email: 'playwright@example.com',
                        firstName: 'Playwright',
                        lastName: 'Test',
                        imageUrl: null
                    });
                    // Slight delay so the mock behaves asynchronously,
                    // matching the real Clerk SDK's async nature.
                    setTimeout(function () {
                        _dotNetRef.invokeMethodAsync('OnAuthStateChanged', userJson);
                    }, 150);
                },

                getUser: function () { return Promise.resolve(null); },

                signOut: function () {
                    if (_dotNetRef) {
                        _dotNetRef.invokeMethodAsync('OnAuthStateChanged', null);
                    }
                    return Promise.resolve();
                },

                onAuthChange: function (dotNetRef) {
                    _dotNetRef = dotNetRef;
                }
            };
        }());
        """;

    [SetUp]
    public void CaptureConsoleErrors()
    {
        _consoleErrors.Clear();
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
                _consoleErrors.Add($"[error] {msg.Text}");
        };
        Page.PageError += (_, err) =>
            _consoleErrors.Add($"[uncaught] {err}");
    }

    /// <summary>
    /// Intercepts the <c>clerkInterop.js</c> request and returns the mock script
    /// instead, so tests run without contacting Clerk's CDN.
    /// </summary>
    private async Task SetupClerkMockAsync()
    {
        await Page.RouteAsync("**/js/clerkInterop.js", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                ContentType = "application/javascript",
                Body = MockClerkInteropJs
            });
        });
    }

    /// <summary>
    /// Clicking the "Sign in" button in the navigation bar must call the
    /// JavaScript <c>clerkInterop.openSignIn</c> function.  The test verifies
    /// this by replacing the interop module with a mock that tracks the call.
    /// </summary>
    [Test]
    [Description("Clicking 'Sign in' must invoke clerkInterop.openSignIn.")]
    public async Task SignIn_ClickButton_InvokesClerkOpenSignIn()
    {
        // Track whether openSignIn was called.
        await Page.AddInitScriptAsync(@"
            window.__openSignInCalled = false;
        ");

        await Page.RouteAsync("**/js/clerkInterop.js", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                ContentType = "application/javascript",
                Body = MockClerkInteropJs.Replace(
                    "openSignIn: function () {",
                    "openSignIn: function () { window.__openSignInCalled = true;")
            });
        });

        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        var signInBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        await Expect(signInBtn).ToBeVisibleAsync();
        await signInBtn.ClickAsync();

        var called = await Page.EvaluateAsync<bool>("() => window.__openSignInCalled");
        Assert.That(called, Is.True, "clerkInterop.openSignIn must be called when the Sign in button is clicked.");
    }

    /// <summary>
    /// After a successful sign-in, the page must NOT reload (the Blazor WASM
    /// runtime must remain alive) and the UI must update reactively:
    ///   - The "Sign in" button disappears from the nav.
    ///   - The top bar shows the authenticated user greeting ("Hello, …!").
    ///   - The nav shows a "Sign out" link.
    /// </summary>
    [Test]
    [Description("After sign-in, UI must update without a page reload.")]
    public async Task SignIn_AfterSuccess_UIUpdatesWithoutPageReload()
    {
        // Track whether the page performed a full navigation/reload.
        bool pageReloaded = false;
        Page.FrameNavigated += (_, frame) =>
        {
            // A full-page navigation fires on the main frame; ignore the initial load.
            if (frame == Page.MainFrame && frame.Url.Contains(BlazorDevServer.BaseUrl))
                pageReloaded = true;
        };

        await SetupClerkMockAsync();
        await Page.GotoAsync(BlazorDevServer.BaseUrl);

        // Reset the flag now that the initial load has completed.
        pageReloaded = false;

        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        // Verify unauthenticated state before sign-in.
        var signInBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        await Expect(signInBtn).ToBeVisibleAsync();

        // Act: click Sign in (the mock fires the .NET callback after ~150 ms).
        await signInBtn.ClickAsync();

        // Assert: the Sign in button must disappear (UI updated to Authorized view).
        await Expect(signInBtn).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

        // The top bar (MainLayout) must greet the signed-in user.
        await Expect(Page.Locator(".auth")).ToContainTextAsync("Hello");

        // The nav must now contain a Sign out link.
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign out");

        // Confirm no full-page reload occurred.
        Assert.That(pageReloaded, Is.False, "The page must not reload after sign-in.");
    }

    /// <summary>
    /// After sign-in, navigating to a protected page must show its content
    /// without triggering a redirect to the login page.
    /// </summary>
    [Test]
    [Description("Authorized content must be accessible after sign-in without a page reload.")]
    public async Task SignIn_AfterSuccess_ProtectedPageIsAccessible()
    {
        await SetupClerkMockAsync();
        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        // Sign in via the nav button.
        var signInBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        await signInBtn.ClickAsync();
        await Expect(signInBtn).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

        // Navigate to the protected page while still on the same page session.
        await Page.GotoAsync($"{BlazorDevServer.BaseUrl}/protected");

        // Should render the protected content, not redirect to login.
        await Expect(Page.Locator("h3")).ToContainTextAsync("Protected");
    }

    /// <summary>
    /// Confirms there are no JavaScript errors when using the mock interop,
    /// ensuring the mock interface contract matches what Blazor expects.
    /// </summary>
    [Test]
    [Description("Sign-in flow must produce no JavaScript errors.")]
    public async Task SignIn_Flow_NoConsoleErrors()
    {
        await SetupClerkMockAsync();
        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");

        var signInBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        await signInBtn.ClickAsync();
        await Expect(signInBtn).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

        Assert.That(
            _consoleErrors, Is.Empty,
            "No JavaScript errors must occur during the sign-in flow:\n" +
            string.Join("\n", _consoleErrors));
    }
}
