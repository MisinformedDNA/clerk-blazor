using System.Text.Json;
using Clerk.Blazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Tests;

public class ClerkEmailLinkCallbackTests
{
    [TestCase(false, false, "/sign-in/factor-two", "/")]
    [TestCase(true, false, "/sign-up/continue", "/")]
    [TestCase(false, true, "/custom-second-factor", "/sign-in-forced")]
    [TestCase(true, true, "/custom-continue", "/sign-up-forced")]
    public async Task Component_SeparatesContinuationFromCompletion(bool signUp, bool overrides, string continuation, string complete)
    {
        var bridge = new EmailLinkBridge();
        var clerk = new ClerkService(bridge, new FakeNavigationManager(),
            Options.Create(new ClerkOptions { PublishableKey = "pk_test_Y2xlcmsuZXhhbXBsZS5jb20k" }));
        using var services = new ServiceCollection().AddLogging().AddSingleton(clerk)
            .AddSingleton<NavigationManager>(new FakeNavigationManager()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            CallbackHarness? callback = null;
            var parameters = new Dictionary<string, object?>
            {
                ["EmailLink"] = true,
                ["EmailLinkSignUp"] = signUp,
                ["Capture"] = (Action<CallbackHarness>)(instance => callback = instance),
            };
            if (overrides)
            {
                parameters["SecondFactorUrl"] = "/custom-second-factor";
                parameters["ContinueSignUpUrl"] = "/custom-continue";
                parameters["SignInFallbackRedirectUrl"] = "/sign-in-fallback";
                parameters["SignUpFallbackRedirectUrl"] = "/sign-up-fallback";
                parameters["SignInForceRedirectUrl"] = "/sign-in-forced";
                parameters["SignUpForceRedirectUrl"] = "/sign-up-forced";
            }
            await renderer.RenderComponentAsync<CallbackHarness>(ParameterView.FromDictionary(parameters));
            await callback!.CompleteAsync();
        });

        Assert.That(bridge.Parameters.GetProperty("redirectUrl").GetString(), Is.EqualTo(continuation));
        Assert.That(bridge.Parameters.GetProperty("redirectUrlComplete").GetString(), Is.EqualTo(complete));
    }

    [Test]
    public async Task Service_DefaultsUseConfiguredForceBeforeFallback()
    {
        var bridge = new EmailLinkBridge();
        var clerk = new ClerkService(bridge, new FakeNavigationManager(),
            Options.Create(new ClerkOptions
            {
                PublishableKey = "pk_test_Y2xlcmsuZXhhbXBsZS5jb20k", SignInUrl = "/login",
                SignInForceRedirectUrl = "/forced", SignInFallbackRedirectUrl = "/fallback",
            }));
        await clerk.HandleEmailLinkVerificationAsync();
        Assert.That(bridge.Parameters.GetProperty("redirectUrl").GetString(), Is.EqualTo("/login/factor-two"));
        Assert.That(bridge.Parameters.GetProperty("redirectUrlComplete").GetString(), Is.EqualTo("/forced"));
    }

    public sealed class CallbackHarness : AuthenticateWithRedirectCallback
    {
        [Parameter] public Action<CallbackHarness>? Capture { get; set; }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Capture?.Invoke(this);
        }
        public Task CompleteAsync() => OnAfterRenderAsync(true);
    }

    private sealed class EmailLinkBridge : IJSRuntime, IJSObjectReference
    {
        public JsonElement Parameters { get; private set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            object result;
            if (identifier == "import") result = this;
            else if (identifier == "load") result = JsonSerializer.Deserialize<JsonElement>(TestClerk.SignedInSnapshot);
            else if (identifier == "handleEmailLinkVerification")
            {
                Parameters = JsonSerializer.SerializeToElement(args![0]);
                result = new InteropResult();
            }
            else throw new InvalidOperationException(identifier);
            return ValueTask.FromResult((TValue)result);
        }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
