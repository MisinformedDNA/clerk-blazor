using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample;
using Clerk.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (!string.IsNullOrWhiteSpace(apiBaseUrl))
{
    builder.Services.AddHttpClient("SampleApi", client =>
        client.BaseAddress = new Uri(apiBaseUrl));
}

// ── Clerk authentication ─────────────────────────────────────────────────────

// Enables [Authorize], <AuthorizeView>, and CascadingAuthenticationState.
builder.Services.AddAuthorizationCore();

// ClerkAuthService wraps the clerkInterop.js JS module.
builder.Services.AddScoped<ClerkAuthService>();

// Register ClerkAuthenticationStateProvider as both the concrete type
// (so pages can cast it and call ForceRefreshAsync / NotifySignOut) and
// as the base AuthenticationStateProvider (used by the framework).
builder.Services.AddScoped<ClerkAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<ClerkAuthenticationStateProvider>());

// ─────────────────────────────────────────────────────────────────────────────

await builder.Build().RunAsync();
