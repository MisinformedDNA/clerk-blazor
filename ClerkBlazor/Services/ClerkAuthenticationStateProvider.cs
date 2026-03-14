using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ClerkBlazor.Services;

/// <summary>
/// Blazor <see cref="AuthenticationStateProvider"/> backed by the Clerk
/// JavaScript SDK (via <see cref="ClerkAuthService"/>).
///
/// Register in <c>Program.cs</c> alongside the base class:
/// <code>
/// builder.Services.AddAuthorizationCore();
/// builder.Services.AddScoped&lt;ClerkAuthenticationStateProvider&gt;();
/// builder.Services.AddScoped&lt;AuthenticationStateProvider&gt;(
///     sp =&gt; sp.GetRequiredService&lt;ClerkAuthenticationStateProvider&gt;());
/// </code>
/// </summary>
public sealed class ClerkAuthenticationStateProvider
    : AuthenticationStateProvider, IAsyncDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ClerkAuthService _clerkService;
    private DotNetObjectReference<ClerkAuthenticationStateProvider>? _selfRef;

    public ClerkAuthenticationStateProvider(ClerkAuthService clerkService)
    {
        _clerkService = clerkService;
    }

    /// <inheritdoc/>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Return anonymous state immediately if Clerk has not been initialized
        // yet (e.g. during the initial render before OnAfterRenderAsync runs,
        // or when no publishable key is configured).
        if (!_clerkService.IsInitialized)
            return _anonymous;

        var user = await _clerkService.GetUserAsync();
        return user is null ? _anonymous : BuildAuthState(user);
    }

    /// <summary>
    /// Registers the Clerk auth-change listener so that authentication state
    /// is automatically refreshed when the Clerk session changes.
    /// Call this once after Clerk has been initialized (e.g. from
    /// <c>OnAfterRenderAsync</c> in <c>App.razor</c>).
    /// </summary>
    public async Task RegisterListenerAsync()
    {
        _selfRef = DotNetObjectReference.Create(this);
        await _clerkService.RegisterAuthListenerAsync(_selfRef);
    }

    /// <summary>
    /// Forces the <see cref="AuthenticationStateProvider"/> to re-fetch the
    /// current user from Clerk and notify all subscribers.
    /// Useful after a manual sign-in or sign-out flow.
    /// </summary>
    public async Task ForceRefreshAsync()
    {
        if (!_clerkService.IsInitialized)
            return;

        var user = await _clerkService.GetUserAsync();
        var state = user is null ? _anonymous : BuildAuthState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    /// <summary>
    /// Immediately notifies subscribers that the current user is anonymous.
    /// Call after <see cref="ClerkAuthService.SignOutAsync"/> completes.
    /// </summary>
    public void NotifySignOut()
        => NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));

    /// <summary>
    /// Invoked by the JavaScript <c>clerkInterop.onAuthChange</c> listener
    /// whenever the Clerk session changes.
    /// </summary>
    /// <param name="userJson">
    ///   JSON-serialised <see cref="ClerkUser"/>, or <c>null</c> when signed out.
    /// </param>
    [JSInvokable]
    public void OnAuthStateChanged(string? userJson)
    {
        AuthenticationState state;

        if (string.IsNullOrWhiteSpace(userJson))
        {
            state = _anonymous;
        }
        else
        {
            var user = JsonSerializer.Deserialize<ClerkUser>(userJson, _jsonOptions);
            state = user is null ? _anonymous : BuildAuthState(user);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static AuthenticationState BuildAuthState(ClerkUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));

        if (!string.IsNullOrWhiteSpace(user.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

        // Use e-mail as the display name if available, otherwise fall back to id.
        var displayName = user.Email ?? user.Id;
        claims.Add(new Claim(ClaimTypes.Name, displayName));

        var identity = new ClaimsIdentity(claims, authenticationType: "Clerk");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
