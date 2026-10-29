namespace Clerk.Blazor.Tests;

public class ClerkServiceTests
{
    [Test]
    public void InitialState_IsLoading()
    {
        var clerk = TestClerk.Create();
        Assert.Multiple(() =>
        {
            Assert.That(clerk.State.Status, Is.EqualTo(ClerkStatus.Loading));
            Assert.That(clerk.IsLoaded, Is.False);
            Assert.That(clerk.Auth.IsSignedIn, Is.False);
        });
    }

    [Test]
    public void LoadAsync_WithoutPublishableKey_PublishesError()
    {
        var js = new FakeJSRuntime();
        var clerk = TestClerk.Create(o => o.PublishableKey = "", js: js);

        var ex = Assert.ThrowsAsync<ClerkException>(() => clerk.LoadAsync());
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo("missing_publishable_key"));
            Assert.That(clerk.State.Status, Is.EqualTo(ClerkStatus.Error));
            Assert.That(js.Calls, Is.Empty, "ClerkJS must not be imported without a key.");
        });
    }

    [Test]
    public void OnStateChanged_PublishesSnapshot()
    {
        var clerk = TestClerk.Create();
        ClerkState? published = null;
        clerk.StateChanged += s => published = s;

        clerk.OnStateChanged(TestClerk.SignedInSnapshot);

        Assert.That(published, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(published!.Status, Is.EqualTo(ClerkStatus.Ready));
            Assert.That(published.ClerkJSVersion, Is.EqualTo("6.42.0"));
            Assert.That(published.Auth.IsSignedIn, Is.True);
            Assert.That(published.Auth.UserId, Is.EqualTo("user_1"));
            Assert.That(published.Auth.OrgId, Is.EqualTo("org_1"));
            Assert.That(published.Auth.OrgRole, Is.EqualTo("org:admin"));
            Assert.That(published.User!.PrimaryEmailAddress!.EmailAddress, Is.EqualTo("ada@example.com"));
            Assert.That(published.Sessions.Select(s => s.Id), Is.EqualTo(new[] { "sess_1" }));
        });
    }

    [Test]
    public void OnStateChanged_IgnoresOutOfOrderSnapshots()
    {
        var clerk = TestClerk.Create();
        clerk.OnStateChanged(TestClerk.SignedInSnapshot.Replace("\"seq\": 1", "\"seq\": 5"));
        clerk.OnStateChanged("""{ "seq": 3, "status": "ready", "loaded": true }""");

        Assert.That(clerk.Auth.IsSignedIn, Is.True, "A stale signed-out snapshot must not overwrite newer state.");

        clerk.OnStateChanged("""{ "seq": 6, "status": "ready", "loaded": true }""");
        Assert.That(clerk.Auth.IsSignedIn, Is.False);
    }

    [Test]
    public void PendingSession_IsSignedOut_WhenTreatPendingAsSignedOut()
    {
        var pending = TestClerk.SignedInSnapshot.Replace("\"status\": \"active\", \"factor", "\"status\": \"pending\", \"currentTask\": { \"key\": \"choose-organization\" }, \"factor");

        var strict = TestClerk.Create(o => o.TreatPendingAsSignedOut = true);
        strict.OnStateChanged(pending);
        var lenient = TestClerk.Create(o => o.TreatPendingAsSignedOut = false);
        lenient.OnStateChanged(pending);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Auth.IsSignedIn, Is.False);
            Assert.That(strict.Auth.SessionStatus, Is.EqualTo("pending"));
            Assert.That(strict.BuildTasksUrl(), Does.EndWith("/choose-organization"));
            Assert.That(lenient.Auth.IsSignedIn, Is.True);
        });
    }

    [TestCase(null, "/fallback")]
    [TestCase("", "/fallback")]
    [TestCase("/dashboard?tab=1", "/dashboard?tab=1")]
    [TestCase("//evil.example.com/x", "/fallback")]
    [TestCase("https://app.example.com/orders#top", "/orders#top")]
    [TestCase("https://evil.example.com/steal", "/fallback")]
    [TestCase("javascript:alert(1)", "/fallback")]
    [TestCase("https://partner.example.com/landing", "https://partner.example.com/landing")]
    public void ResolveRedirect_OnlyAllowsSafeDestinations(string? input, string expected)
    {
        var clerk = TestClerk.Create(o => o.AllowedRedirectOrigins.Add("https://partner.example.com"));
        Assert.That(clerk.ResolveRedirect(input, "/fallback"), Is.EqualTo(expected));
    }

    [Test]
    public void BuildSignInUrl_IncludesCurrentPageAsRedirect()
    {
        var clerk = TestClerk.Create(o => o.SignInUrl = "/sign-in");
        Assert.Multiple(() =>
        {
            Assert.That(clerk.BuildSignInUrl(), Is.EqualTo("/sign-in?redirect_url=" + Uri.EscapeDataString("https://app.example.com/current?x=1")));
            Assert.That(clerk.BuildSignUpUrl("/after"), Does.EndWith("?redirect_url=%2Fafter"));
        });
    }

    [Test]
    public void Redirects_NavigateInsideApp_AndForceLoadExternalOrigins()
    {
        var nav = new FakeNavigationManager();
        var clerk = TestClerk.Create(o => o.UserProfileUrl = "/user", nav: nav);

        clerk.RedirectToUserProfile();
        _ = clerk.Navigate("https://accounts.example.dev/sign-in", replace: true);

        Assert.Multiple(() =>
        {
            Assert.That(nav.Navigations[0].Uri, Is.EqualTo("./user"));
            Assert.That(nav.Navigations[0].ForceLoad, Is.False);
            Assert.That(nav.Navigations[1].ForceLoad, Is.True);
            Assert.That(nav.Navigations[1].Replace, Is.True);
        });
    }

    [Test]
    public async Task WithReverificationAsync_PromptsAndRetriesOnce()
    {
        var clerk = TestClerk.Create();
        ReverificationRequirement? asked = null;
        clerk.ReverificationHandler = req => { asked = req; return Task.FromResult(true); };
        var attempts = 0;

        var result = await clerk.WithReverificationAsync(() =>
        {
            if (++attempts == 1) throw new ClerkReverificationRequiredException("verify", []);
            return Task.FromResult("done");
        });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("done"));
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(asked, Is.EqualTo(ReverificationRequirement.FromPreset(ReverificationPreset.Strict)));
        });
    }

    [Test]
    public void WithReverificationAsync_Cancelled_Throws()
    {
        var clerk = TestClerk.Create();
        clerk.ReverificationHandler = _ => Task.FromResult(false);

        var ex = Assert.ThrowsAsync<ClerkException>(() =>
            clerk.WithReverificationAsync(() => throw new ClerkReverificationRequiredException("verify", [])));
        Assert.That(ex!.Code, Is.EqualTo("reverification_cancelled"));
    }
}
