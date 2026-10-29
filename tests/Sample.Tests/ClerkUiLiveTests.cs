using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>Real Clerk UI smoke checks. Does not sign in or change account data.</summary>
[TestFixture]
[Explicit("Requires a working Clerk development publishable key and network access.")]
public class ClerkUiLiveTests : PageTest
{
    [TestCase(1280, 900)]
    [TestCase(390, 844)]
    public async Task OfficialUi_LoadsWithoutNestedModals(int width, int height)
    {
        var errors = new List<string>();
        Page.PageError += (_, error) => errors.Add(error);
        await Page.SetViewportSizeAsync(width, height);
        await Page.GotoAsync(BlazorDevServer.BaseUrl + "/sign-in");
        await Expect(Page.Locator(".cl-signIn-root input[name='identifier']")).ToBeVisibleAsync(new() { Timeout = 30000 });
        await Expect(Page.Locator("dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".cb-sign-in")).ToHaveCountAsync(0);
        await CaptureAsync($"sign-in-{width}.png");

        await Page.GotoAsync(BlazorDevServer.BaseUrl + "/sign-up");
        await Expect(Page.Locator(".cl-signUp-root")).ToBeVisibleAsync(new() { Timeout = 30000 });
        await CaptureAsync($"sign-up-{width}.png");

        await Page.GotoAsync(BlazorDevServer.BaseUrl);
        var triggers = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in", Exact = true, IncludeHidden = true });
        await Expect(triggers).ToHaveCountAsync(2);
        await triggers.First.DispatchEventAsync("click");
        await Expect(Page.Locator(".cl-modalContent")).ToHaveCountAsync(1, new() { Timeout = 30000 });
        await triggers.Last.DispatchEventAsync("click");
        await Expect(Page.Locator(".cl-modalContent")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".cl-modalContent .cl-signIn-root input[name='identifier']")).ToBeVisibleAsync();
        await Expect(Page.Locator("dialog")).ToHaveCountAsync(0);
        await CaptureAsync($"sign-in-modal-{width}.png");
        await Page.Locator(".cl-modalCloseButton").ClickAsync();
        await Expect(Page.Locator(".cl-modalContent")).ToHaveCountAsync(0);
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    private async Task CaptureAsync(string filename)
    {
        var directory = Environment.GetEnvironmentVariable("CLERK_TEST_SCREENSHOT_DIR");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new() { Path = Path.Combine(directory, filename), FullPage = true, Animations = ScreenshotAnimations.Disabled });
    }
}
