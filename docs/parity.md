# Clerk UI integration matrix

The implementation now wraps Clerk's own UI instead of recreating Clerk React's UI in Razor. ClerkJS 6.42.0 and Clerk UI 1.42.1 are pinned. React runs internally inside Clerk UI; application code remains Razor/C#.

## Ownership and entry points

| Surface | Bridge entry point |
|---|---|
| `SignIn`, `SignUp` | `mountSignIn` / `mountSignUp` |
| `UserAvatar`, `UserButton`, `UserProfile` | Corresponding `mount*` methods |
| `OrganizationSwitcher`, `OrganizationList`, `OrganizationProfile`, `CreateOrganization` | Corresponding `mount*` methods |
| `PricingTable`, `APIKeys`, `Waitlist`, `OAuthConsent` | Corresponding public `mount*` methods |
| Session tasks | `mountTaskChooseOrganization`, `mountTaskResetPassword`, `mountTaskSetupMFA` |
| `GoogleOneTap` | `openGoogleOneTap` / `closeGoogleOneTap` |
| Sign-in/up trigger buttons | `openSignIn` / `openSignUp`, through one shared modal coordinator |
| Checkout, plan/subscription details | `__internal_openCheckout`, `__internal_openPlanDetails`, `__internal_openSubscriptionDetails` (version-sensitive) |
| Reverification | `__internal_openReverification` (version-sensitive); custom handler supported |
| `BillingPage` | Official user/organization profile starting at its billing section |
| Custom Razor pages/menu items | Clerk custom-page/menu mounting callbacks and Blazor-owned content slots |
| Loading/auth/authorization/redirect controls | Native Blazor components using reactive `ClerkService` state |

Every embedded UI island has a Blazor-owned, otherwise empty host. The bridge mounts Clerk inside it and unmounts on disposal. State changes alone do not remount. Actual prop changes currently unmount/remount that island, which can reset form state.

Modal wrappers do not contain a second dialog. The coordinator closes existing modal/drawer entry points before opening another and tracks trigger ownership so disposing an old trigger cannot close a newer trigger's modal. Clerk manages the actual dialog, navigation, focus and presentation.

## Headless integration

`ClerkService` retains auth state, token access, authorization checks, resource APIs, Blazor authentication-state integration and state-change notifications. `LoadUserInterface = false` skips loading Clerk UI; custom UI can use these services. Built-in UI fails explicitly when disabled.

The previous zero-React restriction has been removed. Base/Coinbase wallet calls are no longer rejected just because their dependencies use React.

## Verification boundaries

- Unit tests cover C# auth/state/redirect/error behavior.
- Fake-Clerk browser tests cover loader configuration, nonce/version selection, headless behavior, mounted UI lifecycle and prop changes, content slots, auth-state propagation, modal replacement, ownership and reverification cancellation.
- Explicit real-Clerk smoke tests render sign-in/up and exercise repeated modal triggers at desktop/mobile sizes, with screenshots available through `CLERK_TEST_SCREENSHOT_DIR`.
- Real credential authentication remains a separate opt-in test.
- Signed-in profile management, organization management, live billing/payment entry, Google One Tap, session tasks and OAuth consent still require live end-to-end verification with suitable instance configuration.

The old statement of full tested feature parity was too strong. A wrapper exposes the configured upstream UI; it does not prove that every Clerk feature has been exercised in this integration.

## Differences from Clerk React

- No React provider/hooks in application code. Use `AddClerk`, `ClerkProvider`, `ClerkService` and Blazor authorization.
- Mounted components expose common typed parameters plus JSON-serializable `Options`/`Appearance`. Arbitrary JavaScript callbacks cannot be serialized; selected events and custom Razor composition are bridged explicitly.
- Hash routing is used for embedded flows without catch-all routes. The former native `Virtual` value aliases `Hash`.
- No separate native Razor form suite. The previous native-only gaps (such as missing TOTP QR rendering or combined sign-in/up) are delegated to the upstream UI, not implemented independently.
- Old native-only component parameters are removed; the migration list is in the README.
- Billing drawers and reverification use internal Clerk entry points and must be checked when updating either pinned runtime.

## Feature prerequisites

Organizations, API keys, waitlist, MFA/session tasks, Billing/Stripe, Google credentials and OAuth applications must be enabled/configured in Clerk as appropriate. Wrapping a component does not enable its corresponding backend feature.
