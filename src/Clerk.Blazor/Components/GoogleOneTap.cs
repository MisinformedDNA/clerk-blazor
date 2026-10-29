using Microsoft.AspNetCore.Components;

namespace Clerk.Blazor.Components;

/// <summary>Opens Clerk's Google One Tap integration and closes it when disposed.</summary>
public sealed class GoogleOneTap : ClerkUiComponentBase
{
    protected override string ComponentName => "GoogleOneTap";
    [Parameter] public bool CancelOnTapOutside { get; set; } = true;
    [Parameter] public bool ItpSupport { get; set; } = true;
    [Parameter] public bool FedCmSupport { get; set; } = true;
    [Parameter] public string? SignInForceRedirectUrl { get; set; }
    [Parameter] public string? SignUpForceRedirectUrl { get; set; }
    protected override Dictionary<string, object?> ComponentProps() => ClerkArgs.Of(
        ("cancelOnTapOutside", CancelOnTapOutside), ("itpSupport", ItpSupport), ("fedCmSupport", FedCmSupport),
        ("signInForceRedirectUrl", SignInForceRedirectUrl), ("signUpForceRedirectUrl", SignUpForceRedirectUrl));
}
