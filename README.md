# Clerk.Blazor

Clerk authentication for Blazor WebAssembly: Razor component wrappers around **Clerk's official UI**, plus C# services, reactive state and Blazor authorization.

The application does not need React source code, npm or a JavaScript build step. However, **Clerk UI downloads and runs its own bundled React runtime**. The library no longer recreates Clerk's forms, menus, profiles or payment UI in Razor.

## Quick start

Reference `src\Clerk.Blazor\Clerk.Blazor.csproj` (or your packaged build), then configure the client:

```csharp
using Clerk.Blazor;

builder.Services.AddAuthorizationCore();
builder.Services.AddClerk(options =>
{
    options.PublishableKey = builder.Configuration["Clerk:PublishableKey"];
    options.SignInUrl = "/sign-in";
    options.SignUpUrl = "/sign-up";
});
```

In `_Imports.razor`:

```razor
@using Clerk.Blazor
@using Clerk.Blazor.Components
@using Microsoft.AspNetCore.Components.Authorization
```

Wrap the application once:

```razor
<ClerkProvider>
    <CascadingAuthenticationState>
        @* Router, layouts and app content *@
    </CascadingAuthenticationState>
</ClerkProvider>
```

Trigger a sign-in modal from anywhere:

```razor
<SignedOut>
    <SignInButton Mode="ClerkButtonMode.Modal">Sign in</SignInButton>
</SignedOut>
<SignedIn>
    <UserButton ShowName />
    <OrganizationSwitcher />
    <SignOutButton />
</SignedIn>
```

All modal triggers use the same Clerk instance. They do not create local Blazor dialogs; opening another modal closes existing Clerk modal entry points first.

## Embedded components and routing

For path routing, expose a catch-all route so Clerk's multi-step flow remains mounted:

```razor
@page "/sign-in"
@page "/sign-in/{*Path}"

<SignIn />

@code {
    [Parameter] public string? Path { get; set; }
}
```

Alternatively, `<SignIn Routing="ClerkRouting.Hash" />` uses the URL fragment for its steps. This is useful inside an unauthorized view without navigating to a separate sign-in page. The old `Virtual` enum value is a compatibility alias for `Hash`; it no longer implements a separate Razor router.

The sample uses a modal in the navigation and inline sign-in on unauthorized pages. Its sign-in/up routes are also available for callbacks and direct links.

For email-link callbacks, use `<AuthenticateWithRedirectCallback EmailLink="true" />`. Completion uses the sign-in force redirect, then the fallback redirect; an unfinished sign-in continues at `SecondFactorUrl` (default: `/sign-in/factor-two`). For sign-up email links, also set `EmailLinkSignUp="true"` to use sign-up redirect settings and `ContinueSignUpUrl` (default: `/sign-up/continue`). These continuation destinations are distinct from the destination after session activation.

## Components

| Area | Components |
|---|---|
| Auth | `SignIn`, `SignUp`, `SignInButton`, `SignUpButton`, `SignOutButton`, `SignInWithMetamaskButton`, `AuthenticateWithRedirectCallback` |
| User | `UserAvatar`, `UserButton`, `UserProfile` |
| Organizations | `OrganizationSwitcher`, `OrganizationList`, `OrganizationProfile`, `CreateOrganization` |
| Billing | `PricingTable`, `CheckoutButton`, `PlanDetailsButton`, `SubscriptionDetailsButton`, `BillingPage` |
| Other | `APIKeys`, `Waitlist`, `GoogleOneTap`, `OAuthConsent` |
| Session tasks | `TaskChooseOrganization`, `TaskResetPassword`, `TaskSetupMfa` |
| Blazor controls | `ClerkLoaded`, `ClerkLoading`, `ClerkDegraded`, `ClerkFailed`, `Show`, `SignedIn`, `SignedOut`, redirect components |

`BillingPage` is a convenience wrapper around the user or organization profile's billing section, not a separately recreated billing screen. Feature visibility and authentication flows are controlled by Clerk's instance configuration.

Mountable components accept `Class`, additional HTML attributes on the **host container**, `Appearance` and `Options`. `Options` accepts JSON-serializable upstream props using their JavaScript names:

```razor
<SignIn Options="@SignInOptions" />

@code {
    private readonly Dictionary<string, object?> SignInOptions = new()
    {
        ["withSignUp"] = true,
        ["oauthFlow"] = "popup",
    };
}
```

`Options` overrides named parameters; `Appearance` overrides `Options["appearance"]`. Ordinary authentication-state updates do not remount Clerk's UI. Changing component props remounts the affected island and may reset an in-progress form, so avoid changing those props during a flow.

## Appearance and localization

Use Clerk's appearance object instead of CSS intended for the old `cb-*` forms:

```csharp
options.UIAppearance = new()
{
    ["variables"] = new Dictionary<string, object?>
    {
        ["colorPrimary"] = "#4338ca",
        ["borderRadius"] = "0.5rem",
    },
    ["elements"] = new Dictionary<string, object?>
    {
        ["cardBox"] = new Dictionary<string, object?> { ["boxShadow"] = "none" },
    },
};
```

Per-component `Appearance` accepts the same shape. `UILocalization` accepts Clerk's nested localization resource. `Localization` remains available for the few Blazor-owned trigger labels; it is not automatically translated into Clerk's localization schema.

