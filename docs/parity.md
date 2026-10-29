# Clerk React parity

**Baseline:** `@clerk/react` 6.18.3 is the feature reference. `@clerk/clerk-js` 6.42.0 is the runtime, pinned through `ClerkOptions.ClerkJSVersion`.

**Scope:** every browser-side feature of Clerk React, delivered with native Blazor UI and no React. Server-only features are out of scope: SSR, middleware, backend SDK, and webhooks.

**Status legend:**

- ✅ equivalent
- ⚠️ equivalent with a documented difference
- ❌ not supported, with the reason

## Zero-React guarantee

**How React is kept out:**

- `clerk-blazor.js` loads `clerk.browser.js`, the headless ClerkJS bundle. It never passes `ui`/`clerkUICtor` to `Clerk.load()`, so ClerkJS never fetches `@clerk/ui`, which is the React-based component bundle.
- The bridge rejects these with a `ClerkException`:
  - every `mount*`, `unmount*`, `open*`, and `close*` method
  - every `__internal_*` method except `__internal_loadStripeJs`
  - `authenticateWithBase` and `authenticateWithCoinbaseWallet`, plus the `web3_base_signature` and `web3_coinbase_wallet_signature` strategies. They lazily load the Base Account SDK, which bundles React.
- Payment entry uses Stripe's own Payment Element (`js.stripe.com`). Google One Tap uses Google Identity Services (`accounts.google.com`). Neither uses React.

**Evidence:**

- The 6.42.0 artifacts were audited before the bridge was written. `clerk.browser.js` and its eagerly loaded chunks contain no React runtime. React appears only in `@clerk/ui` and in the Base Account SDK chunk, and neither is reachable through the bridge.
- `tests/Sample.Tests/ZeroReactTests.cs` runs the real bridge in a browser and checks three things:
  - no request matching `react`, `@clerk/ui`, `base-account`, or `coinbase` is made from `/`, `/sign-in`, `/sign-up`, or `/components`
  - UI and wallet methods are rejected before they reach ClerkJS
  - `load()` never receives a UI constructor
- A scan of the published sample (`dotnet publish`) finds no React runtime in its output.

## Hooks and objects

| Clerk React | Clerk.Blazor | Status |
|---|---|---|
| `ClerkProvider` props (keys, proxy, domain, satellite, URLs, redirects, allowed origins/protocols, `localization`, `appearance`, telemetry, nonce, `clerkJSVersion`) | `AddClerk(o => …)` / `ClerkOptions` | ✅ |
| `useAuth` (`isLoaded`, `isSignedIn`, `userId`, `sessionId`, `orgId`, `orgRole`, `orgSlug`, `actor`, `sessionClaims`, `has`, `getToken`, `signOut`) | `ClerkService.Auth`, `GetTokenAsync`, `SignOutAsync` | ✅ |
| `has({ role, permission, feature, plan, reverification })` | `ClerkAuth.Has(ClerkCheck)` | ✅ Same scope rules (`org:`/`user:` prefixes, `fea`/`pla` claims, reverification presets) |
| `useUser` (+ `user.update`, emails, phones, Web3, external accounts, passkeys, TOTP, backup codes, password, image, delete) | `State.User`, `ClerkService.User` | ✅ |
| `useSession`, `useSessionList` | `State.Session`, `State.Sessions`, `SetActiveAsync`, `ClerkService.Session` (active devices, revoke) | ✅ |
| `useOrganization` (memberships, invitations, membership requests, domains, roles; paginated) | `State.Organization`, `ClerkService.Organization` returning `ClerkPage<T>` | ✅ Paging uses explicit page numbers instead of SWR infinite lists |
| `useOrganizationList` (memberships, invitations, suggestions, `createOrganization`, `setActive`) | `ClerkService.Organization`, `SetActiveAsync` | ✅ |
| `useSignIn`, `useSignUp` (all strategies, `authenticateWithRedirect`, Web3, passkeys, email links, tickets, legal acceptance) | `ClerkService.SignIn` / `SignUp` (live `ClerkResource` handles) | ✅ |
| `useReverification` | `ClerkService.WithReverificationAsync` + native dialog | ✅ |
| `usePlans`, `useSubscription`, `useCheckout`, `usePaymentElement`, `usePaymentMethods`, `usePaymentAttempts`, `useStatements` | `ClerkService.Billing` + `CheckoutDrawer` + Stripe Payment Element adapter | ✅ Requires Clerk Billing |
| `useAPIKeys` | `ClerkService.ApiKeys` | ✅ |
| `useWaitlist` | `ClerkService.Waitlist` | ✅ |
| `useOAuthConsent` | `ClerkService.OAuth` | ✅ |
| `useClerk` (URL builders, `redirectTo*`, `handleRedirectCallback`, `handleEmailLinkVerification`, `authenticateWithGoogleOneTap`) | `ClerkService` methods; `InvokeAsync(path, …)` for anything else on the Clerk object | ✅ |
| `isClerkAPIResponseError`, error codes, field errors | `ClerkException` (`Code`, `Errors`, `FieldError`, `GlobalMessage`, `TraceId`), `ClerkOfflineException`, `ClerkReverificationRequiredException` | ✅ |
| Blazor integration | `ClerkAuthenticationStateProvider` (claims for user, session, org role/permissions) | ✅ Blazor-specific addition |

