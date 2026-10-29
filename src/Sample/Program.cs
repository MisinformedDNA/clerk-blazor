using Clerk.Blazor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
{
    throw new InvalidOperationException("Configuration value 'Api:BaseUrl' must be a valid absolute URI.");
}

builder.Services.AddHttpClient("SampleApi", client => client.BaseAddress = apiBaseUri);

// Clerk: loads ClerkJS headless (no React), registers ClerkService, the AuthenticationStateProvider
// and authorization. Publishable key comes from wwwroot/appsettings*.json (safe to expose).
builder.Services.AddClerk(options =>
{
    options.PublishableKey = builder.Configuration["Clerk:PublishableKey"];
    options.SignInUrl = "/sign-in";
    options.SignUpUrl = "/sign-up";
    options.AfterSignOutUrl = "/";
});

await builder.Build().RunAsync();
