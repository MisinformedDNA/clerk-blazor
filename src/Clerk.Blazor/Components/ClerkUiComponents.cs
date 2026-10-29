using Microsoft.AspNetCore.Components;

namespace Clerk.Blazor.Components;

/// <summary>Clerk's official sign-in UI, mounted inside a Blazor-owned container.</summary>
public sealed class SignIn : ClerkUiComponentBase
{
    protected override string ComponentName => "SignIn";
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Path;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? FallbackRedirectUrl { get; set; }
    [Parameter] public string? ForceRedirectUrl { get; set; }
    [Parameter] public string? InitialIdentifier { get; set; }
    [Parameter] public string? SignUpUrl { get; set; }
    [Parameter] public bool WithSignUp { get; set; }
    [Parameter] public bool Transferable { get; set; } = true;
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("routing", Routing == ClerkRouting.Path ? "path" : "hash"),
        ("path", Routing == ClerkRouting.Path ? Path ?? Clerk.Options.SignInUrl : null),
        ("fallbackRedirectUrl", FallbackRedirectUrl), ("forceRedirectUrl", ForceRedirectUrl),
        ("initialValues", InitialIdentifier is null ? null : new { identifier = InitialIdentifier }),
        ("signUpUrl", SignUpUrl ?? Clerk.Options.SignUpUrl), ("withSignUp", WithSignUp), ("transferable", Transferable));
}

public sealed class SignUp : ClerkUiComponentBase
{
    protected override string ComponentName => "SignUp";
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Path;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? FallbackRedirectUrl { get; set; }
    [Parameter] public string? ForceRedirectUrl { get; set; }
    [Parameter] public string? SignInUrl { get; set; }
    [Parameter] public object? UnsafeMetadata { get; set; }
    [Parameter] public string? InitialEmailAddress { get; set; }
    [Parameter] public string? InitialPhoneNumber { get; set; }
    [Parameter] public string? InitialUsername { get; set; }
    [Parameter] public string? InitialFirstName { get; set; }
    [Parameter] public string? InitialLastName { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("routing", Routing == ClerkRouting.Path ? "path" : "hash"),
        ("path", Routing == ClerkRouting.Path ? Path ?? Clerk.Options.SignUpUrl : null),
        ("fallbackRedirectUrl", FallbackRedirectUrl), ("forceRedirectUrl", ForceRedirectUrl),
        ("signInUrl", SignInUrl ?? Clerk.Options.SignInUrl), ("unsafeMetadata", UnsafeMetadata),
        ("initialValues", ClerkArgs.Of(("emailAddress", InitialEmailAddress), ("phoneNumber", InitialPhoneNumber),
            ("username", InitialUsername), ("firstName", InitialFirstName), ("lastName", InitialLastName))));
}

public sealed class UserAvatar : ClerkUiComponentBase
{
    protected override string ComponentName => "UserAvatar";
    [Parameter] public bool Rounded { get; set; } = true;
    protected override Dictionary<string, object?> ComponentProps() => new() { ["rounded"] = Rounded };
}

public sealed class UserButton : ClerkUiComposedComponent
{
    protected override string ComponentName => "UserButton";
    protected override string? ProfilePropsKey => "userProfileProps";
    [Parameter] public bool ShowName { get; set; }
    [Parameter] public bool DefaultOpen { get; set; }
    [Parameter] public ClerkUserProfileMode UserProfileMode { get; set; }
    [Parameter] public string? UserProfileUrl { get; set; }
    [Parameter] public string? SignInUrl { get; set; }
    [Parameter] public RenderFragment? MenuItems { get; set; }
    [Parameter] public RenderFragment? UserProfilePages { get; set; }
    protected override RenderFragment? ExtraDeclarations => builder =>
    {
        builder.AddContent(0, MenuItems);
        builder.AddContent(1, UserProfilePages);
    };
    protected override Dictionary<string, object?> ComponentProps() => Compose(ClerkArgs.Of(
        ("showName", ShowName), ("defaultOpen", DefaultOpen),
        ("userProfileMode", UserProfileMode == ClerkUserProfileMode.Modal ? "modal" : "navigation"),
        ("userProfileUrl", UserProfileMode == ClerkUserProfileMode.Navigation ? UserProfileUrl ?? Clerk.Options.UserProfileUrl : null),
        ("signInUrl", SignInUrl ?? Clerk.Options.SignInUrl)));
}

