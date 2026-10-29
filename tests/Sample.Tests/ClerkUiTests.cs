using System.Text.Json;
using Microsoft.Playwright;

namespace Sample.Tests;

[TestFixture]
public class ClerkUiTests : ClerkPageTest
{
    private async Task ReadyAsync()
    {
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
    }

    [Test]
    public async Task Load_PassesOfficialUiConstructor()
    {
        await ReadyAsync();
        Assert.That(await Page.EvaluateAsync<bool>("() => window.__fakeClerk.loadOptions.ui.ClerkUI === window.__internal_ClerkUICtor"), Is.True);
    }

    [Test]
    public async Task DifferentSignInTriggers_CannotStackModals()
    {
        await ReadyAsync();
        await Page.Locator("nav").GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Page.Locator(".top-row").GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Expect(Page.Locator("[data-fake-clerk-modal]")).ToHaveCountAsync(1);
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task SignUp_ReplacesOpenSignIn()
    {
        await ReadyAsync();
        await Page.EvaluateAsync("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              await b.openUi('SignIn', {});
              await b.openUi('SignUp', {});
            }
            """);
        await Expect(Page.Locator("[data-fake-clerk-modal]")).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-fake-clerk-modal='SignUp']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_UnmountsOldContainer()
    {
        await GotoAsync("sign-in");
        await Expect(Page.Locator(".cl-signIn-root")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");
        await Page.WaitForFunctionAsync("() => window.__fakeClerk.calls.includes('unmountSignIn')");
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task Mount_UpdateAndRelease_PreserveStateUntilPropsChange()
    {
        await ReadyAsync();
        var calls = await Page.EvaluateAsync<string[]>("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              const node = document.body.appendChild(document.createElement('div'));
              window.__fakeClerk.calls.length = 0;
              const mount = b.mountUi('UserAvatar', node, {rounded: true});
              mount.update({rounded: true});
              mount.update({rounded: false});
              mount.release();
              mount.release();
              node.remove();
              return window.__fakeClerk.calls;
            }
            """);
        Assert.That(calls, Is.EqualTo(new[] { "mountUserAvatar", "unmountUserAvatar", "mountUserAvatar", "unmountUserAvatar" }));
    }

    [Test]
    public async Task OldTriggerDisposal_DoesNotCloseNewModal()
    {
        await ReadyAsync();
        await Page.EvaluateAsync("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              await b.openUi('SignIn', {}, null, 'old');
              await b.openUi('SignUp', {}, null, 'new');
              b.closeUi('old');
            }
            """);
        await Expect(Page.Locator("[data-fake-clerk-modal='SignUp']")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task LoadUiScript_UsesPinnedVersionAndNonce()
    {
        await Page.AddInitScriptAsync("delete window.__internal_ClerkUICtor;");
        await Page.RouteAsync("**/npm/@clerk/ui@*/dist/ui.browser.js", route =>
            route.FulfillAsync(new() { ContentType = "text/javascript", Body = "window.__internal_ClerkUICtor = class LoadedClerkUI {};" }));
        await ReadyAsync();
        await Page.EvaluateAsync("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              b.dispose();
              delete window.__internal_ClerkUICtor;
              await b.load({publishableKey: 'pk_test_Y2xlcmsuZXhhbXBsZS5jb20k', nonce: 'test-nonce'});
            }
            """);
        Assert.That(Requests.Any(r => r.Contains("@clerk/ui@1.42.1/dist/ui.browser.js")), Is.True);
        Assert.That(await Page.EvaluateAsync<string>("() => [...document.querySelectorAll('script[data-clerk-ui-script]')].at(-1).nonce"), Is.EqualTo("test-nonce"));
    }

    [Test]
    public async Task Headless_LoadsWithoutUiAndReportsUiDisabled()
    {
        await ReadyAsync();
        var uiRequestsBefore = Requests.Count(r => r.Contains("@clerk/ui"));
        var result = await Page.EvaluateAsync<JsonElement>("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              b.dispose();
              delete window.__internal_ClerkUICtor;
              await b.load({publishableKey: 'pk_test_Y2xlcmsuZXhhbXBsZS5jb20k', loadUserInterface: false});
              return {hasUi: 'ui' in window.__fakeClerk.loadOptions, result: await b.openUi('SignIn', {})};
            }
            """);
        Assert.That(result.GetProperty("hasUi").GetBoolean(), Is.False);
        Assert.That(Requests.Count(r => r.Contains("@clerk/ui")), Is.EqualTo(uiRequestsBefore));
        Assert.That(result.GetProperty("result").GetProperty("error").GetProperty("code").GetString(), Is.EqualTo("clerk_ui_disabled"));
    }

    [Test]
    public async Task UiScriptNetworkFailure_RejectsLoadExplicitly()
    {
        await ReadyAsync();
        await Page.RouteAsync("**/npm/@clerk/ui@*/dist/ui.browser.js", route => route.AbortAsync());
        var code = await Page.EvaluateAsync<string>("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              b.dispose();
              delete window.__internal_ClerkUICtor;
              try {
                await b.load({publishableKey: 'pk_test_Y2xlcmsuZXhhbXBsZS5jb20k'});
                return 'unexpected_success';
              } catch (error) {
                return error.clerkBlazor?.code;
              }
            }
            """);
        Assert.That(code, Is.EqualTo("script_failed_to_load"));
    }

    [Test]
    public async Task Reverification_CancelledByAnotherModal_Completes()
    {
        await ReadyAsync();
        Assert.That(await Page.EvaluateAsync<bool>("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              const verified = b.reverify('second_factor');
              await b.openUi('SignIn', {});
              return (await verified).value;
            }
            """), Is.False);
    }

    [Test]
    public async Task CustomRazorPage_MountsAndRestoresContent()
    {
        await ReadyAsync();
        Assert.That(await Page.EvaluateAsync<bool>("""
            async () => {
              const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js');
              const home = document.body.appendChild(document.createElement('div'));
              const content = home.appendChild(document.createElement('div'));
              content.id = 'test-razor-content';
              content.textContent = 'Custom Razor page';
              const host = document.body.appendChild(document.createElement('div'));
              const target = document.body.appendChild(document.createElement('div'));
              const mounted = b.mountUi('UserProfile', host, {__customItems: [
                {kind: 'page', label: 'Custom', url: 'custom', contentId: content.id}
              ]});
              const page = window.__fakeClerk.lastProps.UserProfile.customPages[0];
              page.mount(target);
              const moved = target.contains(content);
              page.unmount(target);
              const restored = home.contains(content);
              mounted.release();
              home.remove(); host.remove(); target.remove();
              return moved && restored;
            }
            """), Is.True);
    }
}
