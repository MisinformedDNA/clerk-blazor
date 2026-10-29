using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Clerk.Blazor.Components;

public enum ClerkUserProfileMode { Modal, Navigation }

/// <summary>Registration scope for Razor content embedded in Clerk UI custom pages and menus.</summary>
public sealed class ClerkUiRegistry
{
    internal readonly Dictionary<string, ClerkUiItem> Items = [];
    internal Action? Changed;
}

public abstract class ClerkUiComposedComponent : ClerkUiComponentBase
{
    private readonly ClerkUiRegistry _registry = new();
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? CustomPages { get; set; }
    protected virtual RenderFragment? ExtraDeclarations => null;
    protected virtual string? ProfilePropsKey => null;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _registry.Changed = () => _ = InvokeAsync(StateHasChanged);
    }

    protected Dictionary<string, object?> Compose(Dictionary<string, object?> props)
    {
        props["__profilePropsKey"] = ProfilePropsKey;
        props["__customItems"] = _registry.Items.Values.Select(item => new
        {
            id = item.Id, kind = item.Kind, label = item.Label, url = item.Url,
            href = item.Href, open = item.Open,
            iconId = item.Icon is null ? null : item.Id + "-icon",
            contentId = item.ChildContent is null ? null : item.Id + "-content",
            hasClick = item.OnClick.HasDelegate,
        }).ToArray();
        return props;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        builder.OpenComponent<CascadingValue<ClerkUiRegistry>>(20);
        builder.AddAttribute(21, "Value", _registry);
        builder.AddAttribute(22, "IsFixed", true);
        builder.AddAttribute(23, "ChildContent", (RenderFragment)(content =>
        {
            content.AddContent(0, ChildContent);
            content.AddContent(1, CustomPages);
            content.AddContent(2, ExtraDeclarations);
        }));
        builder.CloseComponent();
    }

    protected override Task HandleUiEventAsync(string name, JsonElement value) =>
        _registry.Items.TryGetValue(name, out var item) ? item.OnClick.InvokeAsync() : Task.CompletedTask;

    public override void Dispose()
    {
        _registry.Changed = null;
        base.Dispose();
    }
}

/// <summary>Renders Razor-owned content which Clerk places into a custom page or menu slot.</summary>
public abstract class ClerkUiItem : ComponentBase, IDisposable
{
    internal string Id { get; } = "clerk-slot-" + Guid.NewGuid().ToString("N");
    internal abstract string Kind { get; }
    [CascadingParameter] public ClerkUiRegistry? Registry { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public string? Url { get; set; }
    [Parameter] public string? Href { get; set; }
    [Parameter] public string? Open { get; set; }
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    private ClerkUiRegistry? _registered;
    private (string, string?, string?, string?, RenderFragment?, RenderFragment?, EventCallback)? _previous;

    protected override void OnParametersSet()
    {
        if (Registry is null) throw new InvalidOperationException($"{GetType().Name} must be inside a Clerk profile or menu component.");
        var current = (Label, Url, Href, Open, Icon, ChildContent, OnClick);
        if (_registered == Registry && _previous.Equals(current)) return;
        _registered?.Items.Remove(Id);
        _registered = Registry;
        _registered.Items[Id] = this;
        _previous = current;
        _registered.Changed?.Invoke();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "hidden", true);
        if (Icon is not null)
        {
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "id", Id + "-icon");
            builder.AddContent(4, Icon);
            builder.CloseElement();
        }
        if (ChildContent is not null)
        {
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "id", Id + "-content");
            builder.AddContent(7, ChildContent);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (_registered?.Items.Remove(Id) == true) _registered.Changed?.Invoke();
    }
}

public sealed class UserProfilePage : ClerkUiItem { internal override string Kind => "page"; }
public sealed class UserProfileLink : ClerkUiItem { internal override string Kind => "link"; }
public sealed class OrganizationProfilePage : ClerkUiItem { internal override string Kind => "page"; }
public sealed class OrganizationProfileLink : ClerkUiItem { internal override string Kind => "link"; }
public sealed class UserButtonAction : ClerkUiItem { internal override string Kind => "action"; }
public sealed class UserButtonLink : ClerkUiItem { internal override string Kind => "menuLink"; }