public sealed class UserProfile : ClerkUiComposedComponent
{
    protected override string ComponentName => "UserProfile";
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Hash;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? InitialPage { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => Compose(ClerkArgs.Of(
        ("routing", Routing == ClerkRouting.Path ? "path" : "hash"),
        ("path", Routing == ClerkRouting.Path ? Path ?? Clerk.Options.UserProfileUrl : null),
        ("__experimental_startPath", InitialPage)));
}

public sealed class OrganizationProfile : ClerkUiComposedComponent
{
    protected override string ComponentName => "OrganizationProfile";
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Hash;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? AfterLeaveOrganizationUrl { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => Compose(ClerkArgs.Of(
        ("routing", Routing == ClerkRouting.Path ? "path" : "hash"),
        ("path", Routing == ClerkRouting.Path ? Path ?? Clerk.Options.OrganizationProfileUrl : null),
        ("afterLeaveOrganizationUrl", AfterLeaveOrganizationUrl)));
}

public enum ClerkOrganizationMode { Modal, Navigation }

public sealed class OrganizationSwitcher : ClerkUiComposedComponent
{
    protected override string ComponentName => "OrganizationSwitcher";
    protected override string? ProfilePropsKey => "organizationProfileProps";
    [Parameter] public bool HidePersonal { get; set; }
    [Parameter] public bool DefaultOpen { get; set; }
    [Parameter] public string? AfterSelectOrganizationUrl { get; set; }
    [Parameter] public string? AfterSelectPersonalUrl { get; set; }
    [Parameter] public string? AfterLeaveOrganizationUrl { get; set; }
    [Parameter] public string? AfterCreateOrganizationUrl { get; set; }
    [Parameter] public ClerkOrganizationMode OrganizationProfileMode { get; set; }
    [Parameter] public ClerkOrganizationMode CreateOrganizationMode { get; set; }
    [Parameter] public string? OrganizationProfileUrl { get; set; }
    [Parameter] public string? CreateOrganizationUrl { get; set; }
    [Parameter] public RenderFragment? OrganizationProfilePages { get; set; }
    protected override RenderFragment? ExtraDeclarations => OrganizationProfilePages;
    protected override Dictionary<string, object?> ComponentProps() => Compose(ClerkArgs.Of(
        ("hidePersonal", HidePersonal), ("defaultOpen", DefaultOpen),
        ("afterSelectOrganizationUrl", AfterSelectOrganizationUrl), ("afterSelectPersonalUrl", AfterSelectPersonalUrl),
        ("afterLeaveOrganizationUrl", AfterLeaveOrganizationUrl), ("afterCreateOrganizationUrl", AfterCreateOrganizationUrl),
        ("organizationProfileMode", OrganizationProfileMode == ClerkOrganizationMode.Modal ? "modal" : "navigation"),
        ("createOrganizationMode", CreateOrganizationMode == ClerkOrganizationMode.Modal ? "modal" : "navigation"),
        ("organizationProfileUrl", OrganizationProfileMode == ClerkOrganizationMode.Navigation ? OrganizationProfileUrl ?? Clerk.Options.OrganizationProfileUrl : null),
        ("createOrganizationUrl", CreateOrganizationMode == ClerkOrganizationMode.Navigation ? CreateOrganizationUrl ?? Clerk.Options.CreateOrganizationUrl : null)));
}

public sealed class OrganizationList : ClerkUiComponentBase
{
    protected override string ComponentName => "OrganizationList";
    [Parameter] public bool HidePersonal { get; set; }
    [Parameter] public string? AfterSelectOrganizationUrl { get; set; }
    [Parameter] public string? AfterSelectPersonalUrl { get; set; }
    [Parameter] public string? AfterCreateOrganizationUrl { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("hidePersonal", HidePersonal), ("afterSelectOrganizationUrl", AfterSelectOrganizationUrl),
        ("afterSelectPersonalUrl", AfterSelectPersonalUrl), ("afterCreateOrganizationUrl", AfterCreateOrganizationUrl));
}

public sealed class CreateOrganization : ClerkUiComponentBase
{
    protected override string ComponentName => "CreateOrganization";
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Path;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? AfterCreateOrganizationUrl { get; set; }
    [Parameter] public bool? SkipInvitationScreen { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("routing", Routing == ClerkRouting.Path ? "path" : "hash"),
        ("path", Routing == ClerkRouting.Path ? Path ?? Clerk.Options.CreateOrganizationUrl : null),
        ("afterCreateOrganizationUrl", AfterCreateOrganizationUrl), ("skipInvitationScreen", SkipInvitationScreen));
}

public sealed class PricingTable : ClerkUiComponentBase
{
    protected override string ComponentName => "PricingTable";
    [Parameter] public string For { get; set; } = "user";
    [Parameter] public string? NewSubscriptionRedirectUrl { get; set; }
    [Parameter] public string CtaPosition { get; set; } = "bottom";
    [Parameter] public bool CollapseFeatures { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("for", For), ("newSubscriptionRedirectUrl", NewSubscriptionRedirectUrl),
        ("ctaPosition", CtaPosition), ("collapseFeatures", CollapseFeatures));
}

public sealed class APIKeys : ClerkUiComponentBase
{
    protected override string ComponentName => "APIKeys";
    [Parameter] public int PerPage { get; set; } = 10;
    [Parameter] public bool ShowDescription { get; set; }
    protected override Dictionary<string, object?> ComponentProps() =>
        new() { ["perPage"] = PerPage, ["showDescription"] = ShowDescription };
}

public sealed class Waitlist : ClerkUiComponentBase
{
    protected override string ComponentName => "Waitlist";
    [Parameter] public string? AfterJoinWaitlistUrl { get; set; }
    [Parameter] public string? SignInUrl { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("afterJoinWaitlistUrl", AfterJoinWaitlistUrl), ("signInUrl", SignInUrl ?? Clerk.Options.SignInUrl));
}

public sealed class OAuthConsent : ClerkUiComponentBase
{
    protected override string ComponentName => "OAuthConsent";
    [Parameter] public string? ClientId { get; set; }
    [Parameter] public string? Scope { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(("oauthClientId", ClientId), ("scope", Scope));
}

public abstract class ClerkTaskComponent : ClerkUiComponentBase
{
    [Parameter] public string? RedirectUrlComplete { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => new()
    {
        ["redirectUrlComplete"] = RedirectUrlComplete ?? Clerk.Options.SignInFallbackRedirectUrl,
    };
}

public sealed class TaskChooseOrganization : ClerkTaskComponent { protected override string ComponentName => "TaskChooseOrganization"; }
public sealed class TaskResetPassword : ClerkTaskComponent { protected override string ComponentName => "TaskResetPassword"; }
public sealed class TaskSetupMfa : ClerkTaskComponent { protected override string ComponentName => "TaskSetupMFA"; }
