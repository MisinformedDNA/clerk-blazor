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

    /// <summary>Load Clerk's official UI (and its bundled React runtime). Disable for a service-only/headless integration.</summary>
    public bool LoadUserInterface { get; set; } = true;

    /// <summary>Pinned Clerk UI release, compatible with the default ClerkJS release.</summary>
    public string ClerkUIVersion { get; set; } = ClerkConstants.ClerkUIVersion;

    /// <summary>Optional self-hosted URL of ui.browser.js, including access to its adjacent chunks.</summary>
    public string? ClerkUIUrl { get; set; }

    /// <summary>Clerk's appearance object: variables, elements, layout and per-component overrides.</summary>
    public Dictionary<string, object?>? UIAppearance { get; set; }

    /// <summary>Clerk's nested localization object, passed directly to Clerk UI.</summary>
    public Dictionary<string, object?>? UILocalization { get; set; }

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

    /// <summary>Localized strings used by Blazor-owned trigger buttons and headless custom UI.</summary>
    public ClerkLocalization Localization { get; set; } = new();

    /// <summary>Legacy design tokens, translated into Clerk UI variables. UIAppearance takes precedence.</summary>
    public ClerkAppearance Appearance { get; set; } = new();
}

public enum ClerkTheme { Auto, Light, Dark }

/// <summary>
/// Legacy visual customization, translated into Clerk UI appearance variables.
/// Prefer <see cref="ClerkOptions.UIAppearance"/> for full upstream customization.
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
    /// <summary>Extra class for legacy Blazor-owned status elements, not Clerk UI internals.</summary>
    public string? RootClass { get; set; }

    internal Dictionary<string, object?> ToClerkAppearance()
    {
        var variables = new Dictionary<string, object?>();
        void Add(string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) variables[name] = value; }
        if (Theme == ClerkTheme.Dark)
        {
            variables["colorBackground"] = "#212126";
            variables["colorForeground"] = "#ffffff";
            variables["colorInput"] = "#27272a";
            variables["colorInputForeground"] = "#ffffff";
            variables["colorMutedForeground"] = "#a1a1aa";
            variables["colorPrimary"] = "#ffffff";
            variables["colorPrimaryForeground"] = "#18181b";
        }
        Add("colorPrimary", ColorPrimary);
        Add("colorPrimaryForeground", ColorPrimaryForeground);
        Add("colorBackground", ColorBackground);
        Add("colorForeground", ColorForeground);
        Add("colorMutedForeground", ColorMuted);
        Add("colorBorder", ColorBorder);
        Add("colorDanger", ColorDanger);
        Add("colorSuccess", ColorSuccess);
        Add("borderRadius", BorderRadius);
        Add("fontFamily", FontFamily);
        Add("fontSize", FontSize);
        return new() { ["variables"] = variables };
    }

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
    /// <summary>The ClerkJS release used by the bridge.</summary>
    public const string ClerkJSVersion = "6.42.0";

    public const string ClerkUIVersion = "1.42.1";
    /// <summary>The Clerk React release used as the parity reference.</summary>
    public const string ClerkReactReferenceVersion = "6.18.3";
}
