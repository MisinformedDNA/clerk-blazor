using Clerk.Blazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clerk.Blazor.Tests;

public class ClerkComponentRegressionTests
{
    [Test]
    public void CustomItem_RecreatedDelegatesDoNotChangeRegistration()
    {
        var registry = new ClerkUiRegistry();
        var changes = 0;
        registry.Changed = () => changes++;
        using var page = new TestItem(registry);
        for (var i = 0; i < 10; i++)
        {
            var value = i;
            page.Apply(new()
            {
                ["Label"] = "Custom",
                ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, value)),
                ["Icon"] = (RenderFragment)(builder => builder.AddContent(0, value)),
                ["OnClick"] = EventCallback.Factory.Create(this, () => GC.KeepAlive(value)),
            });
        }
        Assert.That(changes, Is.EqualTo(1));
        page.Apply(new() { ["Label"] = "Updated" });
        Assert.That(changes, Is.EqualTo(2));
        page.Apply(new() { ["Icon"] = null });
        Assert.That(changes, Is.EqualTo(3));
    }

    private sealed class TestItem : ClerkUiItem
    {
        public TestItem(ClerkUiRegistry registry) => Registry = registry;
        internal override string Kind => "page";
        public void Apply(Dictionary<string, object?> parameters)
        {
            ParameterView.FromDictionary(parameters).SetParameterProperties(this);
            OnParametersSet();
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Show_ProjectsLocalPendingSessionOverride(bool pendingAsOut, bool expected)
    {
        var clerk = TestClerk.Create();
        clerk.OnStateChanged(TestClerk.SignedInSnapshot.Replace("\"status\": \"active\", \"factor", "\"status\": \"pending\", \"factor"));
        using var services = new ServiceCollection().AddLogging().AddSingleton(clerk)
            .AddSingleton<NavigationManager>(new FakeNavigationManager()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<Show>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["TreatPendingAsSignedOut"] = pendingAsOut,
                ["Permission"] = "org:sys_billing:manage",
                ["Condition"] = (Func<ClerkAuth, bool>)(auth => auth.IsSignedIn && auth.UserId == "user_1"),
                ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, "authorized")),
            }));
            return result.ToHtmlString();
        });
        Assert.That(html.Contains("authorized"), Is.EqualTo(expected));
    }
}
