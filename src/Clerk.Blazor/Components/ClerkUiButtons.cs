using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Components;

/// <summary>A Blazor-owned trigger for Clerk's shared modal layer. Never creates a Blazor modal.</summary>
public abstract class ClerkUiButton : ClerkComponentBase, IAsyncDisposable
{
    private readonly string _owner = Guid.NewGuid().ToString("N");
    private DotNetObjectReference<ClerkUiButton>? _reference;
    private IJSObjectReference? _module;
    private bool _disposed;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public IReadOnlyDictionary<string, object?>? Options { get; set; }
    [Parameter] public IReadOnlyDictionary<string, object?>? Appearance { get; set; }
    protected abstract ClerkUiModal Modal { get; }
    protected abstract string Label { get; }
    protected virtual Dictionary<string, object?> ModalProps() => new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", $"cb-trigger {(ChildContent is null ? "cb-trigger--button" : "cb-trigger--bare")} {Class}");
        builder.AddAttribute(4, "disabled", Busy);
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, ClickAsync));
        if (ChildContent is null) builder.AddContent(6, Label);
        else builder.AddContent(7, ChildContent);
        builder.CloseElement();
        if (Error is not null)
        {
            builder.OpenElement(8, "span");
            builder.AddAttribute(9, "role", "alert");
            builder.AddContent(10, Error.GlobalMessage);
            builder.CloseElement();
        }
    }

    private Task ClickAsync() => RunAsync(OpenAsync);

    protected virtual async Task OpenAsync()
    {
        _module = await Clerk.BridgeModuleAsync();
        if (_disposed) return;
        var props = ModalProps();
        if (Options is not null)
            foreach (var (key, value) in Options) props[key] = value;
        if (Appearance is not null) props["appearance"] = Appearance;
        _reference ??= DotNetObjectReference.Create(this);
        var result = await _module.InvokeAsync<InteropResult>("openUi", Modal.ToString(), props, _reference, _owner);
        if (result.Error is { } error) throw error.ToException();
        if (_disposed) await _module.InvokeVoidAsync("closeUi", _owner);
    }

    [JSInvokable]
    public Task OnUiEvent(string name, JsonElement value) =>
        _disposed ? Task.CompletedTask : InvokeAsync(() => HandleUiEventAsync(name));

    protected virtual Task HandleUiEventAsync(string name) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        base.Dispose();
        if (_module is not null)
        {
            try { await _module.InvokeVoidAsync("closeUi", _owner); }
            catch (JSDisconnectedException) { }
        }
        _reference?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class SignInButton : ClerkUiButton
{
    protected override ClerkUiModal Modal => ClerkUiModal.SignIn;
    protected override string Label => L.SignInButton;
    [Parameter] public ClerkButtonMode Mode { get; set; }
    [Parameter] public string? FallbackRedirectUrl { get; set; }
    [Parameter] public string? ForceRedirectUrl { get; set; }
    [Parameter] public string? InitialValues { get; set; }
    protected override Dictionary<string, object?> ModalProps() => ClerkArgs.Of(
        ("fallbackRedirectUrl", FallbackRedirectUrl), ("forceRedirectUrl", ForceRedirectUrl),
        ("initialValues", InitialValues is null ? null : new { identifier = InitialValues }));
    protected override Task OpenAsync()
    {
        if (Mode == ClerkButtonMode.Modal) return base.OpenAsync();
        Go(Clerk.BuildSignInUrl(ForceRedirectUrl ?? FallbackRedirectUrl));
        return Task.CompletedTask;
    }
}

public sealed class SignUpButton : ClerkUiButton
{
    protected override ClerkUiModal Modal => ClerkUiModal.SignUp;
    protected override string Label => L.SignUpButton;
    [Parameter] public ClerkButtonMode Mode { get; set; }
    [Parameter] public string? FallbackRedirectUrl { get; set; }
    [Parameter] public string? ForceRedirectUrl { get; set; }
    [Parameter] public object? UnsafeMetadata { get; set; }
    protected override Dictionary<string, object?> ModalProps() => ClerkArgs.Of(
        ("fallbackRedirectUrl", FallbackRedirectUrl), ("forceRedirectUrl", ForceRedirectUrl), ("unsafeMetadata", UnsafeMetadata));
    protected override Task OpenAsync()
    {
        if (Mode == ClerkButtonMode.Modal) return base.OpenAsync();
        Go(Clerk.BuildSignUpUrl(ForceRedirectUrl ?? FallbackRedirectUrl));
        return Task.CompletedTask;
    }
}

public abstract class ClerkBillingUiButton : ClerkUiButton
{
    [Parameter] public string For { get; set; } = "user";
    protected override Task OpenAsync()
    {
        if (For is not ("user" or "organization"))
            throw new ClerkException("invalid_billing_subject", "For must be 'user' or 'organization'.");
        if (!Auth.IsSignedIn)
        {
            Clerk.RedirectToSignIn();
            return Task.CompletedTask;
        }
        if (For == "organization" && Auth.OrgId is null)
            throw new ClerkException("organization_required", "Select an organization before opening organization billing.");
        return base.OpenAsync();
    }
}

public sealed class CheckoutButton : ClerkBillingUiButton
{
    protected override ClerkUiModal Modal => ClerkUiModal.Checkout;
    protected override string Label => L.Subscribe;
    [Parameter, EditorRequired] public string PlanId { get; set; } = "";
    [Parameter] public string PlanPeriod { get; set; } = "month";
    [Parameter] public string? NewSubscriptionRedirectUrl { get; set; }
    [Parameter] public EventCallback OnSubscriptionComplete { get; set; }
    protected override Task OpenAsync()
    {
        if (string.IsNullOrWhiteSpace(PlanId))
            throw new ClerkException("plan_required", "A plan ID is required to open checkout.");
        return base.OpenAsync();
    }
    protected override Dictionary<string, object?> ModalProps() => ClerkArgs.Of(
        ("planId", PlanId), ("planPeriod", PlanPeriod), ("for", For),
        ("newSubscriptionRedirectUrl", NewSubscriptionRedirectUrl),
        ("__events", OnSubscriptionComplete.HasDelegate ? new[] { "onSubscriptionComplete" } : null));
    protected override Task HandleUiEventAsync(string name) =>
        name == "onSubscriptionComplete" ? OnSubscriptionComplete.InvokeAsync() : Task.CompletedTask;
}

public sealed class PlanDetailsButton : ClerkUiButton
{
    protected override ClerkUiModal Modal => ClerkUiModal.PlanDetails;
    protected override string Label => L.BillingPlanDetails;
    [Parameter, EditorRequired] public string PlanId { get; set; } = "";
    [Parameter] public string InitialPlanPeriod { get; set; } = "month";
    protected override Dictionary<string, object?> ModalProps() => ClerkArgs.Of(
        ("planId", PlanId), ("initialPlanPeriod", InitialPlanPeriod));
}

public sealed class SubscriptionDetailsButton : ClerkBillingUiButton
{
    protected override ClerkUiModal Modal => ClerkUiModal.SubscriptionDetails;
    protected override string Label => L.BillingSubscriptionDetails;
    [Parameter] public EventCallback OnSubscriptionCancel { get; set; }
    protected override Dictionary<string, object?> ModalProps() => ClerkArgs.Of(("for", For),
        ("__events", OnSubscriptionCancel.HasDelegate ? new[] { "onSubscriptionCancel" } : null));
    protected override Task HandleUiEventAsync(string name) =>
        name == "onSubscriptionCancel" ? OnSubscriptionCancel.InvokeAsync() : Task.CompletedTask;
}
