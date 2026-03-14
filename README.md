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
2. [Prerequisites](#prerequisites)
3. [Quick start (local development)](#quick-start-local-development)
4. [Redirect URLs](#redirect-urls)
5. [Configuration & secrets](#configuration--secrets)
6. [Required GitHub Secrets](#required-github-secrets)
7. [Project structure](#project-structure)
8. [Extending the integration](#extending-the-integration)
9. [Security notes](#security-notes)

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
│   └─ NotifySignOut()                                │
└─────────────────────────────────────────────────────┘
```

The browser loads the Clerk JS SDK from CDN. The C# `ClerkAuthService` calls
into `clerkInterop.js` via `IJSRuntime`. The `ClerkAuthenticationStateProvider`
builds a `ClaimsPrincipal` from the returned user data and feeds it to Blazor's
`CascadingAuthenticationState`.

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
   will appear.

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
│   └── NavMenu.razor                  # Login/logout links
│
└── wwwroot/
    ├── appsettings.json               # Default config (Clerk:PublishableKey placeholder)
    ├── appsettings.Development.json   # Dev overlay — replace placeholder with your key
    ├── index.html                     # Loads Clerk CDN + clerkInterop.js
    └── js/
        └── clerkInterop.js            # JS interop module (Clerk SDK wrapper)
```

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
- **Content Security Policy**: if you add a CSP header, allow the Clerk CDN
  (`cdn.jsdelivr.net`) and Clerk's own domain (`*.clerk.accounts.dev`).
- **Token storage**: Clerk JS manages tokens in memory / HTTP-only cookies. Do
  not store session tokens in `localStorage` or `sessionStorage`.

