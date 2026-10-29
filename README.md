# clerk-blazor

[Clerk](https://clerk.com) authentication for **Blazor WebAssembly**, with feature parity with Clerk React and **no React**.

`Clerk.Blazor` loads the pinned, headless ClerkJS browser SDK (`@clerk/clerk-js@6.42.0`) through a small owned JavaScript bridge, and renders all UI as native Razor components:

- **Typed services and reactive state.** `ClerkService` provides the equivalents of `useAuth`, `useUser`, `useSession`, `useOrganization`, `useSignIn`, `useSignUp`, billing hooks, and more. It plugs into Blazor's `AuthenticationStateProvider`.
- **Native components.** `<SignIn>`, `<SignUp>`, `<UserButton>`, `<UserProfile>`, `<OrganizationSwitcher>`, `<OrganizationProfile>`, `<PricingTable>`, checkout, `<Waitlist>`, `<GoogleOneTap>`, and the rest of the Clerk React component set, styled with one themeable stylesheet.
- **Zero React.** The bridge never loads `@clerk/ui`, never passes a UI constructor to `Clerk.load()`, and refuses every `mount*`/`open*` UI method and the React-backed Base/Coinbase wallet flows. Tests guard this.

See [docs/parity.md](docs/parity.md) for the feature-by-feature mapping and the known gaps.

> **Scope:** Blazor WebAssembly (browser) only. Blazor Server/SSR, middleware, and backend SDKs are out of scope. Client-side checks such as `<Show>` are presentation logic only. Always authorize on your API using the session token.

---

## Contents

1. [Quick start](#quick-start)
2. [Routing and Clerk URLs](#routing-and-clerk-urls)
3. [Components](#components)
4. [Programmatic API](#programmatic-api)
5. [Authorization](#authorization)
6. [Theming and localization](#theming-and-localization)
7. [Configuration and secrets](#configuration-and-secrets)
8. [Migrating from the MVP API](#migrating-from-the-mvp-api)
9. [Project structure and tests](#project-structure-and-tests)
10. [Security notes](#security-notes)

---

## Quick start

**1. Register Clerk** in `Program.cs`:

```csharp
builder.Services.AddClerk(options =>
{
    options.PublishableKey = builder.Configuration["Clerk:PublishableKey"]!;
    options.SignInUrl = "/sign-in";
    options.SignUpUrl = "/sign-up";
    options.AfterSignOutUrl = "/";
});
```

`AddClerk` registers `ClerkService`, the `AuthenticationStateProvider`, and authorization core.

**2. Wrap the router** in `App.razor`:

```razor
<ClerkProvider>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <ClerkLoaded><SignIn Routing="ClerkRouting.Virtual" /></ClerkLoaded>
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</ClerkProvider>
```

`ClerkProvider` loads ClerkJS, cascades `ClerkState`, injects the stylesheet, and hosts the reverification dialog.

**3. Import the namespaces** in `_Imports.razor`:

```razor
@using Clerk.Blazor
@using Clerk.Blazor.Components
```

**4. Use the components:**

```razor
<SignedIn>
    <OrganizationSwitcher />
    <UserButton ShowName />
</SignedIn>
<SignedOut>
    <SignInButton Mode="ClerkButtonMode.Modal" />
</SignedOut>
```

No script tags are needed. The bridge module and stylesheet ship as static web assets of the package.

### Run the sample

```bash
# Put your pk_test_… key in src/Sample/wwwroot/appsettings.Development.json
cd src/Sample
dotnet run --launch-profile https
```

The sample shows every feature group:

| Route | What it shows |
|---|---|
| `/` | Header with `UserButton`, `OrganizationSwitcher`, and a modal sign-in button |
| `/protected` | `[Authorize]` page with inline sign-in; calls the sample API with `GetTokenAsync()` |
| `/components` | Live state and the control, button, billing, and API-key components |
| `/user` | `UserProfile`, with billing and API keys |
| `/organization` | `OrganizationProfile`, or `OrganizationList` |
| `/create-organization` | `CreateOrganization` |
| `/pricing` | `PricingTable` |
| `/waitlist` | `Waitlist` |
| `/tasks/*` | Session task screens |
| `/sign-in`, `/sign-up` | Callback targets for OAuth, SSO, and email links |

---

## Routing and Clerk URLs

Native flows run inside Blazor without page reloads. Some steps must arrive on a URL:

- OAuth and SAML callbacks (`/sign-in/sso-callback`)
- Email-link verification (`/sign-in/verify`, `/sign-up/verify`)
- Session tasks (`/tasks/choose-organization`, `/tasks/reset-password`, `/tasks/setup-mfa`)

Pages hosting `<SignIn>` or `<SignUp>` with the default `ClerkRouting.Path` therefore need a catch-all route:

```razor
@page "/sign-in"
@page "/sign-in/{*Path}"
<SignIn />
@code { [Parameter] public string? Path { get; set; } }
```

Use `Routing="ClerkRouting.Virtual"` for modal or inline usage. `SignInButton`/`SignUpButton` with `Mode="ClerkButtonMode.Modal"` do this automatically.

In the Clerk Dashboard (**Paths**), set the sign-in, sign-up, and after-sign-out URLs to match `ClerkOptions`. Add your origins to the allowed redirect origins.

---

## Components

All components accept `Class` and arbitrary HTML attributes (including `style` for per-instance `--cb-*` overrides). The table below summarizes the main parameters; see [docs/parity.md](docs/parity.md) for the full mapping.

| Clerk React | Clerk.Blazor | Notes |
|---|---|---|
| `<ClerkProvider>` | `<ClerkProvider>` | Options come from `AddClerk`; `FailedContent` and `OnLoaded` parameters |
| `<ClerkLoaded>`, `<ClerkLoading>`, `<ClerkDegraded>`, `<ClerkFailed>` | same | |
| `<Show>` (`<SignedIn>`, `<SignedOut>`, `<Protect>`) | `<Show When=… Role=… Permission=… Feature=… Plan=… Condition=… Fallback=…>`, `<SignedIn>`, `<SignedOut>` | |
| `<SignIn>`, `<SignUp>` | same | Every first and second factor, passkeys, Web3 (MetaMask, OKX, Solana), social/enterprise SSO, ticket, restricted/waitlist, legal consent, CAPTCHA |
| `<SignInButton>`, `<SignUpButton>`, `<SignOutButton>`, `<SignInWithMetamaskButton>` | same | `ChildContent` replaces the default button |
| `<UserAvatar>`, `<UserButton>` | same | Custom `UserButtonAction`/`UserButtonLink` items, multi-session switching |
| `<UserProfile>` | same | Account and Security tabs; `UserProfilePage`/`UserProfileLink`; `BillingContent`, `ApiKeysContent` |
| `<OrganizationSwitcher>`, `<OrganizationList>`, `<OrganizationProfile>`, `<CreateOrganization>` | same | Members, invitations, requests, domains, roles; custom pages |
| `<PricingTable>`, `<CheckoutButton>`, `<PlanDetailsButton>`, `<SubscriptionDetailsButton>` | same | Native drawers; card entry stays inside Stripe's Payment Element |
| `<APIKeys>`, `<Waitlist>`, `<GoogleOneTap>`, `<OAuthConsent>` | same | |
| `<TaskChooseOrganization>`, `<TaskResetPassword>`, `<TaskSetupMFA>` | `<TaskChooseOrganization>`, `<TaskResetPassword>`, `<TaskSetupMfa>` | |
| `<RedirectToSignIn>` and the other redirect controls | same | |
| `<AuthenticateWithRedirectCallback>` | same | Usually unnecessary: `<SignIn>` handles `sso-callback` itself |

---

## Programmatic API

Inject `ClerkService` (or use the cascaded `ClerkState`):

```razor
@inject ClerkService Clerk

<p>@Clerk.State.User?.DisplayName</p>

@code {
    protected override void OnInitialized() => Clerk.StateChanged += _ => InvokeAsync(StateHasChanged);

    async Task CallApiAsync()
    {
        var token = await Clerk.GetTokenAsync(new GetTokenOptions { Template = "my-template" });
        // send as Authorization: Bearer <token>
    }
}
```

| React hook | Clerk.Blazor |
|---|---|
| `useAuth()` | `Clerk.Auth` / `State.Auth` (`IsSignedIn`, `UserId`, `SessionId`, `OrgId`, `OrgRole`, `Has(...)`), `GetTokenAsync`, `SignOutAsync` |
| `useUser()`, `useSession()`, `useSessionList()` | `State.User`, `State.Session`, `State.Sessions`, `Clerk.User.*`, `Clerk.Session.*` |
| `useOrganization()`, `useOrganizationList()` | `State.Organization`, `State.Membership`, `Clerk.Organization.*`, `SetActiveAsync` |
| `useSignIn()`, `useSignUp()` | `Clerk.SignIn.*`, `Clerk.SignUp.*` (resources with live handles) |
| `useReverification()` | `Clerk.WithReverificationAsync(...)` (shows the native dialog) |
| `usePlans()`, `useSubscription()`, `useCheckout()`, `usePaymentMethods()`, `usePaymentAttempts()`, `useStatements()` | `Clerk.Billing.*` |
| `useAPIKeys()` | `Clerk.ApiKeys.*` |
| `useWaitlist()` | `Clerk.Waitlist.*` |
| `useClerk()` | `ClerkService` (`Build*Url`, `RedirectTo*`, `HandleRedirectCallbackAsync`, `InvokeAsync(path, …)`) |

Errors surface as `ClerkException` (`Code`, `Errors`, `FieldError(...)`, `TraceId`). Subtypes are `ClerkReverificationRequiredException` and `ClerkOfflineException`.

---

## Authorization

`ClerkAuthenticationStateProvider` emits these claims:

- `NameIdentifier`/`sub`, `sid`, `sts`
- `Name`, `Email`, `GivenName`, `Surname`, `username`, `picture`
- `org_id`, `org_slug`, `org_role` (also a `Role` claim), and one `org_permission` claim per permission

`[Authorize]`, `<AuthorizeView>`, and role policies therefore work. Pending sessions (unfinished session tasks) count as signed out unless you set `TreatPendingAsSignedOut = false`.

```razor
<Show Permission="org:invoices:create" Fallback="@NoAccess">…</Show>
@if (Clerk.Auth.Has(plan: "pro")) { … }
```

---

## Theming and localization

The stylesheet uses CSS custom properties on `.cb-root`:

- `--cb-primary`, `--cb-primary-fg`
- `--cb-bg`, `--cb-fg`, `--cb-muted`, `--cb-border`, `--cb-subtle`
- `--cb-danger`, `--cb-success`, `--cb-warning`, `--cb-focus`
- `--cb-radius`, `--cb-font`, `--cb-font-size`, `--cb-shadow`

Set them in your own CSS, per component through `style`, or globally through `ClerkOptions.Appearance`:

```csharp
options.Appearance = new ClerkAppearance
{
    Theme = ClerkTheme.Auto,           // Light, Dark, Auto (prefers-color-scheme)
    ColorPrimary = "#4f46e5",
    BorderRadius = "10px",
    FontFamily = "Inter, system-ui, sans-serif",
};
```

Disable automatic stylesheet injection with `options.InjectStyles = false` and supply your own styles for the `cb-` classes.

All user-visible strings live in `ClerkLocalization`. Replace any of them through `options.Localization`, e.g. `options.Localization = new ClerkLocalization { SignInTitle = "Welcome back" };`.

---

## Configuration and secrets

`ClerkOptions` covers:

- **Keys and hosting:** `PublishableKey`, `ProxyUrl`, `Domain`, `IsSatellite`
- **Clerk URLs:** `SignInUrl`, `SignUpUrl`, `UserProfileUrl`, `OrganizationProfileUrl`, `CreateOrganizationUrl`, `WaitlistUrl`, `TasksUrl`
- **Redirects:** sign-in/sign-up fallback and force URLs, `AfterSignOutUrl`, `AllowedRedirectOrigins`, `AllowedRedirectProtocols`
- **Behavior:** `TreatPendingAsSignedOut`, `InjectStyles`, `Appearance`, `Localization`
- **Script loading:** `ClerkJSVersion`/`ClerkJSUrl` (pinned to 6.42.0 by default), `Nonce`, `TelemetryDisabled`

The **publishable key** (`pk_…`) is public and belongs in `wwwroot/appsettings*.json`. For CI, patch it in at build time:

```yaml
- name: Patch Clerk publishable key
  run: sed -i 's|YOUR_CLERK_PUBLISHABLE_KEY|${{ secrets.CLERK_PUBLISHABLE_KEY }}|g' src/Sample/wwwroot/appsettings.json
```

The **secret key** (`sk_…`) is server-side only. Use it in your API (see `src/Sample.Api`) through user secrets or a vault. Never put it in WASM configuration.

---

## Migrating from the MVP API

| Before | Now |
|---|---|
| `ClerkAuthService` + manual `InitializeAsync` in `App.razor` | `AddClerk(...)` + `<ClerkProvider>` |
| `<script src="_content/Clerk.Blazor/js/clerkInterop.js">` | Remove it. The module is imported automatically |
| `OpenSignInAsync()` (Clerk's React modal) | `<SignInButton Mode="ClerkButtonMode.Modal">` or `<SignIn>` |
| `GetSessionTokenAsync()` | `ClerkService.GetTokenAsync()` |
| `SignOutAsync()` | `ClerkService.SignOutAsync()` or `<SignOutButton>` |
| `<ClerkUserButton>` | `<UserButton>` |
| `<RedirectToLogin>` | `<RedirectToSignIn>` |
| `ClerkUser` DTO | `ClerkState.User` (`ClerkUser` with emails, phones, wallets, accounts, passkeys, memberships) |

---

## Project structure and tests

```
src/Clerk.Blazor/        Library
  ClerkService.cs          Lifecycle, state, auth helpers, URL builders
  ClerkApis.cs             Typed SignIn/SignUp/User/Organization/Billing/ApiKeys/Waitlist/OAuth/Session APIs
  Models/                  State and resource snapshots
  Components/              Public Razor components
  Internal/                Primitives: Card, Field, Modal, Popover, …
  wwwroot/js/clerk-blazor.js   Headless ClerkJS bridge
  wwwroot/clerk-blazor.css     Themeable stylesheet
src/Sample/              Blazor WASM sample
src/Sample.Api/          Minimal API that verifies Clerk session tokens
tests/Clerk.Blazor.Tests/  NUnit: state, Has(), redirects, reverification, errors, claims
tests/Sample.Tests/        Playwright: real bridge against a fake window.Clerk, zero-React guards,
                           opt-in real-Clerk E2E (ClerkSignInE2ETests, [Explicit])
```

```bash
dotnet test tests/Clerk.Blazor.Tests
dotnet test tests/Sample.Tests          # starts the sample automatically
```

The real-Clerk E2E test needs a development key in `appsettings.Development.json`, plus the `CLERK_TEST_EMAIL` and `CLERK_TEST_PASSWORD` environment variables. Run it with `--filter ClerkSignInE2ETests`.

---

## Security notes

- Client-side checks (`<Show>`, `Has`, `[Authorize]` in WASM) hide UI only. Verify the session token on every API request.
- `redirect_url` values are restricted to same-origin paths and `AllowedRedirectOrigins` (`ClerkService.ResolveRedirect`).
- **Content Security Policy:**
  - `script-src` and `connect-src`: allow your Clerk Frontend API (`*.clerk.accounts.dev` or your custom domain).
  - Billing: allow `js.stripe.com`.
  - Google One Tap: allow `accounts.google.com`.
  - Set `ClerkOptions.Nonce` if you use script nonces.
- Card details are entered only in Stripe's Payment Element iframe. They never reach .NET.
- Do not store session tokens yourself. ClerkJS manages them.