Legacy `Appearance` color/font/radius values are translated into Clerk UI variables when `UIAppearance` is absent. `ClerkTheme.Dark` maps a dark palette; `Auto` and `Light` leave the base palette to Clerk defaults. `RootClass` and the old `--cb-*` theme variables do not style Clerk's UI. Use `Class` for the host, or Clerk's appearance element selectors for the contents.

Keep application-wide heading, button and link rules narrowly scoped. Clerk mounts modals outside the Blazor component tree, so Blazor CSS isolation does not style those modals.

## Custom Razor pages and menu items

Razor content can be embedded through Clerk's custom-page mounting hooks:

```razor
<UserProfile>
    <UserProfilePage Label="Preferences" Url="preferences">
        <Icon><span aria-hidden="true">P</span></Icon>
        <ChildContent><Preferences /></ChildContent>
    </UserProfilePage>
    <UserProfileLink Label="Help" Url="/help" />
</UserProfile>
```

Use `UserButtonAction`/`UserButtonLink` inside `UserButton.MenuItems`, `UserProfilePage`/`UserProfileLink` inside `UserButton.UserProfilePages`, or the corresponding `OrganizationProfilePage`/`OrganizationProfileLink` inside organization profiles/switchers. Supply icons with the `Icon` fragment. The bridge moves only the Razor-owned slot element into Clerk's supplied container and restores it when unmounted.

## C# services and headless use

Inject `ClerkService` for `State`, `Auth`, `GetTokenAsync`, `SignOutAsync`, `SetActiveAsync`, authorization checks, and typed `User`, `Organization`, `SignIn`, `SignUp`, `Billing`, `ApiKeys`, `Waitlist`, `OAuth` and `Session` APIs.

```csharp
var token = await clerk.GetTokenAsync();
var canManage = clerk.Auth.Has(permission: "org:sys_memberships:manage");
await clerk.OpenUiAsync(ClerkUiModal.UserProfile);
```

`InvokeAsync` is the resource API escape hatch. UI lifecycle methods are deliberately routed through components and `OpenUiAsync`, not through resource invocation. Blazor authorization is for client-side UX; validate JWTs and enforce permissions in your backend.

To supply your own UI:

```csharp
options.LoadUserInterface = false;
```

This skips the Clerk UI script and constructor. Services and auth state remain available; built-in UI components report that UI is disabled. A custom `ReverificationHandler` can provide headless reverification. Wallet SDKs or other features you explicitly invoke may load their own dependencies; this is not a blanket zero-React guarantee.

## Loading and deployment

ClerkJS **6.42.0** and Clerk UI **1.42.1** are pinned. The bridge loads `clerk.browser.js` and `ui.browser.js` from the Frontend API host (or `ProxyUrl`) and passes the UI constructor into `Clerk.load`.

Configure `ClerkJSVersion`/`ClerkUIVersion` together when upgrading. `ClerkJSUrl`/`ClerkUIUrl` support self-hosted bundles; retain adjacent lazy-loaded chunks. `Nonce` is applied to both script tags. Allow the configured Clerk asset/API hosts in your CSP, plus feature-specific domains such as Stripe and Google when enabled. No secret key belongs in a WASM application.

Checkout, plan/subscription drawers and reverification currently require Clerk's **internal, version-pinned** entry points. They are isolated in the bridge and are not promised to remain compatible with arbitrary Clerk upgrades.

## Migration from the native Razor UI

This is a breaking UI implementation change:

- Remove CSS targeting the old `cb-*` forms/profiles. Use Clerk appearance props.
- Remove `BillingContent`/`ApiKeysContent` overrides. Clerk profiles manage their built-in billing/API-key pages.
- Replace `UserAvatar.Size` with appearance customization.
- Use `PlanDetailsButton.PlanId` rather than a serialized `BillingPlan`.
- Replace `DrawerClass` and `PricingTableCheckoutProps` with the upstream `Appearance`/`Options` objects.
- `APIKeys` uses Clerk's active user/organization context. For an explicit subject, use the C# `ApiKeys` service in a custom UI.
- Native-only completion/selection events, sign-in/up `SsoCallbackUrl`/`ChildContent`, and `CreateOrganization.HideSlug` have been removed rather than silently ignored. Clerk owns those flows. Use auth state, upstream props or custom headless flows instead.
- `PricingTable.OnSubscriptionComplete` is not an upstream prop. Use `CheckoutButton.OnSubscriptionComplete` for an explicitly triggered checkout.
- Google One Tap uses Clerk's supported props; custom error handling is available through headless services rather than the removed native-component events.

## Validation

```powershell
dotnet test tests\Clerk.Blazor.Tests -c Release
dotnet test tests\Sample.Tests -c Release

# Explicit, real-Clerk UI smoke checks (development publishable key required)
dotnet test tests\Sample.Tests -c Release --filter "FullyQualifiedName~ClerkUiLiveTests"
```

The ordinary browser suite uses a contract fake: it verifies loading, props, lifecycle, modal coordination and reactive state, **not Clerk's actual visuals or payment/authentication behavior**. Live smoke tests render real sign-in/up UI and exercise repeated modal triggers at desktop/mobile sizes without authenticating.

The separate real sign-in test requires `CLERK_TEST_EMAIL` and `CLERK_TEST_PASSWORD` and an explicitly selected `ClerkSignInE2ETests` fixture. Billing, Google One Tap, signed-in profiles, session tasks and OAuth consent need their corresponding instance configuration and live scenario coverage. See [the integration matrix](docs/parity.md); this library does not claim exhaustive tested parity with Clerk React.
