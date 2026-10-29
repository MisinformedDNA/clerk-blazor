namespace Clerk.Blazor.Components;

public enum ClerkButtonMode
{
    Redirect,
    Modal,
}

/// <summary>How multi-step components track their current step.</summary>
public enum ClerkRouting
{
    /// <summary>Steps are reflected in the URL path (e.g. <c>/sign-in/factor-one</c>). Requires a catch-all route.</summary>
    Path,
    /// <summary>Clerk tracks steps in the URL fragment. Does not require catch-all app routes.</summary>
    Hash,
    /// <summary>Compatibility alias. Official embedded Clerk UI uses hash routing instead of native virtual routing.</summary>
    Virtual = Hash,
}
