using System.Security.Claims;
using ClerkBlazor.Services;

namespace ClerkBlazor.Tests;

/// <summary>
/// Unit tests for <see cref="ClerkAuthenticationStateProvider"/>.
/// These tests verify the authentication state management logic without
/// requiring a running server or browser.
/// </summary>
[TestFixture]
public class ClerkAuthStateProviderTests
{
    private FakeJSRuntime _js = null!;
    private ClerkAuthService _clerkService = null!;
    private ClerkAuthenticationStateProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _js = new FakeJSRuntime();
        _clerkService = new ClerkAuthService(_js);
        _sut = new ClerkAuthenticationStateProvider(_clerkService);
    }

    [TearDown]
    public async Task TearDown()
        => await _sut.DisposeAsync();

    // ── GetAuthenticationStateAsync ──────────────────────────────────────────

    [Test]
    [Description("GetAuthenticationStateAsync must return anonymous state when Clerk is not initialised.")]
    public async Task GetAuthenticationState_NotInitialized_ReturnsAnonymous()
    {
        var state = await _sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
    }

    // ── OnAuthStateChanged ───────────────────────────────────────────────────

    [Test]
    [Description("OnAuthStateChanged(null) must notify subscribers with anonymous state.")]
    public async Task OnAuthStateChanged_NullJson_SetsAnonymousState()
    {
        bool notified = false;
        Microsoft.AspNetCore.Components.Authorization.AuthenticationState? receivedState = null;

        _sut.AuthenticationStateChanged += async stateTask =>
        {
            receivedState = await stateTask;
            notified = true;
        };

        _sut.OnAuthStateChanged(null);

        // Give the async notification a moment to propagate.
        await Task.Delay(50);

        Assert.That(notified, Is.True, "State change notification must fire.");
        Assert.That(receivedState?.User.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    [Description("OnAuthStateChanged with empty/whitespace JSON must set anonymous state.")]
    public async Task OnAuthStateChanged_EmptyJson_SetsAnonymousState()
    {
        bool notified = false;
        Microsoft.AspNetCore.Components.Authorization.AuthenticationState? receivedState = null;

        _sut.AuthenticationStateChanged += async stateTask =>
        {
            receivedState = await stateTask;
            notified = true;
        };

        _sut.OnAuthStateChanged("   ");

        await Task.Delay(50);

        Assert.That(notified, Is.True);
        Assert.That(receivedState?.User.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    [Description("OnAuthStateChanged with valid user JSON must set authenticated state with correct claims.")]
    public async Task OnAuthStateChanged_ValidUserJson_SetsAuthenticatedState()
    {
        bool notified = false;
        Microsoft.AspNetCore.Components.Authorization.AuthenticationState? receivedState = null;

        _sut.AuthenticationStateChanged += async stateTask =>
        {
            receivedState = await stateTask;
            notified = true;
        };

        const string userJson = """
            {
                "id": "user_test123",
                "email": "alice@example.com",
                "firstName": "Alice",
                "lastName": "Smith",
                "imageUrl": null
            }
            """;

        _sut.OnAuthStateChanged(userJson);

        await Task.Delay(50);

        Assert.That(notified, Is.True, "State change notification must fire.");
        Assert.That(receivedState?.User.Identity?.IsAuthenticated, Is.True,
            "User must be authenticated after a valid user JSON is received.");
        Assert.That(receivedState?.User.Identity?.AuthenticationType, Is.EqualTo("Clerk"));

        var principal = receivedState!.User;
        Assert.That(
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Is.EqualTo("user_test123"),
            "NameIdentifier claim must match the user id.");
        Assert.That(
            principal.FindFirst(ClaimTypes.Email)?.Value,
            Is.EqualTo("alice@example.com"),
            "Email claim must be set.");
        Assert.That(
            principal.FindFirst(ClaimTypes.GivenName)?.Value,
            Is.EqualTo("Alice"),
            "GivenName claim must be set.");
        Assert.That(
            principal.FindFirst(ClaimTypes.Surname)?.Value,
            Is.EqualTo("Smith"),
            "Surname claim must be set.");
        Assert.That(
            principal.FindFirst(ClaimTypes.Name)?.Value,
            Is.EqualTo("alice@example.com"),
            "Name claim defaults to the email address when available.");
    }

    [Test]
    [Description("OnAuthStateChanged with a user that has no email must fall back to user ID for the Name claim.")]
    public async Task OnAuthStateChanged_UserWithNoEmail_FallsBackToIdForNameClaim()
    {
        Microsoft.AspNetCore.Components.Authorization.AuthenticationState? receivedState = null;

        _sut.AuthenticationStateChanged += async stateTask =>
            receivedState = await stateTask;

        const string userJson = """
            {
                "id": "user_noemail",
                "email": null,
                "firstName": null,
                "lastName": null,
                "imageUrl": null
            }
            """;

        _sut.OnAuthStateChanged(userJson);
        await Task.Delay(50);

        var principal = receivedState!.User;
        Assert.That(
            principal.FindFirst(ClaimTypes.Name)?.Value,
            Is.EqualTo("user_noemail"),
            "Name claim must fall back to user ID when no email is set.");
        Assert.That(
            principal.FindFirst(ClaimTypes.Email),
            Is.Null,
            "No email claim must be added when email is null.");
    }

    [Test]
    [Description("Calling OnAuthStateChanged twice must notify for each call.")]
    public async Task OnAuthStateChanged_CalledTwice_NotifiesTwice()
    {
        int notificationCount = 0;
        _sut.AuthenticationStateChanged += async stateTask =>
        {
            await stateTask;
            Interlocked.Increment(ref notificationCount);
        };

        const string userJson = """{"id":"u1","email":"a@b.com","firstName":null,"lastName":null,"imageUrl":null}""";
        _sut.OnAuthStateChanged(userJson);
        _sut.OnAuthStateChanged(null);

        await Task.Delay(100);

        Assert.That(notificationCount, Is.EqualTo(2));
    }

    // ── ForceRefreshAsync ────────────────────────────────────────────────────

    [Test]
    [Description("ForceRefreshAsync must be a no-op when Clerk is not initialised.")]
    public async Task ForceRefreshAsync_NotInitialized_DoesNotNotify()
    {
        bool notified = false;
        _sut.AuthenticationStateChanged += _ => { notified = true; };

        await _sut.ForceRefreshAsync();

        Assert.That(notified, Is.False);
    }

    [Test]
    [Description("ForceRefreshAsync must notify subscribers after Clerk is initialised.")]
    public async Task ForceRefreshAsync_WhenInitialized_Notifies()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _clerkService.InitializeAsync("pk_test_key");

        bool notified = false;
        _sut.AuthenticationStateChanged += async stateTask =>
        {
            await stateTask;
            notified = true;
        };

        await _sut.ForceRefreshAsync();

        await Task.Delay(50);

        Assert.That(notified, Is.True);
    }
}
