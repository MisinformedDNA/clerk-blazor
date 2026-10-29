using System.Text.Json;
using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>Guards the zero-React guarantee: no React bundle is requested and the bridge refuses Clerk's React UI APIs.</summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ZeroReactTests : ClerkPageTest
{
    private static readonly Regex ReactAsset = new(@"react|@clerk/ui|clerk-ui|base-account|coinbase", RegexOptions.IgnoreCase);

    [TestCase("")]
    [TestCase("sign-in")]
    [TestCase("sign-up")]
    [TestCase("components")]
    public async Task Pages_DoNotRequestReact(string path)
    {
        await GotoAsync(path);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.That(Requests.Where(r => ReactAsset.IsMatch(r)), Is.Empty);
    }

    [TestCase("mountSignIn")]
    [TestCase("openSignIn")]
    [TestCase("__internal_openReverification")]
    [TestCase("authenticateWithBase")]
    [TestCase("authenticateWithCoinbaseWallet")]
    public async Task Bridge_RejectsReactUiMethods(string method)
    {
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");

        var result = await Page.EvaluateAsync<JsonElement>(
            "async (m) => { const b = await import('./_content/Clerk.Blazor/js/clerk-blazor.js'); return await b.invoke(m, []); }", method);

        Assert.That(result.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object, Is.True, $"'{method}' must be rejected.");
        var calls = await Page.EvaluateAsync<string[]>("() => window.__fakeClerk.calls");
        Assert.That(calls, Does.Not.Contain(method));
    }

    [Test]
    public async Task Bridge_NeverPassesUiConstructorToLoad()
    {
        await GotoAsync();
        await Expect(Page.Locator("nav")).ToContainTextAsync("Sign in");
        var keys = await Page.EvaluateAsync<string[]>("() => Object.keys(window.__fakeClerk.loadOptions)");
        Assert.That(keys, Has.None.EqualTo("ui").And.None.EqualTo("clerkUICtor"));
    }
}
