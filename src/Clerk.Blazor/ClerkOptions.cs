namespace Clerk.Blazor;

/// <summary>
/// Configuration for Clerk.Blazor. Mirrors the browser-relevant props of
/// Clerk React's <c>&lt;ClerkProvider&gt;</c>.
/// </summary>
public sealed class ClerkOptions
{
    /// <summary>Clerk publishable key (<c>pk_test_…</c> or <c>pk_live_…</c>). Required.</summary>
    public string? PublishableKey { get; set; }

    /// <summary>Proxy URL used to reach the Clerk Frontend API (satellite/proxy setups).</summary>
    public string? ProxyUrl { get; set; }

    /// <summary>Satellite application domain.</summary>
    public string? Domain { get; set; }

    /// <summary>Whether this application is a satellite application.</summary>
    public bool IsSatellite { get; set; }

    /// <summary>ClerkJS version to load. Defaults to the version this library was verified against.</summary>
    public string ClerkJSVersion { get; set; } = ClerkConstants.ClerkJSVersion;

    /// <summary>Full URL of a self-hosted <c>clerk.browser.js</c>. Overrides <see cref="ClerkJSVersion"/>.</summary>
    public string? ClerkJSUrl { get; set; }

    /// <summary>CSP nonce applied to the injected ClerkJS script.</summary>
    public string? Nonce { get; set; }

    /// <summary>App route hosting <see cref="Components.SignIn"/>. Defaults to <c>/sign-in</c>.</summary>
    public string SignInUrl { get; set; } = "/sign-in";

    /// <summary>App route hosting <see cref="Components.SignUp"/>. Defaults to <c>/sign-up</c>.</summary>
    public string SignUpUrl { get; set; } = "/sign-up";

    /// <summary>App route hosting <see cref="Components.UserProfile"/>.</summary>
    public string UserProfileUrl { get; set; } = "/user";

    /// <summary>App route hosting <see cref="Components.OrganizationProfile"/>.</summary>
    public string OrganizationProfileUrl { get; set; } = "/organization";

    /// <summary>App route hosting <see cref="Components.CreateOrganization"/>.</summary>
    public string CreateOrganizationUrl { get; set; } = "/create-organization";

    /// <summary>App route hosting <see cref="Components.Waitlist"/>.</summary>
    public string WaitlistUrl { get; set; } = "/waitlist";

    /// <summary>App route hosting session task components (<c>/tasks</c>).</summary>
    public string TasksUrl { get; set; } = "/tasks";

    /// <summary>Redirect after sign-in when no redirect URL is present in the query string.</summary>
    public string SignInFallbackRedirectUrl { get; set; } = "/";

    /// <summary>Redirect after sign-up when no redirect URL is present in the query string.</summary>
    public string SignUpFallbackRedirectUrl { get; set; } = "/";

    /// <summary>Always redirect here after sign-in, ignoring the query string.</summary>
    public string? SignInForceRedirectUrl { get; set; }

    /// <summary>Always redirect here after sign-up, ignoring the query string.</summary>
    public string? SignUpForceRedirectUrl { get; set; }

    /// <summary>Redirect after signing out.</summary>
    public string AfterSignOutUrl { get; set; } = "/";

    /// <summary>Additional origins permitted as redirect targets.</summary>
    public List<string> AllowedRedirectOrigins { get; } = [];

    /// <summary>Allowed protocols for redirects (beyond http/https).</summary>
    public List<string> AllowedRedirectProtocols { get; } = [];

    /// <summary>
    /// When <c>true</c> (the default, matching Clerk Core 3), sessions with pending tasks
    /// are treated as signed out by auth helpers.
    /// </summary>
    public bool TreatPendingAsSignedOut { get; set; } = true;

    /// <summary>Opt out of Clerk telemetry.</summary>
    public bool TelemetryDisabled { get; set; }

    /// <summary>Allow ClerkJS to keep working from cached resources when the Frontend API is unavailable.</summary>
    public bool StandardBrowser { get; set; } = true;

    /// <summary>Whether to load the bundled stylesheet automatically. Defaults to <c>true</c>.</summary>
    public bool InjectStyles { get; set; } = true;

    /// <summary>Localized strings used by native components.</summary>
    public ClerkLocalization Localization { get; set; } = new();

    /// <summary>Theme and design tokens for native components (Clerk React's <c>appearance</c> prop).</summary>
    public ClerkAppearance Appearance { get; set; } = new();
}

public enum ClerkTheme { Auto, Light, Dark }

/// <summary>
/// Visual customization. Values map to CSS custom properties on every component root, so
/// you can also override <c>--cb-*</c> variables or target <c>cb-*</c> classes in your own CSS.
/// </summary>
public sealed class ClerkAppearance
{
    public ClerkTheme Theme { get; set; } = ClerkTheme.Auto;
    public string? ColorPrimary { get; set; }
    public string? ColorPrimaryForeground { get; set; }
    public string? ColorBackground { get; set; }
    public string? ColorForeground { get; set; }
    public string? ColorMuted { get; set; }
    public string? ColorBorder { get; set; }
    public string? ColorDanger { get; set; }
    public string? ColorSuccess { get; set; }
    public string? BorderRadius { get; set; }
    public string? FontFamily { get; set; }
    public string? FontSize { get; set; }
    /// <summary>Extra CSS class added to every component root.</summary>
    public string? RootClass { get; set; }

    internal string? ToStyle()
    {
        var parts = new List<string>();
        void Add(string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{name}:{value}"); }
        Add("--cb-primary", ColorPrimary);
        Add("--cb-primary-fg", ColorPrimaryForeground);
        Add("--cb-bg", ColorBackground);
        Add("--cb-fg", ColorForeground);
        Add("--cb-muted", ColorMuted);
        Add("--cb-border", ColorBorder);
        Add("--cb-danger", ColorDanger);
        Add("--cb-success", ColorSuccess);
        Add("--cb-radius", BorderRadius);
        Add("--cb-font", FontFamily);
        Add("--cb-font-size", FontSize);
        return parts.Count == 0 ? null : string.Join(';', parts);
    }

    internal string? ThemeAttribute => Theme switch { ClerkTheme.Light => "light", ClerkTheme.Dark => "dark", _ => null };
}

/// <summary>Version information.</summary>
public static class ClerkConstants
{
    /// <summary>The ClerkJS release the bridge has been audited against (headless, React-free).</summary>
    public const string ClerkJSVersion = "6.42.0";

    /// <summary>The Clerk React release used as the parity reference.</summary>
    public const string ClerkReactReferenceVersion = "6.18.3";
}
