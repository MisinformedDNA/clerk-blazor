using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Components;

/// <summary>
/// Base class for Clerk components. Re-renders on every Clerk state change and provides
/// busy/error handling for operations. Derive from it to build custom Clerk-aware UI.
/// </summary>
public abstract class ClerkComponentBase : ComponentBase, IDisposable
{
    [Inject] protected ClerkService Clerk { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>Extra CSS class for the component root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the component root.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected ClerkState State => Clerk.State;
    protected ClerkAuth Auth => Clerk.State.Auth;
    protected ClerkEnvironment Env => Clerk.State.Environment ?? new ClerkEnvironment();
    protected ClerkLocalization L => Clerk.Options.Localization;

    /// <summary>True while <see cref="RunAsync(Func{Task})"/> is executing.</summary>
    protected bool Busy { get; private set; }

    /// <summary>The last error from <see cref="RunAsync(Func{Task})"/>.</summary>
    protected ClerkException? Error { get; set; }

    protected override void OnInitialized() => Clerk.StateChanged += HandleStateChanged;

    private void HandleStateChanged(ClerkState state) => _ = InvokeAsync(() => OnClerkStateChanged(state));

    /// <summary>Called on the renderer's sync context after Clerk state changes. Re-renders by default.</summary>
    protected virtual void OnClerkStateChanged(ClerkState state) => StateHasChanged();

    /// <summary>Runs an operation with busy tracking, reverification and error capture. Returns <c>true</c> on success.</summary>
    protected async Task<bool> RunAsync(Func<Task> operation)
    {
        if (Busy) return false;
        Busy = true;
        Error = null;
        StateHasChanged();
        try
        {
            await Clerk.WithReverificationAsync(operation);
            return true;
        }
        catch (ClerkException ex)
        {
            Error = ex;
            return false;
        }
        catch (JSException ex)
        {
            Error = new ClerkException("unexpected_error", ex.Message);
            return false;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    protected string? FieldError(string param) => Error?.FieldError(param);

    /// <summary>Error text not attributable to one of <paramref name="shownFields"/>.</summary>
    protected string? GlobalError(params string[] shownFields)
    {
        if (Error is null) return null;
        if (Error.Errors.Count == 0) return Error.Message;
        var rest = Error.Errors.Where(e => string.IsNullOrEmpty(e.ParamName) || !shownFields.Any(f => Same(f, e.ParamName!))).ToList();
        if (rest.Count == 0) return null;
        return rest[0].LongMessage ?? rest[0].Message;
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.Replace("_", ""), b.Replace("_", ""), StringComparison.OrdinalIgnoreCase);

    /// <summary>CSS class for the root element: <c>cb-root {block} {Class}</c>.</summary>
    protected string RootClass(string block) =>
        string.Join(' ', new[] { "cb-root", block, Clerk.Options.Appearance.RootClass, Class }.Where(s => !string.IsNullOrWhiteSpace(s)));

    protected string? RootStyle => Clerk.Options.Appearance.ToStyle();
    protected string? ThemeAttr => Clerk.Options.Appearance.ThemeAttribute;

    protected string Format(string template, params object?[] args) => string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args);

    protected void Go(string url) => Clerk.NavigateTo(url);

    public virtual void Dispose()
    {
        Clerk.StateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
