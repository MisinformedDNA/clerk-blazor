using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Components;

/// <summary>Lifecycle boundary between a Blazor container and Clerk-owned UI.</summary>
public abstract class ClerkUiComponentBase : ClerkRenderedComponentBase, IAsyncDisposable
{
    private ElementReference _container;
    private IJSObjectReference? _mount;
    private DotNetObjectReference<ClerkUiComponentBase>? _reference;
    private bool _disposed, _mounting;
    private string? _failure;

    /// <summary>Additional JSON-serializable Clerk component props, using Clerk's JavaScript property names.</summary>
    [Parameter] public IReadOnlyDictionary<string, object?>? Options { get; set; }

    /// <summary>Per-component Clerk appearance object (variables, elements and layout).</summary>
    [Parameter] public IReadOnlyDictionary<string, object?>? Appearance { get; set; }

    protected abstract string ComponentName { get; }
    protected virtual Dictionary<string, object?> ComponentProps() => new();

    protected Dictionary<string, object?> BuildUiProps()
    {
        var props = ComponentProps();
        if (Options is not null)
            foreach (var (key, value) in Options) props[key] = value;
        if (Appearance is not null) props["appearance"] = Appearance;
        return props;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", string.Join(' ', new[] { "clerk-ui-host", Class }.Where(s => !string.IsNullOrEmpty(s))));
        builder.AddAttribute(3, "data-clerk-component", ComponentName);
        builder.AddElementReferenceCapture(4, value => _container = value);
        builder.CloseElement();
        if (_failure is not null)
        {
            builder.OpenElement(5, "p");
            builder.AddAttribute(6, "role", "alert");
            builder.AddContent(7, _failure);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || _mounting || _failure is not null) return;
        _mounting = true;
        try
        {
            var module = await Clerk.BridgeModuleAsync();
            if (_disposed) return;
            _reference ??= DotNetObjectReference.Create(this);
            var props = BuildUiProps();
            if (_mount is null)
                _mount = await module.InvokeAsync<IJSObjectReference>("mountUi", ComponentName, _container, props, _reference);
            else
                await _mount.InvokeVoidAsync("update", props);
            if (_disposed) await ReleaseAsync();
        }
        catch (JSException ex)
        {
            ReportFailure(ex);
        }
        catch (ClerkException ex)
        {
            ReportFailure(ex);
        }
        finally { _mounting = false; }
    }

    private void ReportFailure(Exception error)
    {
        _failure = error.Message;
        Console.Error.WriteLine($"[Clerk.Blazor] {ComponentName}: {error.Message}");
        if (!_disposed) StateHasChanged();
    }

    [JSInvokable]
    public Task OnUiEvent(string name, JsonElement value) =>
        _disposed ? Task.CompletedTask : InvokeAsync(() => HandleUiEventAsync(name, value));

    protected virtual Task HandleUiEventAsync(string name, JsonElement value) => Task.CompletedTask;

    private async Task ReleaseAsync()
    {
        if (_mount is null) return;
        var mount = _mount;
        _mount = null;
        try
        {
            await mount.InvokeVoidAsync("release");
            await mount.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }

    public override void Dispose()
    {
        _disposed = true;
        base.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await ReleaseAsync();
        _reference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
