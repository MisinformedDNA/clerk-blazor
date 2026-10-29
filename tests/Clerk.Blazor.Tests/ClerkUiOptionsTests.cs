namespace Clerk.Blazor.Tests;

public class ClerkUiOptionsTests
{
    [Test]
    public void Defaults_EnablePinnedOfficialUi()
    {
        var options = new ClerkOptions();
        Assert.Multiple(() =>
        {
            Assert.That(options.LoadUserInterface, Is.True);
            Assert.That(options.ClerkUIVersion, Is.EqualTo("1.42.1"));
            Assert.That(options.ClerkJSVersion, Is.EqualTo("6.42.0"));
        });
    }

    [Test]
    public void LegacyAppearance_MapsToClerkVariables()
    {
        var appearance = new ClerkAppearance { Theme = ClerkTheme.Dark, ColorPrimary = "#4338ca", BorderRadius = "8px" };
        var translated = appearance.ToClerkAppearance();
        Assert.That(translated["variables"], Is.InstanceOf<Dictionary<string, object?>>());
        var variables = (Dictionary<string, object?>)translated["variables"]!;
        Assert.Multiple(() =>
        {
            Assert.That(variables["colorPrimary"], Is.EqualTo("#4338ca"));
            Assert.That(variables["borderRadius"], Is.EqualTo("8px"));
            Assert.That(variables["colorBackground"], Is.EqualTo("#212126"));
            Assert.That(variables["colorInputForeground"], Is.EqualTo("#ffffff"));
        });
    }

    [Test]
    public void LegacyVirtualRouting_IsHashRoutingAlias() =>
        Assert.That(Components.ClerkRouting.Virtual, Is.EqualTo(Components.ClerkRouting.Hash));
}
