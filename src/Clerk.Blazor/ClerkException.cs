namespace Clerk.Blazor;

/// <summary>A single Clerk API error.</summary>
public sealed record ClerkApiError(string? Code, string? Message, string? LongMessage, string? ParamName);

/// <summary>
/// Raised when a ClerkJS operation fails. Preserves Clerk's error codes and per-field errors,
/// equivalent to <c>isClerkAPIResponseError</c> in Clerk React.
/// </summary>
public class ClerkException : Exception
{
    public ClerkException(string code, string message, IReadOnlyList<ClerkApiError>? errors = null, int? status = null, string? traceId = null)
        : base(message)
    {
        Code = code;
        Errors = errors ?? [];
        Status = status;
        TraceId = traceId;
    }

    /// <summary>Clerk error code, e.g. <c>form_password_incorrect</c>.</summary>
    public string Code { get; }

    /// <summary>All errors returned by the Frontend API.</summary>
    public IReadOnlyList<ClerkApiError> Errors { get; }

    /// <summary>HTTP status, when the error came from the Frontend API.</summary>
    public int? Status { get; }

    /// <summary>Clerk trace identifier for support requests.</summary>
    public string? TraceId { get; }

    /// <summary>Returns the first error message for a form field (e.g. <c>password</c>, <c>identifier</c>).</summary>
    public string? FieldError(string paramName)
    {
        var error = Errors.FirstOrDefault(e => string.Equals(e.ParamName, paramName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(e.ParamName?.Replace("_", ""), paramName.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
        return error?.LongMessage ?? error?.Message;
    }

    /// <summary>The message for errors that are not tied to a field.</summary>
    public string GlobalMessage =>
        Errors.FirstOrDefault(e => string.IsNullOrEmpty(e.ParamName))?.LongMessage
        ?? Errors.FirstOrDefault(e => string.IsNullOrEmpty(e.ParamName))?.Message
        ?? (Errors.Count == 0 ? Message : string.Empty);
}

/// <summary>Raised when an action requires the user to re-verify their identity.</summary>
public sealed class ClerkReverificationRequiredException(string message, IReadOnlyList<ClerkApiError> errors)
    : ClerkException("session_reverification_required", message, errors, 403);

/// <summary>Raised when the browser is offline and no cached token is available (Core 3 <c>ClerkOfflineError</c>).</summary>
public sealed class ClerkOfflineException(string message) : ClerkException("clerk_offline", message);
