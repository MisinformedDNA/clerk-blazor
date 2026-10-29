using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Tests;

internal sealed class FakeJSRuntime : IJSRuntime
{
    public List<string> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);
}

internal sealed class FakeNavigationManager : NavigationManager
{
    public FakeNavigationManager(string baseUri = "https://app.example.com/", string? uri = null) =>
        Initialize(baseUri, uri ?? baseUri + "current?x=1");

    public List<(string Uri, bool ForceLoad, bool Replace)> Navigations { get; } = [];

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        Navigations.Add((uri, options.ForceLoad, options.ReplaceHistoryEntry));
    }
}

internal static class TestClerk
{
    public static ClerkService Create(Action<ClerkOptions>? configure = null, FakeNavigationManager? nav = null, FakeJSRuntime? js = null)
    {
        var options = new ClerkOptions { PublishableKey = "pk_test_Y2xlcmsuZXhhbXBsZS5jb20k" };
        configure?.Invoke(options);
        return new ClerkService(js ?? new FakeJSRuntime(), nav ?? new FakeNavigationManager(), Options.Create(options));
    }

    public const string SignedInSnapshot = """
    {
      "seq": 1, "status": "ready", "loaded": true, "version": "6.42.0",
      "session": { "id": "sess_1", "status": "active", "factorVerificationAge": [5, -1],
                   "claims": { "fea": "u:export,o:reports", "pla": "u:pro" } },
      "user": { "id": "user_1", "firstName": "Ada", "lastName": "Lovelace", "fullName": "Ada Lovelace",
                "username": "ada", "primaryEmailAddressId": "idn_1",
                "emailAddresses": [ { "id": "idn_1", "emailAddress": "ada@example.com" } ] },
      "organization": { "id": "org_1", "name": "Engines", "slug": "engines" },
      "membership": { "id": "mem_1", "role": "org:admin", "permissions": ["org:sys_billing:manage"] },
      "client": { "id": "client_1", "sessions": [ { "id": "sess_1", "status": "active" }, { "id": "sess_2", "status": "ended" } ] }
    }
    """;
}
