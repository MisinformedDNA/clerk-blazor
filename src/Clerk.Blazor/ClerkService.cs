using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Clerk.Blazor;

/// <summary>Token options, equivalent to <c>getToken(options)</c>.</summary>
public sealed record GetTokenOptions
{
    /// <summary>JWT template name.</summary>
    public string? Template { get; init; }
    /// <summary>Organization to mint the token for.</summary>
    public string? OrganizationId { get; init; }
    /// <summary>Bypass the token cache.</summary>
    public bool SkipCache { get; init; }
}

/// <summary>Parameters for <see cref="ClerkService.SetActiveAsync"/>.</summary>
public sealed record SetActiveOptions
{
    /// <summary>Session id to activate. <c>null</c> keeps the current session.</summary>
    public string? SessionId { get; init; }
    /// <summary>Organization id or slug to activate.</summary>
    public string? OrganizationId { get; init; }
    /// <summary>Activate the personal account (no organization).</summary>
    public bool PersonalAccount { get; init; }
    /// <summary>Navigate here once the session is active.</summary>
    public string? RedirectUrl { get; init; }
}

/// <summary>
/// The Clerk instance for this Blazor app, equivalent to <c>useClerk()</c> plus the
/// reactive state returned by <c>useAuth</c>, <c>useUser</c>, <c>useSession</c>,
/// <c>useSessionList</c> and <c>useOrganization</c>.
/// </summary>
public sealed class ClerkService : IAsyncDisposable
{
    private const string ModulePath = "./_content/Clerk.Blazor/js/clerk-blazor.js";

    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly ClerkOptions _options;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ClerkService>? _selfRef;
    private long _lastSeq = -1;
    private Task? _loadTask;

    public ClerkService(IJSRuntime js, NavigationManager nav, IOptions<ClerkOptions> options)
    {
        _js = js;
        _nav = nav;
        _options = options.Value;
        SignIn = new SignInApi(this);
        SignUp = new SignUpApi(this);
        User = new UserApi(this);
        Organization = new OrganizationApi(this);
        Billing = new BillingApi(this);
        ApiKeys = new ApiKeysApi(this);
        Waitlist = new WaitlistApi(this);
        OAuth = new OAuthApi(this);
        Session = new SessionApi(this);
    }

    /// <summary>Options this instance was configured with.</summary>
    public ClerkOptions Options => _options;

    /// <summary>The latest immutable state snapshot.</summary>
    public ClerkState State { get; private set; } = ClerkState.Initial;

    /// <summary>Raised after <see cref="State"/> changes.</summary>
    public event Action<ClerkState>? StateChanged;

    /// <summary>Shortcut for <c>State.Auth</c> (<c>useAuth()</c>).</summary>
    public ClerkAuth Auth => State.Auth;

    /// <summary><c>true</c> once ClerkJS has loaded.</summary>
    public bool IsLoaded => State.IsLoaded;

    /// <summary>Sign-in resource operations (<c>useSignIn</c>).</summary>
    public SignInApi SignIn { get; }

    /// <summary>Sign-up resource operations (<c>useSignUp</c>).</summary>
    public SignUpApi SignUp { get; }

    /// <summary>Signed-in user operations (<c>useUser</c>).</summary>
    public UserApi User { get; }

    /// <summary>Organization operations (<c>useOrganization</c>, <c>useOrganizationList</c>).</summary>
    public OrganizationApi Organization { get; }

    /// <summary>Billing operations (<c>usePlans</c>, <c>useSubscription</c>, <c>useCheckout</c>, …).</summary>
    public BillingApi Billing { get; }

    /// <summary>API key operations (<c>useAPIKeys</c>).</summary>
    public ApiKeysApi ApiKeys { get; }

    /// <summary>Waitlist operations (<c>useWaitlist</c>).</summary>
    public WaitlistApi Waitlist { get; }

    /// <summary>OAuth consent operations (<c>useOAuthConsent</c>).</summary>
    public OAuthApi OAuth { get; }

    /// <summary>Session operations, including reverification (<c>useSession</c>, <c>useReverification</c>).</summary>
    public SessionApi Session { get; }

