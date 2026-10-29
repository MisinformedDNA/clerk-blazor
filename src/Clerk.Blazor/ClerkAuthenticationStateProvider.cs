using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Clerk.Blazor;

/// <summary>
/// <see cref="AuthenticationStateProvider"/> that mirrors <see cref="ClerkService.State"/>.
/// Registered automatically by <see cref="ClerkServiceCollectionExtensions.AddClerk"/>.
/// </summary>
public sealed class ClerkAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    public const string AuthenticationType = "Clerk";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ClerkService _clerk;
    private string? _lastKey;

    public ClerkAuthenticationStateProvider(ClerkService clerk)
    {
        _clerk = clerk;
        _clerk.StateChanged += OnStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(Build(_clerk.State));

    private void OnStateChanged(ClerkState state)
    {
        var key = $"{state.Auth.UserId}|{state.Auth.SessionId}|{state.Auth.OrgId}|{state.Auth.OrgRole}|{state.User?.UpdatedAt}";
        if (key == _lastKey) return;
        _lastKey = key;
        NotifyAuthenticationStateChanged(Task.FromResult(Build(state)));
    }

    /// <summary>Builds a principal from Clerk state. Pending sessions are anonymous when <see cref="ClerkOptions.TreatPendingAsSignedOut"/> is set.</summary>
    public static AuthenticationState Build(ClerkState state)
    {
        var auth = state.Auth;
        if (!auth.IsSignedIn || auth.UserId is null) return Anonymous;

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, auth.UserId), new("sub", auth.UserId) };
        if (auth.SessionId is not null) claims.Add(new Claim("sid", auth.SessionId));
        if (auth.SessionStatus is not null) claims.Add(new Claim("sts", auth.SessionStatus));

        var user = state.User;
        if (user is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));
            if (user.PrimaryEmailAddress?.EmailAddress is { } email) claims.Add(new Claim(ClaimTypes.Email, email));
            if (user.FirstName is { Length: > 0 } fn) claims.Add(new Claim(ClaimTypes.GivenName, fn));
            if (user.LastName is { Length: > 0 } ln) claims.Add(new Claim(ClaimTypes.Surname, ln));
            if (user.Username is { Length: > 0 } un) claims.Add(new Claim("username", un));
            if (user.ImageUrl is { Length: > 0 } img) claims.Add(new Claim("picture", img));
        }

        if (auth.OrgId is not null)
        {
            claims.Add(new Claim("org_id", auth.OrgId));
            if (auth.OrgSlug is not null) claims.Add(new Claim("org_slug", auth.OrgSlug));
            if (auth.OrgRole is not null)
            {
                claims.Add(new Claim("org_role", auth.OrgRole));
                claims.Add(new Claim(ClaimTypes.Role, auth.OrgRole));
            }
            foreach (var p in auth.OrgPermissions) claims.Add(new Claim("org_permission", p));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, ClaimTypes.Role)));
    }

    public void Dispose() => _clerk.StateChanged -= OnStateChanged;
}
