using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>Clerk UI mounting and reactive auth state, using a contract fake (not a visual test).</summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SignInFlowTests : ClerkPageTest
{
    [Test]
    public async Task SignInPage_MountsClerkForm()
    {
        await GotoAsync("sign-in");
        await Expect(Page).ToHaveTitleAsync("Sign in");

        var card = Page.Locator(".cl-signIn-root");
        await Expect(card).ToBeVisibleAsync();
        await Expect(card.Locator("input[name='identifier']")).ToBeVisibleAsync();
        await Expect(card.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToBeEnabledAsync();
        await Expect(Page.Locator("iframe")).ToHaveCountAsync(0);
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task SignInPage_PassesRoutingPropsToClerkUi()
    {
        await GotoAsync("sign-in");
        await Expect(Page.Locator(".cl-signIn-root")).ToBeVisibleAsync();

        var calls = await Page.EvaluateAsync<string[]>("() => window.__fakeClerk.calls");
        Assert.That(calls, Does.Contain("mountSignIn"));
        Assert.That(await Page.EvaluateAsync<string>("() => window.__fakeClerk.lastProps.SignIn.routing"), Is.EqualTo("path"));
        Assert.That(await Page.EvaluateAsync<string>("() => window.__fakeClerk.lastProps.SignIn.path"), Is.EqualTo("/sign-in"));
    }

    [Test]
    public async Task SignIn_UpdatesUiWithoutPageReload()
    {
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
        await Page.EvaluateAsync("() => { window.__wasmAlive = true; }");

        await Page.EvaluateAsync("() => window.__fakeClerk.signIn()");

        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign out");
        await Expect(Page.Locator(".cl-userButtonTrigger")).ToBeVisibleAsync();
        Assert.That(await Page.EvaluateAsync<bool>("() => window.__wasmAlive === true"), Is.True, "The page must not reload after sign-in.");
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task SignOutButton_SignsOut()
    {
        await Page.AddInitScriptAsync("window.__fakeClerkSignedIn = true;");
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign out");

        await Page.Locator("nav").GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();

        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
        var calls = await Page.EvaluateAsync<string[]>("() => window.__fakeClerk.calls");
        Assert.That(calls, Does.Contain("signOut"));
    }

    [Test]
    public async Task NavSignIn_OpensClerkModalWithoutBlazorDialog()
    {
        await GotoAsync();
        await Page.Locator("nav").GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        var dialog = Page.Locator("[data-fake-clerk-modal='SignIn']");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("dialog")).ToHaveCountAsync(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task ProtectedPage_ShowsInlineSignIn_WhenSignedOut()
    {
        await GotoAsync("protected");
        await Expect(Page.Locator(".cl-signIn-root")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/protected$"));

        await Page.EvaluateAsync("() => window.__fakeClerk.signIn()");
        await Expect(Page.Locator("h3")).ToContainTextAsync("Protected");
    }

    [Test]
    public async Task ProtectedPage_IsAccessible_WhenSignedIn()
    {
        await Page.AddInitScriptAsync("window.__fakeClerkSignedIn = true;");
        await GotoAsync("protected");
        await Expect(Page.Locator("h3")).ToContainTextAsync("Protected");
        await Expect(Page.Locator("main")).ToContainTextAsync("Ada Lovelace");
    }
}
