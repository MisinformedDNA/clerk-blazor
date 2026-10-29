using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Tests;

public class ClerkReverificationTests
{
    [TestCase(ReverificationLevel.FirstFactor, "first_factor")]
    [TestCase(ReverificationLevel.SecondFactor, "second_factor")]
    [TestCase(ReverificationLevel.MultiFactor, "multi_factor")]
    public async Task OfficialUi_ReceivesClerkVerificationLevel(ReverificationLevel level, string expected)
    {
        var bridge = new VerificationBridge();
        var clerk = new ClerkService(bridge, new FakeNavigationManager(),
            Options.Create(new ClerkOptions { PublishableKey = "pk_test_Y2xlcmsuZXhhbXBsZS5jb20k" }));
        var attempts = 0;
        await clerk.WithReverificationAsync(() =>
        {
            if (++attempts == 1) throw new ClerkReverificationRequiredException("verify", []);
            return Task.CompletedTask;
        }, new ReverificationRequirement(level, 10));

        Assert.That(bridge.Level, Is.EqualTo(expected));
        Assert.That(attempts, Is.EqualTo(2));
    }

    private sealed class VerificationBridge : IJSRuntime, IJSObjectReference
    {
        public string? Level { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            object result;
            if (identifier == "import") result = this;
            else if (identifier == "load") result = JsonSerializer.Deserialize<JsonElement>(TestClerk.SignedInSnapshot);
            else if (identifier == "reverify")
            {
                Level = (string)args![0]!;
                result = new InteropResult { Value = JsonSerializer.SerializeToElement(true) };
            }
            else throw new InvalidOperationException(identifier);
            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
