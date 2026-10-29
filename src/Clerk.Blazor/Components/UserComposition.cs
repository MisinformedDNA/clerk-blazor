using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Clerk.Blazor.Components;

/// <summary>How the user menu opens the account profile.</summary>
public enum ClerkUserProfileMode { Modal, Navigation }

/// <summary>Registration context used by declarative user profile pages.</summary>
public sealed class UserProfileRegistry
{
    internal readonly Dictionary<object, UserProfileEntry> Entries = [];
    internal Action? Changed;
    internal void Set(object owner, UserProfileEntry entry)
    {
        if (Entries.TryGetValue(owner, out var previous) && previous == entry) return;
        Entries[owner] = entry;
        Changed?.Invoke();
    }
    internal void Remove(object owner) { if (Entries.Remove(owner)) Changed?.Invoke(); }
}
internal sealed record UserProfileEntry(string Label, string Url, RenderFragment? Icon, RenderFragment? Content, bool IsLink);

/// <summary>A custom account profile page. Place inside UserProfile.CustomPages.</summary>
public sealed class UserProfilePage : ComponentBase, IDisposable
{
    [CascadingParameter] public UserProfileRegistry? Registry { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public string Url { get; set; } = "";
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private void RenderContent(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
    protected override void OnParametersSet() => Registry?.Set(this, new(Label, Url.Trim('/'), Icon is null ? null : RenderIcon, ChildContent is null ? null : RenderContent, false));
    private void RenderIcon(RenderTreeBuilder builder) => builder.AddContent(0, Icon);
    public void Dispose() => Registry?.Remove(this);
}

/// <summary>A navigation link in the account profile sidebar.</summary>
public sealed class UserProfileLink : ComponentBase, IDisposable
{
    [CascadingParameter] public UserProfileRegistry? Registry { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public string Url { get; set; } = "";
    [Parameter] public RenderFragment? Icon { get; set; }
    protected override void OnParametersSet() => Registry?.Set(this, new(Label, Url, Icon is null ? null : RenderIcon, null, true));
    private void RenderIcon(RenderTreeBuilder builder) => builder.AddContent(0, Icon);
    public void Dispose() => Registry?.Remove(this);
}

/// <summary>Registration context used by declarative user menu items.</summary>
public sealed class UserButtonRegistry
{
    internal readonly Dictionary<object, UserButtonEntry> Entries = [];
    internal Action? Changed;
    internal void Set(object owner, UserButtonEntry entry)
    {
        if (Entries.TryGetValue(owner, out var previous) && previous == entry) return;
        Entries[owner] = entry;
        Changed?.Invoke();
    }
    internal void Remove(object owner) { if (Entries.Remove(owner)) Changed?.Invoke(); }
}
internal sealed record UserButtonEntry(string Label, RenderFragment? Icon, EventCallback OnClick, string? Open, string? Href);

/// <summary>A custom menu action; Open selects a custom profile page by its Url.</summary>
public sealed class UserButtonAction : ComponentBase, IDisposable
{
    [CascadingParameter] public UserButtonRegistry? Registry { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    private Task InvokeClickAsync() => OnClick.InvokeAsync();
    [Parameter] public string? Open { get; set; }
    protected override void OnParametersSet() => Registry?.Set(this, new(Label, Icon is null ? null : RenderIcon, OnClick.HasDelegate ? EventCallback.Factory.Create(this, InvokeClickAsync) : default, Open, null));
    private void RenderIcon(RenderTreeBuilder builder) => builder.AddContent(0, Icon);
    public void Dispose() => Registry?.Remove(this);
}

/// <summary>A custom link in the user menu.</summary>
public sealed class UserButtonLink : ComponentBase, IDisposable
{
    [CascadingParameter] public UserButtonRegistry? Registry { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public string Href { get; set; } = "";
    [Parameter] public RenderFragment? Icon { get; set; }
    protected override void OnParametersSet() => Registry?.Set(this, new(Label, Icon is null ? null : RenderIcon, default, null, Href));
    private void RenderIcon(RenderTreeBuilder builder) => builder.AddContent(0, Icon);
    public void Dispose() => Registry?.Remove(this);
}
