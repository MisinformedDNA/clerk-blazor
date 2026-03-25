# clerk-blazor

Clerk Authentication with Blazor WebAssembly.

---

## Clerk Blazor WASM Integration (MVP)

This repository provides a minimal, production-ready starting point for
integrating [Clerk](https://clerk.com) authentication into a **Blazor
WebAssembly** (standalone) application using the official
[@clerk/clerk-js](https://www.npmjs.com/package/@clerk/clerk-js) browser SDK
via a thin C#/JS interop layer.

> **Blazor Server** support is out of scope for this MVP. The implementation is
> intentionally scoped to Blazor WASM (SPA) to keep it focused and reviewable.

---

### Table of Contents

1. [Architecture overview](#architecture-overview)
2. [Sign-in flow (no page reload)](#sign-in-flow-no-page-reload)
3. [Prerequisites](#prerequisites)
4. [Quick start (local development)](#quick-start-local-development)
5. [Redirect URLs](#redirect-urls)
6. [Configuration & secrets](#configuration--secrets)
7. [Required GitHub Secrets](#required-github-secrets)
8. [Project structure](#project-structure)
9. [Running tests](#running-tests)
10. [Extending the integration](#extending-the-integration)
11. [Security notes](#security-notes)

---

### Architecture overview

```
┌─────────────────────────────────────────────────────┐
│  Blazor WASM (browser)                              │
│                                                     │
│  App.razor ──► ClerkAuthService (C#)                │
│                   └──► clerkInterop.js (JS)         │
│                            └──► window.Clerk (SDK)  │
│                                                     │
│  ClerkAuthenticationStateProvider                   │
│   ├─ GetAuthenticationStateAsync()                  │
│   ├─ ForceRefreshAsync()                            │
│   ├─ NotifySignOut()                                │
│   └─ OnAuthStateChanged(userJson?) ◄── JS callback  │
└─────────────────────────────────────────────────────┘
```

The Clerk JS SDK is **loaded dynamically** from CDN by `clerkInterop.initialize()` only
after the publishable key has been read from `IConfiguration` (see
[Configuration & secrets](#configuration--secrets)). This ensures the SDK never
executes without a valid key — preventing the `Missing publishableKey` error that
newer versions of `clerk-js` throw at load time when no key is found. The C#
`ClerkAuthService` calls into `clerkInterop.js` via `IJSRuntime`. The
`ClerkAuthenticationStateProvider` builds a `ClaimsPrincipal` from the returned
user data and feeds it to Blazor's `CascadingAuthenticationState`.

---

### Sign-in flow (no page reload)

Clicking **Sign in** from any page:

1. `NavMenu.razor` calls `ClerkAuthService.SignInAsync()`, which calls
   `clerkInterop.openSignIn()` in JavaScript.
2. The Clerk hosted sign-in modal opens. `openSignIn()` returns immediately —
   the JS runtime remains on the same page.
3. The user completes sign-in inside the modal.
4. Clerk fires the `addListener` callback registered via `clerkInterop.onAuthChange()`.
5. In Clerk's hash-routing flow, after sign-in completes, Clerk fires the listener
   with `{ user: null/undefined, session: undefined }`. The new session is already
   present in `_clerk.client.activeSessions` but not yet activated client-side.
   `setActive()` is called with that session; once it resolves, `_clerk.user` is
   populated and its JSON is sent to .NET via
   `DotNetObjectReference.invokeMethodAsync('OnAuthStateChanged', userJson)`.
6. `ClerkAuthenticationStateProvider.OnAuthStateChanged` deserializes the user,
   builds a `ClaimsPrincipal`, and calls `NotifyAuthenticationStateChanged`.
7. Blazor's `CascadingAuthenticationState` propagates the new state to all
   `<AuthorizeView>` components — the UI updates in-place with no page reload.

**Key implementation detail**: `openSignIn()` redirects back to the same document
using a hash-fragment URL (`window.location.pathname + window.location.search + '#auth-complete'`).
A hash change does not cause a full-page reload, which keeps the Blazor WASM
runtime alive and in memory.

> **Bug that was fixed**: An earlier version called `await _clerk.load()` inside
> the `addListener` callback. This is problematic for two reasons: (1) `_clerk.load()`
> is idempotent after the first call — it does not re-fetch state; (2) calling it
> inside the listener can trigger re-entrant listener invocations with stale null
> state, overwriting the authenticated user. The fix instead reads the pending
> session from `_clerk.client.activeSessions`, calls `setActive()` to activate it,
> and reads `_clerk.user` directly after `setActive()` resolves — all within the
> same callback invocation.

---

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.x or later |
| A Clerk application | Free tier is fine |

Create your Clerk app at <https://dashboard.clerk.com>.

---

### Quick start (local development)

1. **Clone the repository**

   ```bash
   git clone https://github.com/MisinformedDNA/clerk-blazor.git
   cd clerk-blazor
   ```

2. **Set your Publishable Key in `appsettings.Development.json`**

   Open `ClerkBlazor/wwwroot/appsettings.Development.json` and replace the
   placeholder:

   ```json
   {
     "Clerk": {
       "PublishableKey": "pk_test_BYl3uCvG5R4RlKBa"
     }
   }
   ```

   Replace `pk_test_BYl3uCvG5R4RlKBa` with your **Publishable Key** from the
   [Clerk Dashboard → API Keys](https://dashboard.clerk.com) page.

   The Blazor WASM runtime automatically merges `appsettings.json` (default)
   and `appsettings.Development.json` (development overlay) at startup via
   `IConfiguration`.

   > ℹ️ The Clerk Publishable Key (`pk_test_…` / `pk_live_…`) is **not** a
   > secret — it is intentionally public. The actual secret is `CLERK_SECRET`
   > (used server-side only). Never add `CLERK_SECRET` to any appsettings file.

3. **Run the app**

   ```bash
   cd ClerkBlazor
   dotnet run --launch-profile https
   # Open https://localhost:7077 in your browser
   ```

4. Click **Sign in** in the navigation bar. The Clerk hosted sign-in modal
   will appear. After completing sign-in, the page does **not** reload — the
   navigation bar updates in-place to show the authenticated view.

---

### Redirect URLs

Register the following URLs in your Clerk application's
**Redirect URLs** list (Dashboard → Paths / Redirects):

| URL | Purpose |
|-----|---------|
| `https://localhost:7077/` | Root / post-sign-in landing |
| `https://localhost:7077/login` | Sign-in page |
| `https://localhost:7077/logout` | Sign-out page |
| `https://localhost:7077/authentication/login-callback` | Login callback |
| `https://localhost:7077/authentication/logout-callback` | Logout callback |
| `https://localhost:7077/authentication/silent-refresh` | Silent token refresh (optional) |

For **production** add the equivalent URLs for your deployed domain.

---

### Configuration & secrets

#### Publishable Key (safe to expose to the browser)

The Publishable Key (`pk_test_…` / `pk_live_…`) is a **non-secret** value that
is safe to ship in the browser bundle. It identifies your Clerk application.

Configuration is read from `wwwroot/appsettings.json` (default) and
`wwwroot/appsettings.Development.json` (development overlay) via the standard
Blazor WASM `IConfiguration` mechanism.

| File | Purpose |
|------|---------|
| `wwwroot/appsettings.json` | Committed default — contains placeholder. Update for production at build time via CI/CD. |
| `wwwroot/appsettings.Development.json` | Local dev overlay — replace the placeholder with your dev key. |

For CI/CD, patch `appsettings.json` using a GitHub Actions step (see
[Required GitHub Secrets](#required-github-secrets)):

```yaml
- name: Patch Clerk publishable key into appsettings.json
  run: |
    sed -i 's|YOUR_CLERK_PUBLISHABLE_KEY|${{ secrets.CLERK_PUBLISHABLE_KEY }}|g' \
      ClerkBlazor/wwwroot/appsettings.json
```

#### Secret Key (server-side only — never in the browser)

The Clerk **Secret Key** (`sk_test_…` / `sk_live_…`) is used to verify
sessions server-side (e.g. in a .NET API or Azure Function). **Never** expose
this in the browser or commit it to source control.

Store it as:
- `CLERK_SECRET` GitHub Actions Secret
- `dotnet user-secrets` for local API development:
  ```bash
  dotnet user-secrets set "Clerk:SecretKey" "sk_test_…"
  ```
- Azure Key Vault / AWS Secrets Manager / HashiCorp Vault in production.

---

### Required GitHub Secrets

Add these secrets to your GitHub repository
(Settings → Secrets and variables → Actions → New repository secret):

| Secret name | Description | Required |
|-------------|-------------|----------|
| `CLERK_PUBLISHABLE_KEY` | Clerk Publishable Key (`pk_test_…` or `pk_live_…`) | ✅ Required |
| `CLERK_SECRET` | Clerk Secret Key (`sk_test_…`) for server-side session verification | ✅ Required (future server integration) |
| `CLIENT_APP_URL` | Base URL of the deployed Blazor app (e.g. `https://app.example.com`) | Optional |

#### Example GitHub Actions workflow snippet

```yaml
- name: Patch Clerk publishable key into appsettings.json
  run: |
    sed -i 's|YOUR_CLERK_PUBLISHABLE_KEY|${{ secrets.CLERK_PUBLISHABLE_KEY }}|g' \
      ClerkBlazor/wwwroot/appsettings.json

- name: Publish
  run: dotnet publish ClerkBlazor/ClerkBlazor.csproj -c Release -o publish/
```

> ⚠️ The example publishable key `BYl3uCvG5R4RlKBa` is a **local development
> test key only**. It must not be committed to any non-example file and must
> be rotated after testing.

---

### Project structure

```
ClerkBlazor/
├── App.razor                          # Root component; initialises Clerk
├── RedirectToLogin.razor              # Helper: redirects unauthenticated users
├── _Imports.razor                     # Global namespace imports
├── Program.cs                         # Service registration
├── ClerkBlazor.csproj                 # Project file (includes auth NuGet pkg)
│
├── Services/
│   ├── ClerkUser.cs                   # DTO for JS → C# user data
│   ├── ClerkAuthService.cs            # C# wrapper around clerkInterop.js
│   └── ClerkAuthenticationStateProvider.cs  # AuthenticationStateProvider impl
│
├── Pages/
│   ├── Login.razor                    # Sign-in page
│   ├── Logout.razor                   # Sign-out page
│   └── …                             # Default Blazor pages
│
├── Layout/
│   ├── MainLayout.razor               # Shows current user in top bar
│   └── NavMenu.razor                  # Login/logout links + Sign in button
│
└── wwwroot/
    ├── appsettings.json               # Default config (Clerk:PublishableKey placeholder)
    ├── appsettings.Development.json   # Dev overlay — replace placeholder with your key
    ├── index.html                     # Loads clerkInterop.js
    └── js/
        └── clerkInterop.js            # JS interop module (Clerk SDK wrapper)

ClerkBlazor.Tests/
├── BlazorDevServer.cs                 # Assembly fixture: builds & starts dev server
├── FakeJSRuntime.cs                   # Mock IJSRuntime for unit tests
├── ClerkAuthServiceTests.cs           # Unit tests for ClerkAuthService
├── ClerkAuthStateProviderTests.cs     # Unit tests for ClerkAuthenticationStateProvider
├── SignInFlowTests.cs                 # Playwright tests for the sign-in flow
├── HomePageTests.cs                   # Playwright tests for the Home page
└── LoginPageTests.cs                  # Playwright tests for the Login page
```

---

### Running tests

The test suite combines **unit tests** (fast, no browser required) and
**Playwright end-to-end tests** (requires the Blazor dev server to start).

```bash
# Run the full suite (unit + Playwright)
cd ClerkBlazor.Tests
dotnet test

# Run only unit tests (no server needed)
dotnet test --filter "FullyQualifiedName~ClerkAuthServiceTests|FullyQualifiedName~ClerkAuthStateProviderTests"

# Run only Playwright tests
dotnet test --filter "FullyQualifiedName~HomePageTests|FullyQualifiedName~LoginPageTests|FullyQualifiedName~SignInFlowTests"
```

#### Unit tests

| Class | What is tested |
|-------|---------------|
| `ClerkAuthServiceTests` | `IsInitialized` lifecycle, JS calls for sign-in/sign-out/get-user, idempotent initialization |
| `ClerkAuthStateProviderTests` | `OnAuthStateChanged` with null and valid JSON, claim mapping, `ForceRefreshAsync` notifications |

#### Playwright tests

The Playwright tests start the Blazor dev server automatically via
`BlazorDevServer` (`[SetUpFixture]`).  The sign-in flow tests replace
`clerkInterop.js` at the network level with a mock that fires the .NET auth
callback immediately, so they run without contacting Clerk's CDN or requiring
real credentials.

| Class | What is tested |
|-------|---------------|
| `HomePageTests` | Page load, heading, title, no JS errors, Sign in link visible |
| `LoginPageTests` | Page load, heading, title, no JS errors, Sign in button visible |
| `SignInFlowTests` | `openSignIn` is called on button click; UI updates to authorized view without page reload; protected pages accessible after sign-in; no JS errors during flow |
| `ClerkSignInE2ETests` | Full sign-in with real Clerk credentials and OTP verification; Sign out link appears; Blazor WASM runtime stays alive (requires `CLERK_TEST_EMAIL` / `CLERK_TEST_PASSWORD` env vars) |

---

### Extending the integration

The interop layer is intentionally minimal. Common extensions:

#### Server-side session verification

Add a .NET API (separate project or Azure Function) that accepts a Clerk
session token from the browser, verifies it using `CLERK_SECRET`, and returns a
cookie or JWT:

```csharp
// Example verification using Clerk's REST API
// See: https://clerk.com/docs/references/backend/sessions/verify-session
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", config["Clerk:SecretKey"]);
var result = await client.GetAsync($"https://api.clerk.com/v1/sessions/{sessionId}/verify");
```

#### Additional Clerk SDK methods

Add new methods to `clerkInterop.js` (e.g. `openUserProfile()`, `openSignUp()`)
and expose them through `ClerkAuthService.cs`. The JS interop module documents
where to make these changes.

#### Blazor Server

For Blazor Server, replace the JS interop approach with a server-side Clerk
SDK call on the HTTP request pipeline. This is a separate integration and is
not covered by this MVP.

---

### Security notes

- **Do not commit secrets.** The `.gitignore` already ignores `*.env` files.
  `appsettings.Development.json` ships with a placeholder — replace it locally
  with your real dev key. Use `git update-index --skip-worktree
  ClerkBlazor/wwwroot/appsettings.Development.json` to prevent accidental
  commits once you add a real key.
- **Rotate test credentials** after sharing them or testing in CI.
- **Use HTTPS** in all environments. The Blazor dev server defaults to
  `https://localhost:7077`.
- **Content Security Policy**: if you add a CSP header, allow the Clerk Frontend
  API CDN (`*.clerk.accounts.dev`) for `script-src` and `connect-src`.
- **Token storage**: Clerk JS manages tokens in memory / HTTP-only cookies. Do
  not store session tokens in `localStorage` or `sessionStorage`.

