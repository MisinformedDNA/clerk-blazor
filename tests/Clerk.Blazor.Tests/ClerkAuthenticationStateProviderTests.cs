using System.Security.Claims;

namespace Clerk.Blazor.Tests;

public class ClerkAuthenticationStateProviderTests
{
    [Test]
    public async Task SignedOut_IsAnonymous()
    {
        var clerk = TestClerk.Create();
        using var provider = new ClerkAuthenticationStateProvider(clerk);

        var state = await provider.GetAuthenticationStateAsync();
        Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task SignedIn_ExposesClerkClaims()
    {
        var clerk = TestClerk.Create();
        using var provider = new ClerkAuthenticationStateProvider(clerk);
        clerk.OnStateChanged(TestClerk.SignedInSnapshot);

        var user = (await provider.GetAuthenticationStateAsync()).User;
        Assert.Multiple(() =>
        {
            Assert.That(user.Identity?.IsAuthenticated, Is.True);
            Assert.That(user.Identity?.AuthenticationType, Is.EqualTo(ClerkAuthenticationStateProvider.AuthenticationType));
            Assert.That(user.Identity?.Name, Is.EqualTo("Ada Lovelace"));
            Assert.That(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("user_1"));
            Assert.That(user.FindFirst("sid")?.Value, Is.EqualTo("sess_1"));
            Assert.That(user.FindFirst(ClaimTypes.Email)?.Value, Is.EqualTo("ada@example.com"));
            Assert.That(user.FindFirst("org_id")?.Value, Is.EqualTo("org_1"));
            Assert.That(user.IsInRole("org:admin"), Is.True);
            Assert.That(user.FindAll("org_permission").Select(c => c.Value), Is.EqualTo(new[] { "org:sys_billing:manage" }));
        });
    }

    [Test]
    public void StateChanges_NotifyOnlyWhenIdentityChanges()
    {
        var clerk = TestClerk.Create();
        using var provider = new ClerkAuthenticationStateProvider(clerk);
        var notifications = 0;
        provider.AuthenticationStateChanged += _ => notifications++;

        clerk.OnStateChanged(TestClerk.SignedInSnapshot);
        clerk.OnStateChanged(TestClerk.SignedInSnapshot.Replace("\"seq\": 1", "\"seq\": 2"));
        clerk.OnStateChanged("""{ "seq": 3, "status": "ready", "loaded": true }""");

        Assert.That(notifications, Is.EqualTo(2));
    }
}
