using Microsoft.AspNetCore.Components;

namespace Clerk.Blazor.Components;

/// <summary>Shared behavior for <see cref="SignIn"/>, <see cref="SignUp"/> and task components.</summary>
public abstract class ClerkAuthFlowBase : ClerkComponentBase
{
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Path;
    /// <summary>Destination after completion when no <c>redirect_url</c> is present.</summary>
    [Parameter] public string? FallbackRedirectUrl { get; set; }
    /// <summary>Destination after completion, overriding <c>redirect_url</c>.</summary>
    [Parameter] public string? ForceRedirectUrl { get; set; }
    /// <summary>Raised when the flow completes and the session is active. In modal mode the dialog closes.</summary>
    [Parameter] public EventCallback OnComplete { get; set; }

    protected abstract string? OptionsForceRedirect { get; }
    protected abstract string OptionsFallbackRedirect { get; }

    protected string? QueryParam(string name)
    {
        var query = Navigation.ToAbsoluteUri(Navigation.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var key = Uri.UnescapeDataString((idx < 0 ? pair : pair[..idx]).Replace('+', ' '));
            if (key == name) return idx < 0 ? "" : Uri.UnescapeDataString(pair[(idx + 1)..].Replace('+', ' '));
        }
        return null;
    }

    /// <summary>Last path segment of the current URL, used for path routing (e.g. <c>sso-callback</c>).</summary>
    protected string? SubPath(string baseUrl)
    {
        if (Routing != ClerkRouting.Path) return null;
        var path = Navigation.ToAbsoluteUri(Navigation.Uri).AbsolutePath.TrimEnd('/');
        var basePath = Navigation.ToAbsoluteUri(Navigation.BaseUri).AbsolutePath.TrimEnd('/') + "/" + baseUrl.Trim('/');
        return path.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase) ? path[(basePath.Length + 1)..] : null;
    }

    protected string Absolute(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var abs) ? abs.ToString() : Navigation.ToAbsoluteUri(url.TrimStart('/')).ToString();

    /// <summary>Where to go after completing the flow.</summary>
    protected string AfterUrl =>
        ForceRedirectUrl ?? OptionsForceRedirect
        ?? (QueryParam("redirect_url") is { Length: > 0 } r ? Clerk.ResolveRedirect(r, FallbackRedirectUrl ?? OptionsFallbackRedirect) : null)
        ?? FallbackRedirectUrl ?? OptionsFallbackRedirect;

    /// <summary>Activates the created session and navigates (or resolves the pending session task).</summary>
    protected async Task CompleteAsync(string? sessionId)
    {
        if (sessionId is null) return;
        await Clerk.SetActiveAsync(new SetActiveOptions { SessionId = sessionId, RedirectUrl = AfterUrl });
        await OnComplete.InvokeAsync();
    }
}