    /// <summary>
    /// Application override for the default Clerk UI verification dialog.
    /// Required for custom reverification in headless mode. Returns <c>true</c> when verified.
    /// </summary>
    public Func<ReverificationRequirement, Task<bool>>? ReverificationHandler { get; set; }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    /// <summary>Loads ClerkJS and the configured UI. Safe to call multiple times.</summary>
    public Task LoadAsync() => _loadTask ??= LoadCoreAsync();

    private async Task LoadCoreAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.PublishableKey))
        {
            Publish(State with { Status = ClerkStatus.Error, Error = new ClerkException("missing_publishable_key", "Clerk:PublishableKey is not configured.") });
            throw (ClerkException)State.Error!;
        }

        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _selfRef ??= DotNetObjectReference.Create(this);
            var snapshot = await _module.InvokeAsync<JsonElement>("load", BuildLoadOptions(), _selfRef);
            ApplySnapshot(snapshot.Deserialize<ClerkSnapshot>(ClerkJson.Options)!);
        }
        catch (Exception ex)
        {
            _loadTask = null;
            Publish(State with { Status = ClerkStatus.Error, Error = ex });
            throw;
        }
    }

    private object BuildLoadOptions()
    {
        var o = _options;
        var loadOptions = ClerkArgs.Of(
            ("appearance", o.UIAppearance ?? o.Appearance.ToClerkAppearance()),
            ("localization", o.UILocalization),
            ("signInUrl", o.SignInUrl),
            ("signUpUrl", o.SignUpUrl),
            ("waitlistUrl", o.WaitlistUrl),
            ("signInFallbackRedirectUrl", o.SignInFallbackRedirectUrl),
            ("signUpFallbackRedirectUrl", o.SignUpFallbackRedirectUrl),
            ("signInForceRedirectUrl", o.SignInForceRedirectUrl),
            ("signUpForceRedirectUrl", o.SignUpForceRedirectUrl),
            ("afterSignOutUrl", o.AfterSignOutUrl),
            ("allowedRedirectOrigins", o.AllowedRedirectOrigins.Count > 0 ? o.AllowedRedirectOrigins : null),
            ("allowedRedirectProtocols", o.AllowedRedirectProtocols.Count > 0 ? o.AllowedRedirectProtocols : null),
            ("isSatellite", o.IsSatellite ? true : null),
            ("telemetry", o.TelemetryDisabled ? false : null),
            ("standardBrowser", o.StandardBrowser),
            ("taskUrls", new Dictionary<string, string>
            {
                ["choose-organization"] = $"{o.TasksUrl.TrimEnd('/')}/choose-organization",
                ["reset-password"] = $"{o.TasksUrl.TrimEnd('/')}/reset-password",
                ["setup-mfa"] = $"{o.TasksUrl.TrimEnd('/')}/setup-mfa",
            }),
            ("sdkMetadata", new { name = "Clerk.Blazor", version = typeof(ClerkService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0" }));

        return ClerkArgs.Of(
            ("publishableKey", o.PublishableKey),
            ("proxyUrl", o.ProxyUrl),
            ("domain", o.Domain),
            ("clerkJSVersion", o.ClerkJSVersion),
            ("clerkJSUrl", o.ClerkJSUrl),
            ("clerkUIVersion", o.ClerkUIVersion),
            ("clerkUIUrl", o.ClerkUIUrl),
            ("loadUserInterface", o.LoadUserInterface),
            ("nonce", o.Nonce),
            ("stylesheetUrl", o.InjectStyles ? new Uri(new Uri(_nav.BaseUri), "_content/Clerk.Blazor/clerk-blazor.css").ToString() : null),
            ("loadOptions", loadOptions));
    }

    /// <summary>Called by the bridge whenever ClerkJS emits a change.</summary>
    [JSInvokable]
    public void OnStateChanged(string snapshotJson)
    {
        var snapshot = JsonSerializer.Deserialize<ClerkSnapshot>(snapshotJson, ClerkJson.Options);
        if (snapshot is not null) ApplySnapshot(snapshot);
    }

    /// <summary>Called by ClerkJS (<c>routerPush</c>/<c>routerReplace</c>) to navigate.</summary>
    [JSInvokable]
    public Task Navigate(string to, bool replace)
    {
        NavigateTo(_nav.ToAbsoluteUri(to).AbsoluteUri, replace);
        return Task.CompletedTask;
    }

    internal void ApplySnapshot(ClerkSnapshot snapshot)
    {
        // The bridge numbers every snapshot; drop any that arrive out of order.
        if (snapshot.Seq <= _lastSeq) return;
        _lastSeq = snapshot.Seq;
        Publish(ClerkState.From(snapshot, _options.TreatPendingAsSignedOut));
    }

    private void Publish(ClerkState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    // ── Generic invocation ──────────────────────────────────────────────────

    private async Task<IJSObjectReference> ModuleAsync()
    {
        await LoadAsync();
        return _module!;
    }

    private static JsonElement Unwrap(InteropResult result) =>
        result.Error is { } error ? throw error.ToException() : result.Value;

    private async Task<JsonElement> RunAsync(string function, params object?[] args)
    {
        var module = await ModuleAsync();
        var result = await module.InvokeAsync<InteropResult>(function, args);
        return Unwrap(result);
    }

    /// <summary>
    /// Invokes a ClerkJS resource method by path (e.g. <c>"client.signIn.create"</c>).
    /// Use the UI components or <see cref="OpenUiAsync"/> for UI lifecycle operations.
    /// </summary>
    public async Task<ClerkResource> InvokeAsync(string path, params object?[] args) =>
        new(this, await RunAsync("invoke", path, args));

    /// <summary>Invokes a ClerkJS method by path and deserializes the result.</summary>
    public async Task<T> InvokeAsync<T>(string path, params object?[] args) => (await InvokeAsync(path, args)).As<T>();

    internal async Task<ClerkResource> InvokeHandleAsync(string handle, string path, params object?[] args) =>
        new(this, await RunAsync("invokeHandle", handle, path, args));

    internal async Task<JsonElement> CallBridgeAsync(string function, params object?[] args) => await RunAsync(function, args);

    internal async Task<IJSObjectReference> BridgeModuleAsync() => await ModuleAsync();

    /// <summary>Opens a Clerk modal, closing any modal previously opened through this integration.</summary>
    public async Task OpenUiAsync(ClerkUiModal modal, IReadOnlyDictionary<string, object?>? props = null) =>
        await RunAsync("openUi", modal.ToString(), props);

    /// <summary>Closes Clerk modals and drawers.</summary>
    public async Task CloseUiAsync()
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("closeUi");
    }

    /// <summary>
    /// Runs an operation that may require reverification (<c>useReverification</c>). If ClerkJS
    /// reports that reverification is required, the registered handler prompts the user
    /// and the operation is retried once.
    /// </summary>
    public async Task<T> WithReverificationAsync<T>(Func<Task<T>> operation, ReverificationRequirement? requirement = null)
    {
        try
        {
            return await operation();
        }
        catch (ClerkReverificationRequiredException) when (ReverificationHandler is not null || Options.LoadUserInterface)
        {
            var needed = requirement ?? ReverificationRequirement.FromPreset(ReverificationPreset.Strict);
            var verified = ReverificationHandler is not null
                ? await ReverificationHandler(needed)
                : (await RunAsync("reverify", needed.Level switch
                {
                    ReverificationLevel.FirstFactor => "first_factor",
                    ReverificationLevel.MultiFactor => "multi_factor",
                    _ => "second_factor",
                })).GetBoolean();
            if (!verified) throw new ClerkException("reverification_cancelled", "Verification was cancelled.");
            return await operation();
        }
    }

    /// <inheritdoc cref="WithReverificationAsync{T}(Func{Task{T}}, ReverificationRequirement?)"/>
    public Task WithReverificationAsync(Func<Task> operation, ReverificationRequirement? requirement = null) =>
        WithReverificationAsync(async () => { await operation(); return true; }, requirement);

    // ── Auth helpers ────────────────────────────────────────────────────────

    /// <summary>Returns a session token (JWT), or <c>null</c> when signed out.</summary>
    public async Task<string?> GetTokenAsync(GetTokenOptions? options = null)
    {
        var args = options is null ? null : ClerkArgs.Of(
            ("template", options.Template),
            ("organizationId", options.OrganizationId),
            ("skipCache", options.SkipCache ? true : null));
        var value = await RunAsync("getToken", args);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    /// <summary>Signs out the current session, or all sessions.</summary>
    public async Task SignOutAsync(string? sessionId = null, string? redirectUrl = null)
    {
        await RunAsync("signOut", ClerkArgs.Of(("sessionId", sessionId), ("redirectUrl", redirectUrl ?? _options.AfterSignOutUrl)));
    }

    /// <summary>Sets the active session and/or organization.</summary>
    public async Task SetActiveAsync(SetActiveOptions options)
    {
        var args = new Dictionary<string, object?>();
        if (options.SessionId is not null) args["session"] = options.SessionId;
        if (options.PersonalAccount) args["organization"] = null;
        else if (options.OrganizationId is not null) args["organization"] = options.OrganizationId;
        if (options.RedirectUrl is not null) args["redirectUrl"] = options.RedirectUrl;
        await RunAsync("setActive", args);
    }

    /// <summary>Completes an OAuth/SAML redirect (<c>AuthenticateWithRedirectCallback</c>).</summary>
    public async Task HandleRedirectCallbackAsync(RedirectCallbackOptions? options = null)
    {
        options ??= new();
        await RunAsync("handleRedirectCallback", ClerkArgs.Of(
            ("signInUrl", options.SignInUrl ?? _options.SignInUrl),
            ("signUpUrl", options.SignUpUrl ?? _options.SignUpUrl),
            ("signInFallbackRedirectUrl", options.SignInFallbackRedirectUrl ?? _options.SignInFallbackRedirectUrl),
            ("signUpFallbackRedirectUrl", options.SignUpFallbackRedirectUrl ?? _options.SignUpFallbackRedirectUrl),
            ("signInForceRedirectUrl", options.SignInForceRedirectUrl ?? _options.SignInForceRedirectUrl),
            ("signUpForceRedirectUrl", options.SignUpForceRedirectUrl ?? _options.SignUpForceRedirectUrl),
            ("continueSignUpUrl", options.ContinueSignUpUrl ?? $"{_options.SignUpUrl.TrimEnd('/')}/continue"),
            ("secondFactorUrl", options.SecondFactorUrl ?? $"{_options.SignInUrl.TrimEnd('/')}/factor-two"),
            ("firstFactorUrl", options.FirstFactorUrl ?? $"{_options.SignInUrl.TrimEnd('/')}/factor-one"),
            ("resetPasswordUrl", $"{_options.SignInUrl.TrimEnd('/')}/reset-password"),
            ("verifyEmailAddressUrl", $"{_options.SignUpUrl.TrimEnd('/')}/verify-email-address"),
            ("verifyPhoneNumberUrl", $"{_options.SignUpUrl.TrimEnd('/')}/verify-phone-number"),
            ("transferable", options.Transferable ? null : false)));
    }

    /// <summary>
    /// Completes an email-link verification opened in this tab. <paramref name="redirectUrl"/>
    /// is the continuation route (second factor by default), while <paramref name="redirectUrlComplete"/>
    /// is the destination after session activation.
    /// </summary>
    public async Task HandleEmailLinkVerificationAsync(string? redirectUrl = null, string? redirectUrlComplete = null)
    {
        redirectUrl ??= $"{_options.SignInUrl.TrimEnd('/')}/factor-two";
        redirectUrlComplete ??= _options.SignInForceRedirectUrl ?? _options.SignInFallbackRedirectUrl;
        await RunAsync("handleEmailLinkVerification", ClerkArgs.Of(("redirectUrl", redirectUrl), ("redirectUrlComplete", redirectUrlComplete)));
    }

    // ── URL builders & redirects (useClerk().buildXUrl / redirectToX) ───────

    /// <summary>Sign-in URL including the current page as <c>redirect_url</c>.</summary>
    public string BuildSignInUrl(string? redirectUrl = null) => WithRedirect(_options.SignInUrl, redirectUrl);

    /// <summary>Sign-up URL including the current page as <c>redirect_url</c>.</summary>
    public string BuildSignUpUrl(string? redirectUrl = null) => WithRedirect(_options.SignUpUrl, redirectUrl);

    public string BuildUserProfileUrl() => _options.UserProfileUrl;
    public string BuildOrganizationProfileUrl() => _options.OrganizationProfileUrl;
    public string BuildCreateOrganizationUrl() => _options.CreateOrganizationUrl;
    public string BuildWaitlistUrl() => _options.WaitlistUrl;
    public string BuildAfterSignOutUrl() => _options.AfterSignOutUrl;

    /// <summary>URL of the page for the current session task, or <c>null</c>.</summary>
    public string? BuildTasksUrl() => State.Session?.CurrentTask?.Key is { } key ? $"{_options.TasksUrl.TrimEnd('/')}/{key}" : null;

    public void RedirectToSignIn(string? redirectUrl = null) => NavigateTo(BuildSignInUrl(redirectUrl));
    public void RedirectToSignUp(string? redirectUrl = null) => NavigateTo(BuildSignUpUrl(redirectUrl));
    public void RedirectToUserProfile() => NavigateTo(BuildUserProfileUrl());
    public void RedirectToOrganizationProfile() => NavigateTo(BuildOrganizationProfileUrl());
    public void RedirectToCreateOrganization() => NavigateTo(BuildCreateOrganizationUrl());
    public void RedirectToWaitlist() => NavigateTo(BuildWaitlistUrl());
    public void RedirectToTasks() { if (BuildTasksUrl() is { } url) NavigateTo(url); }
    public void RedirectToAfterSignOut() => NavigateTo(BuildAfterSignOutUrl());

    private string WithRedirect(string url, string? redirectUrl)
    {
        redirectUrl ??= _nav.Uri;
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}redirect_url={Uri.EscapeDataString(redirectUrl)}";
    }

    /// <summary>
    /// Returns a safe in-app destination from a <c>redirect_url</c> query value. Only
    /// same-origin or explicitly allowed origins are honored.
    /// </summary>
    public string ResolveRedirect(string? redirectUrl, string fallback)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl)) return fallback;
        if (redirectUrl.Contains('\\') || redirectUrl.Any(char.IsControl)) return fallback;
        if (redirectUrl.StartsWith('/') && !redirectUrl.StartsWith("//", StringComparison.Ordinal)) return redirectUrl;
        if (!Uri.TryCreate(redirectUrl, UriKind.Absolute, out var target)) return fallback;
        var origin = new Uri(_nav.BaseUri);
        if (Uri.Compare(target, origin, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            return target.PathAndQuery + target.Fragment;
        return _options.AllowedRedirectOrigins.Any(o => Uri.TryCreate(o, UriKind.Absolute, out var allowed) &&
                   Uri.Compare(target, allowed, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            ? target.ToString()
            : fallback;
    }

    internal void NavigateTo(string to, bool replace = false)
    {
        if (Uri.TryCreate(to, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
        {
            var baseUri = new Uri(_nav.BaseUri);
            var sameOrigin = Uri.Compare(absolute, baseUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
            _nav.NavigateTo(to, forceLoad: !sameOrigin, replace: replace);
            return;
        }
        _nav.NavigateTo(to.StartsWith('/') ? "." + to : to, replace: replace);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        _selfRef?.Dispose();
    }
}

/// <summary>Options for <see cref="ClerkService.HandleRedirectCallbackAsync"/>.</summary>
public sealed record RedirectCallbackOptions
{
    public string? SignInUrl { get; init; }
    public string? SignUpUrl { get; init; }
    public string? SignInFallbackRedirectUrl { get; init; }
    public string? SignUpFallbackRedirectUrl { get; init; }
    public string? SignInForceRedirectUrl { get; init; }
    public string? SignUpForceRedirectUrl { get; init; }
    public string? ContinueSignUpUrl { get; init; }
    public string? FirstFactorUrl { get; init; }
    public string? SecondFactorUrl { get; init; }
    public bool Transferable { get; init; } = true;
}
