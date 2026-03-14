using System.Text.Json;
using Microsoft.JSInterop;

namespace ClerkBlazor.Services;

/// <summary>
/// Client-side service that wraps the <c>clerkInterop</c> JavaScript module.
/// Register as a scoped service in <c>Program.cs</c>:
/// <code>builder.Services.AddScoped&lt;ClerkAuthService&gt;();</code>
/// </summary>
public sealed class ClerkAuthService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _js;
    private bool _initialized;

    public ClerkAuthService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Initialise the Clerk SDK in the browser.
    /// Must be called once before any other methods, typically in the root
    /// component's <c>OnAfterRenderAsync</c> (first render).
    /// </summary>
    /// <param name="publishableKey">
    ///   Your Clerk Publishable Key (starts with "pk_test_" or "pk_live_").
    ///   Load from configuration – do NOT hard-code in source.
    /// </param>
    public async Task InitializeAsync(string publishableKey)
    {
        if (_initialized) return;
        await _js.InvokeAsync<bool>("clerkInterop.initialize", publishableKey);
        _initialized = true;
    }

    /// <summary>
    /// Opens the Clerk hosted sign-in modal/UI.
    /// Returns after the user dismisses the modal; call <see cref="GetUserAsync"/>
    /// afterwards to check whether sign-in succeeded.
    /// </summary>
    public async Task SignInAsync()
    {
        EnsureInitialized();
        await _js.InvokeVoidAsync("clerkInterop.openSignIn");
    }

    /// <summary>
    /// Signs the current user out via the Clerk SDK.
    /// </summary>
    public async Task SignOutAsync()
    {
        EnsureInitialized();
        await _js.InvokeVoidAsync("clerkInterop.signOut");
    }

    /// <summary>
    /// Returns the currently authenticated user, or <c>null</c> when no user
    /// is signed in.
    /// </summary>
    /// <returns>A <see cref="ClerkUser"/> instance or <c>null</c>.</returns>
    public async Task<ClerkUser?> GetUserAsync()
    {
        EnsureInitialized();

        // The JS function returns a plain object or null.
        // InvokeAsync<JsonElement?> gives us a raw JSON value we can
        // deserialise into our typed DTO without a second round-trip.
        var element = await _js.InvokeAsync<JsonElement?>("clerkInterop.getUser");

        if (element is null || element.Value.ValueKind == JsonValueKind.Null)
            return null;

        return element.Value.Deserialize<ClerkUser>(_jsonOptions);
    }

    /// <summary>
    /// Registers a .NET callback that is invoked whenever the Clerk auth state
    /// changes (sign-in, sign-out, token refresh).
    /// </summary>
    /// <param name="dotNetRef">
    ///   A <see cref="DotNetObjectReference{T}"/> whose target implements
    ///   a <c>[JSInvokable]</c> method named <c>OnAuthStateChanged(string?)</c>.
    /// </param>
    public async Task RegisterAuthListenerAsync(DotNetObjectReference<ClerkAuthenticationStateProvider> dotNetRef)
    {
        EnsureInitialized();
        await _js.InvokeVoidAsync("clerkInterop.onAuthChange", dotNetRef);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "ClerkAuthService is not initialised. " +
                "Call InitializeAsync(publishableKey) before using other methods.");
    }
}
