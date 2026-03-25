using System.Text.Json;
using Clerk.Blazor.Services;

namespace Clerk.Blazor.Tests;

/// <summary>
/// Unit tests for <see cref="ClerkAuthService"/>.
/// These tests verify the C# service layer without requiring a running server
/// or browser, using a <see cref="FakeJSRuntime"/>.
/// </summary>
[TestFixture]
public class ClerkAuthServiceTests
{
    private FakeJSRuntime _js = null!;
    private ClerkAuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _js = new FakeJSRuntime();
        _sut = new ClerkAuthService(_js);
    }

    // ── IsInitialized ────────────────────────────────────────────────────────

    [Test]
    [Description("IsInitialized must be false before InitializeAsync is called.")]
    public void IsInitialized_BeforeInit_IsFalse()
        => Assert.That(_sut.IsInitialized, Is.False);

    [Test]
    [Description("IsInitialized must be true after InitializeAsync completes.")]
    public async Task IsInitialized_AfterInit_IsTrue()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");
        Assert.That(_sut.IsInitialized, Is.True);
    }

    [Test]
    [Description("Calling InitializeAsync a second time must be a no-op (single JS call).")]
    public async Task InitializeAsync_CalledTwice_CallsJsOnlyOnce()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");
        await _sut.InitializeAsync("pk_test_key");

        Assert.That(
            _js.Calls.Count(c => c.Identifier == "clerkInterop.initialize"),
            Is.EqualTo(1));
    }

    // ── SignInAsync ──────────────────────────────────────────────────────────

    [Test]
    [Description("SignInAsync must throw when Clerk is not initialized.")]
    public void SignInAsync_NotInitialized_Throws()
        => Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SignInAsync());

    [Test]
    [Description("SignInAsync must invoke clerkInterop.openSignIn when initialized.")]
    public async Task SignInAsync_WhenInitialized_CallsOpenSignIn()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");

        await _sut.SignInAsync();

        Assert.That(
            _js.Calls.Any(c => c.Identifier == "clerkInterop.openSignIn"),
            Is.True);
    }

    // ── SignOutAsync ─────────────────────────────────────────────────────────

    [Test]
    [Description("SignOutAsync must throw when Clerk is not initialized.")]
    public void SignOutAsync_NotInitialized_Throws()
        => Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SignOutAsync());

    [Test]
    [Description("SignOutAsync must invoke clerkInterop.signOut when initialized.")]
    public async Task SignOutAsync_WhenInitialized_CallsSignOut()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");

        await _sut.SignOutAsync();

        Assert.That(
            _js.Calls.Any(c => c.Identifier == "clerkInterop.signOut"),
            Is.True);
    }

    // ── GetUserAsync ─────────────────────────────────────────────────────────

    [Test]
    [Description("GetUserAsync must throw when Clerk is not initialized.")]
    public void GetUserAsync_NotInitialized_Throws()
        => Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetUserAsync());

    [Test]
    [Description("GetUserAsync must return null when JS returns null.")]
    public async Task GetUserAsync_JsReturnsNull_ReturnsNull()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");

        // FakeJSRuntime returns default(JsonElement?) = null for unconfigured keys.
        var user = await _sut.GetUserAsync();

        Assert.That(user, Is.Null);
    }

    [Test]
    [Description("GetUserAsync must deserialize the JS user object into a ClerkUser.")]
    public async Task GetUserAsync_JsReturnsUserJson_ReturnsDeserializedUser()
    {
        _js.SetResult("clerkInterop.initialize", true);
        await _sut.InitializeAsync("pk_test_key");

        // Simulate the JSON element that JS interop returns.
        var jsonDoc = JsonDocument.Parse("""
            {
                "id": "user_test123",
                "email": "test@example.com",
                "firstName": "Test",
                "lastName": "User",
                "imageUrl": "https://example.com/avatar.png"
            }
            """);
        _js.SetResult("clerkInterop.getUser", jsonDoc.RootElement);

        var user = await _sut.GetUserAsync();

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Id, Is.EqualTo("user_test123"));
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
        Assert.That(user.FirstName, Is.EqualTo("Test"));
        Assert.That(user.LastName, Is.EqualTo("User"));
        Assert.That(user.ImageUrl, Is.EqualTo("https://example.com/avatar.png"));
    }
}
