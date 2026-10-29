using System.Text.Json;

namespace Clerk.Blazor;

/// <summary>Reverification presets accepted by <see cref="ClerkCheck.Reverification"/>.</summary>
public enum ReverificationPreset
{
    StrictMfa,
    Strict,
    Moderate,
    Lax,
}

/// <summary>Required factor level for reverification.</summary>
public enum ReverificationLevel
{
    FirstFactor,
    SecondFactor,
    MultiFactor,
}

/// <summary>Custom reverification requirement.</summary>
public sealed record ReverificationRequirement(ReverificationLevel Level, int AfterMinutes)
{
    public static ReverificationRequirement FromPreset(ReverificationPreset preset) => preset switch
    {
        ReverificationPreset.StrictMfa => new(ReverificationLevel.MultiFactor, 10),
        ReverificationPreset.Strict => new(ReverificationLevel.SecondFactor, 10),
        ReverificationPreset.Moderate => new(ReverificationLevel.SecondFactor, 60),
        _ => new(ReverificationLevel.SecondFactor, 1440),
    };
}

/// <summary>
/// Authorization check, equivalent to the argument of Clerk React's <c>has()</c>
/// and the <c>when</c> object of <c>&lt;Show&gt;</c>.
/// </summary>
public sealed record ClerkCheck
{
    public string? Role { get; init; }
    public string? Permission { get; init; }
    public string? Feature { get; init; }
    public string? Plan { get; init; }
    public ReverificationRequirement? Reverification { get; init; }
}

/// <summary>
/// Authentication values, equivalent to the return value of Clerk React's <c>useAuth()</c>.
/// </summary>
public sealed record ClerkAuth
{
    public bool IsLoaded { get; init; }
    public bool IsSignedIn { get; init; }
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? SessionStatus { get; init; }
    public string? OrgId { get; init; }
    public string? OrgRole { get; init; }
    public string? OrgSlug { get; init; }
    public IReadOnlyList<string> OrgPermissions { get; init; } = [];
    public JsonElement? Actor { get; init; }
    public JsonElement? SessionClaims { get; init; }
    public int[]? FactorVerificationAge { get; init; }

    /// <summary>Checks roles, permissions, features, plans and reverification freshness.</summary>
    public bool Has(ClerkCheck check)
    {
        if (!IsSignedIn) return false;
        var results = new List<bool>();

        if (check.Role is not null)
            results.Add(OrgId is not null && string.Equals(OrgRole, PrefixOrg(check.Role), StringComparison.Ordinal));
        if (check.Permission is not null)
            results.Add(OrgId is not null && OrgPermissions.Contains(PrefixOrg(check.Permission)));
        if (check.Feature is not null)
            results.Add(HasEntitlement("fea", check.Feature));
        if (check.Plan is not null)
            results.Add(HasEntitlement("pla", check.Plan));
        if (check.Reverification is not null)
            results.Add(IsFresh(check.Reverification));

        return results.Count > 0 && results.All(r => r);
    }

    /// <summary>Shorthand for <see cref="Has(ClerkCheck)"/>.</summary>
    public bool Has(string? role = null, string? permission = null, string? feature = null, string? plan = null) =>
        Has(new ClerkCheck { Role = role, Permission = permission, Feature = feature, Plan = plan });

    private static string PrefixOrg(string value) => value.StartsWith("org:", StringComparison.Ordinal) ? value : "org:" + value;

    private bool HasEntitlement(string claim, string value)
    {
        if (SessionClaims is not { ValueKind: JsonValueKind.Object } claims ||
            !claims.TryGetProperty(claim, out var raw) || raw.ValueKind != JsonValueKind.String)
            return false;

        string[] scopes;
        string name;
        if (value.StartsWith("org:", StringComparison.Ordinal)) { scopes = ["o"]; name = value[4..]; }
        else if (value.StartsWith("user:", StringComparison.Ordinal)) { scopes = ["u"]; name = value[5..]; }
        else { scopes = ["o", "u"]; name = value; }

        foreach (var entry in raw.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(':', 2);
            if (parts.Length != 2) continue;
            if (parts[1] == name && parts[0].Any(c => scopes.Contains(c.ToString())))
                return true;
        }
        return false;
    }

    private bool IsFresh(ReverificationRequirement req)
    {
        if (FactorVerificationAge is not { Length: 2 } age) return false;
        var (first, second) = (age[0], age[1]);
        var after = req.AfterMinutes;
        return req.Level switch
        {
            ReverificationLevel.FirstFactor => first != -1 && after > first,
            ReverificationLevel.SecondFactor => second != -1 ? after > second : first != -1 && after > first,
            _ => second == -1 ? first != -1 && after > first : after > first && after > second,
        };
    }
}

/// <summary>
/// Immutable snapshot of everything ClerkJS knows, published on every change.
/// Cascaded by <see cref="Components.ClerkProvider"/>.
/// </summary>
public sealed record ClerkState
{
    public static readonly ClerkState Initial = new();

    public ClerkStatus Status { get; init; } = ClerkStatus.Loading;
    public bool IsLoaded { get; init; }
    public string? ClerkJSVersion { get; init; }
    public ClerkClient? Client { get; init; }
    public ClerkSession? Session { get; init; }
    public ClerkUser? User { get; init; }
    public ClerkOrganization? Organization { get; init; }
    public ClerkOrganizationMembership? Membership { get; init; }
    public ClerkEnvironment? Environment { get; init; }
    public ClerkAuth Auth { get; init; } = new();
    public Exception? Error { get; init; }

    /// <summary>Sessions that can be switched to (<c>useSessionList</c>).</summary>
    public IReadOnlyList<ClerkSession> Sessions =>
        Client?.Sessions.Where(s => s.Status is "active" or "pending").ToList() ?? [];

    internal static ClerkState From(ClerkSnapshot s, bool treatPendingAsSignedOut)
    {
        var status = s.Status switch
        {
            "ready" => ClerkStatus.Ready,
            "degraded" => ClerkStatus.Degraded,
            "error" => ClerkStatus.Error,
            _ => s.Loaded ? ClerkStatus.Ready : ClerkStatus.Loading,
        };
        var session = s.Session;
        var signedIn = session is not null && (session.Status == "active" || (!treatPendingAsSignedOut && session.IsPending));
        var membership = s.Membership;

        JsonElement? claims = session?.Claims;
        var auth = new ClerkAuth
        {
            IsLoaded = s.Loaded,
            IsSignedIn = signedIn,
            UserId = signedIn ? s.User?.Id ?? session!.User?.Id : null,
            SessionId = signedIn ? session!.Id : null,
            SessionStatus = session?.Status,
            OrgId = signedIn ? s.Organization?.Id : null,
            OrgRole = signedIn ? membership?.Role : null,
            OrgSlug = signedIn ? s.Organization?.Slug : null,
            OrgPermissions = signedIn ? membership?.Permissions ?? [] : [],
            Actor = signedIn ? session!.Actor : null,
            SessionClaims = signedIn ? claims : null,
            FactorVerificationAge = signedIn ? session!.FactorVerificationAge : null,
        };

        return new ClerkState
        {
            Status = status,
            IsLoaded = s.Loaded,
            ClerkJSVersion = s.Version,
            Client = s.Client,
            Session = session,
            User = s.User,
            Organization = s.Organization,
            Membership = membership,
            Environment = s.Environment,
            Auth = auth,
        };
    }
}