## Components

| Clerk React | Clerk.Blazor | Status |
|---|---|---|
| `ClerkLoaded`, `ClerkLoading`, `ClerkDegraded`, `ClerkFailed` | same | ✅ |
| `Show` / `SignedIn` / `SignedOut` / `Protect` | `Show`, `SignedIn`, `SignedOut` | ✅ |
| `RedirectToSignIn`, `RedirectToSignUp`, `RedirectToUserProfile`, `RedirectToOrganizationProfile`, `RedirectToCreateOrganization`, `RedirectToTasks` | same | ✅ |
| `AuthenticateWithRedirectCallback` | same, and built into `SignIn`/`SignUp` | ✅ |
| `SignInButton`, `SignUpButton`, `SignOutButton`, `SignInWithMetamaskButton` | same | ✅ |
| `SignIn` | same | ⚠️ See the gaps below (combined sign-up, passkey autofill) |
| `SignUp` | same | ✅ |
| `UserAvatar`, `UserButton` (+ `UserButton.Action`, `UserButton.Link`, multi-session) | `UserAvatar`, `UserButton`, `UserButtonAction`, `UserButtonLink` | ✅ |
| `UserProfile` (+ `UserProfile.Page`, `UserProfile.Link`) | `UserProfile`, `UserProfilePage`, `UserProfileLink` | ⚠️ The TOTP setup shows the secret and URI instead of a QR code |
| `OrganizationSwitcher`, `OrganizationList`, `OrganizationProfile` (+ `Page`/`Link`), `CreateOrganization` | same | ✅ |
| `TaskChooseOrganization`, `TaskResetPassword`, `TaskSetupMFA` | `TaskChooseOrganization`, `TaskResetPassword`, `TaskSetupMfa` | ✅ |
| `Waitlist` | same | ✅ |
| `GoogleOneTap` | same | ✅ |
| `OAuthConsent` | same | ✅ |
| `PricingTable`, `CheckoutButton`, `PlanDetailsButton`, `SubscriptionDetailsButton` | same, with native drawers | ✅ |
| `APIKeys` | same | ✅ |
| `appearance` (variables, theme, layout, element selectors) | CSS custom properties (`--cb-*`), `ClerkAppearance`, `cb-*` classes, `Class` and attribute pass-through | ⚠️ No `elements` selector map: style the `cb-*` classes directly |
| `localization` | `ClerkLocalization` (every string) | ✅ |

## Known gaps

| Feature | Reason |
|---|---|
| Base and Coinbase Wallet sign-in | ClerkJS loads the Base Account SDK for both, and it bundles React. They are blocked by design. MetaMask, OKX Wallet, and Solana wallets are supported. |
| TOTP QR code | No QR encoder ships, to keep the payload small. The setup screen shows the secret and an `otpauth://` link, which most authenticator apps can open or import. |
| Passkey autofill (conditional mediation) in the identifier field | Not wired up. The explicit "Use passkey" button is supported. |
| `SignIn` `withSignUp` combined flow | `SignIn` links to `SignUp` instead. |
| `appearance.elements` / `layout` objects | Use CSS on the `cb-*` classes. |

## Prerequisites by feature

- **Organizations:** enable Organizations in the Clerk Dashboard.
- **Billing:** enable Clerk Billing and connect Stripe. Allow `js.stripe.com` in your CSP.
- **API keys:** enable API keys for users and/or organizations.
- **Waitlist:** set sign-up mode to *Waitlist*.
- **Google One Tap:** configure Google as a social connection with custom credentials. Allow `accounts.google.com` in your CSP.
- **OAuth consent:** requires an OAuth application. Host `<OAuthConsent>` on the consent route that Clerk redirects to.
- **Session tasks:** enable the task in the Dashboard (force organization selection, password reset, MFA). Route `/tasks/*` to the task components.
