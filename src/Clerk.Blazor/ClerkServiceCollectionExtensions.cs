using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Clerk.Blazor;

public static class ClerkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ClerkService"/>, authorization core and <see cref="ClerkAuthenticationStateProvider"/>.
    /// Render <c>&lt;ClerkProvider&gt;</c> around your router to load Clerk.
    /// </summary>
    public static IServiceCollection AddClerk(this IServiceCollection services, Action<ClerkOptions> configure)
    {
        services.Configure(configure);
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddScoped<ClerkService>();
        services.AddScoped<ClerkAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ClerkAuthenticationStateProvider>());
        return services;
    }
}
