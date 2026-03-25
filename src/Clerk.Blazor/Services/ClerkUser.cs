using System.Text.Json.Serialization;

namespace Clerk.Blazor.Services;

/// <summary>
/// Lightweight DTO that mirrors the user object returned by the
/// <c>clerkInterop.getUser()</c> JavaScript function.
/// </summary>
public sealed class ClerkUser
{
    /// <summary>Clerk user identifier (e.g. "user_2abc…").</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Primary e-mail address, or <c>null</c> if not set.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>Given name, or <c>null</c> if not set.</summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    /// <summary>Family name, or <c>null</c> if not set.</summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    /// <summary>Profile picture URL, or <c>null</c> if not set.</summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; init; }
}
