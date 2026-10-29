using System.Text.RegularExpressions;

namespace Clerk.Blazor.Components;

/// <summary>Whether organization actions open a dialog or navigate to an application route.</summary>
public enum ClerkOrganizationMode { Modal, Navigation }

internal static class OrganizationUi
{
    internal static string Slug(string name) => Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    internal static string? Url(string? template, string? id, string? slug) => template?
        .Replace(":id", Uri.EscapeDataString(id ?? ""), StringComparison.Ordinal)
        .Replace(":slug", Uri.EscapeDataString(slug ?? id ?? ""), StringComparison.Ordinal);
    internal static string[] Emails(string text) => Regex.Split(text.Trim(), @"[,;\s]+").Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    internal static string WithQuery(string url, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) return url;
        var hash = url.IndexOf('#');
        var fragment = hash < 0 ? "" : url[hash..];
        var path = hash < 0 ? url : url[..hash];
        return path + (path.Contains('?') ? "&" : "?") + Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value) + fragment;
    }
    internal static string? Query(Uri uri, string name)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0].Replace('+', ' ')) == name)
                return parts.Length == 1 ? "" : Uri.UnescapeDataString(parts[1].Replace('+', ' '));
        }
        return null;
    }
}
