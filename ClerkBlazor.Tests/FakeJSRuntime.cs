using Microsoft.JSInterop;

namespace ClerkBlazor.Tests;

/// <summary>
/// A simple fake <see cref="IJSRuntime"/> for unit tests.
/// Records all invocations and returns pre-configured results.
/// </summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    private readonly Dictionary<string, object?> _results =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All recorded JS invocations in call order.</summary>
    public List<(string Identifier, object?[]? Args)> Calls { get; } = [];

    /// <summary>
    /// Configure the value that <see cref="InvokeAsync{TValue}"/> returns for
    /// the given JS identifier.
    /// </summary>
    public void SetResult(string identifier, object? result)
        => _results[identifier] = result;

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, object?[]? args)
    {
        Calls.Add((identifier, args));
        if (_results.TryGetValue(identifier, out var result))
            return ValueTask.FromResult((TValue)result!);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);
}
