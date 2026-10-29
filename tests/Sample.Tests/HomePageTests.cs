using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>The Home page boots Blazor and Clerk with no errors.</summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomePageTests : ClerkPageTest
{
    [Test]
    public async Task HomePageLoads_WithExpectedContent_AndNoConsoleErrors()
    {
        await GotoAsync();
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
        AssertNoConsoleErrors();
    }

    [Test]
    public async Task HomePageLoads_NoBlazorErrorBanner()
    {
        await GotoAsync();
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");
        await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task HomePageLoads_CorrectPageTitle()
    {
        await GotoAsync();
        await Expect(Page).ToHaveTitleAsync("Home");
    }

    [Test]
    public async Task HomePageLoads_InjectsClerkStylesheet()
    {
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
        await Expect(Page.Locator("link[data-clerk-blazor-styles]")).ToHaveAttributeAsync("href", new Regex("clerk-blazor\\.css$"));
    }
}
