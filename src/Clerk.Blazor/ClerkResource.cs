using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clerk.Blazor;

/// <summary>
/// A live ClerkJS resource returned by an operation. The JavaScript object stays in the
/// browser; <see cref="Data"/> is a JSON snapshot and <see cref="Handle"/> addresses
/// the live object for follow-up calls.
/// </summary>
public sealed class ClerkResource
{
    private readonly ClerkService _clerk;

    internal ClerkResource(ClerkService clerk, JsonElement data)
    {
        _clerk = clerk;
        Data = data;
        Handle = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("$handle", out var h) ? h.GetString() : null;
    }

    /// <summary>Opaque reference to the live JavaScript resource.</summary>
    public string? Handle { get; }

    /// <summary>JSON snapshot of the resource (camelCase, as exposed by ClerkJS).</summary>
    public JsonElement Data { get; }

    /// <summary>Resource id, when present.</summary>
    public string? Id => GetString("id");

    /// <summary>Reads a string property.</summary>
    public string? GetString(string name) =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Deserializes the snapshot into <typeparamref name="T"/>.</summary>
    public T As<T>() => Data.Deserialize<T>(ClerkJson.Options)!;

    /// <summary>Calls a method on the live resource, e.g. <c>"attemptVerification"</c>.</summary>
    public Task<ClerkResource> InvokeAsync(string method, params object?[] args) =>
        Handle is null
            ? throw new InvalidOperationException("This value is not a live Clerk resource.")
            : _clerk.InvokeHandleAsync(Handle, method, args);

    /// <summary>Calls a method on the live resource and deserializes the result.</summary>
    public async Task<T> InvokeAsync<T>(string method, params object?[] args) => (await InvokeAsync(method, args)).As<T>();

    /// <summary>Items of a list or paginated response, each as a resource.</summary>
    public IReadOnlyList<ClerkResource> Items()
    {
        var array = Data.ValueKind == JsonValueKind.Array ? Data
            : Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("data", out var d) ? d : default;
        return array.ValueKind == JsonValueKind.Array ? array.EnumerateArray().Select(e => new ClerkResource(_clerk, e)).ToList() : [];
    }

    /// <summary>Total count of a paginated response.</summary>
    public int TotalCount =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("total_count", out var t) && t.TryGetInt32(out var n) ? n
        : Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("totalCount", out var t2) && t2.TryGetInt32(out var n2) ? n2
        : Items().Count;
}

/// <summary>A page of results (<c>ClerkPaginatedResponse</c>).</summary>
public sealed record ClerkPage<T>(IReadOnlyList<T> Data, int TotalCount);

internal static class ClerkJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}

/// <summary>Builds argument objects without <c>null</c> members, which ClerkJS treats differently from <c>undefined</c>.</summary>
public static class ClerkArgs
{
    /// <summary>Creates a dictionary from key/value pairs, skipping null values.</summary>
    public static Dictionary<string, object?> Of(params (string Key, object? Value)[] values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (k, v) in values)
            if (v is not null) dict[k] = v;
        return dict;
    }
}

internal sealed record InteropError
{
    public string? Code { get; init; }
    public string? Message { get; init; }
    public int? Status { get; init; }
    public string? ClerkTraceId { get; init; }
    public List<ClerkApiError>? Errors { get; init; }
    public bool IsReverification { get; init; }

    public ClerkException ToException()
    {
        var errors = Errors ?? [];
        var message = Message ?? "An unknown Clerk error occurred.";
        if (IsReverification) return new ClerkReverificationRequiredException(message, errors);
        if (Code is "clerk_offline" or "ClerkOfflineError") return new ClerkOfflineException(message);
        return new ClerkException(Code ?? "unknown_error", message, errors, Status, ClerkTraceId);
    }
}

internal sealed record InteropResult
{
    public JsonElement Value { get; init; }
    public InteropError? Error { get; init; }
}
