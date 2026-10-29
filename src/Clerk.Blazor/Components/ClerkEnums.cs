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
    /// <summary>Steps are held in component state only.</summary>
    Virtual,
}
