using Microsoft.AspNetCore.Components;

namespace Clerk.Blazor.Components;

/// <summary>Base for <c>RedirectTo*</c> components: navigates once Clerk has loaded.</summary>
public abstract class ClerkRedirectBase : ClerkComponentBase
{
    private bool _done;

    protected abstract void Redirect();

    protected virtual bool ShouldRedirect => true;

    protected override void OnClerkStateChanged(ClerkState state) => TryRedirect();

    protected override void OnAfterRender(bool firstRender) => TryRedirect();

    private void TryRedirect()
    {
        if (_done || !State.IsLoaded || !ShouldRedirect) return;
        _done = true;
        Redirect();
    }
}

/// <summary>Redirects to the sign-in page, returning to the current page afterwards (<c>&lt;RedirectToSignIn /&gt;</c>).</summary>
public sealed class RedirectToSignIn : ClerkRedirectBase
{
    [Parameter] public string? RedirectUrl { get; set; }
    protected override void Redirect() => Clerk.RedirectToSignIn(RedirectUrl);
}

public sealed class RedirectToSignUp : ClerkRedirectBase
{
    [Parameter] public string? RedirectUrl { get; set; }
    protected override void Redirect() => Clerk.RedirectToSignUp(RedirectUrl);
}

public sealed class RedirectToUserProfile : ClerkRedirectBase
{
    protected override void Redirect() => Clerk.RedirectToUserProfile();
}

public sealed class RedirectToOrganizationProfile : ClerkRedirectBase
{
    protected override void Redirect() => Clerk.RedirectToOrganizationProfile();
}

public sealed class RedirectToCreateOrganization : ClerkRedirectBase
{
    protected override void Redirect() => Clerk.RedirectToCreateOrganization();
}

/// <summary>Redirects to the page for the current pending session task (<c>&lt;RedirectToTasks /&gt;</c>).</summary>
public sealed class RedirectToTasks : ClerkRedirectBase
{
    protected override bool ShouldRedirect => State.Session?.CurrentTask is not null;
    protected override void Redirect() => Clerk.RedirectToTasks();
}
