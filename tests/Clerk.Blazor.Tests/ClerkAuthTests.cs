using System.Text.Json;

namespace Clerk.Blazor.Tests;

public class ClerkAuthTests
{
    private static ClerkAuth SignedIn()
    {
        var clerk = TestClerk.Create();
        clerk.OnStateChanged(TestClerk.SignedInSnapshot);
        return clerk.Auth;
    }

    [Test]
    public void Has_ReturnsFalse_WhenSignedOut() =>
        Assert.That(new ClerkAuth().Has(role: "org:admin"), Is.False);

    [Test]
    public void Has_ReturnsFalse_ForEmptyCheck() =>
        Assert.That(SignedIn().Has(new ClerkCheck()), Is.False);

    [TestCase("org:admin", true)]
    [TestCase("admin", true)]
    [TestCase("org:member", false)]
    public void Has_Role(string role, bool expected) => Assert.That(SignedIn().Has(role: role), Is.EqualTo(expected));

    [TestCase("org:sys_billing:manage", true)]
    [TestCase("sys_billing:manage", true)]
    [TestCase("org:sys_memberships:manage", false)]
    public void Has_Permission(string permission, bool expected) => Assert.That(SignedIn().Has(permission: permission), Is.EqualTo(expected));

    [TestCase("export", true)]
    [TestCase("user:export", true)]
    [TestCase("org:export", false)]
    [TestCase("org:reports", true)]
    [TestCase("reports", true)]
    [TestCase("user:reports", false)]
    [TestCase("missing", false)]
    public void Has_Feature(string feature, bool expected) => Assert.That(SignedIn().Has(feature: feature), Is.EqualTo(expected));

    [TestCase("pro", true)]
    [TestCase("user:pro", true)]
    [TestCase("org:pro", false)]
    public void Has_Plan(string plan, bool expected) => Assert.That(SignedIn().Has(plan: plan), Is.EqualTo(expected));

    [Test]
    public void Has_CombinesChecksWithAnd()
    {
        var auth = SignedIn();
        Assert.Multiple(() =>
        {
            Assert.That(auth.Has(role: "org:admin", plan: "pro"), Is.True);
            Assert.That(auth.Has(role: "org:admin", plan: "enterprise"), Is.False);
        });
    }

    [TestCase(ReverificationLevel.FirstFactor, 10, true)]
    [TestCase(ReverificationLevel.FirstFactor, 5, false)]
    [TestCase(ReverificationLevel.SecondFactor, 10, true)]
    [TestCase(ReverificationLevel.MultiFactor, 10, true)]
    public void Has_Reverification(ReverificationLevel level, int afterMinutes, bool expected)
    {
        // Snapshot: first factor verified 5 minutes ago, second factor never.
        var auth = SignedIn();
        Assert.That(auth.Has(new ClerkCheck { Reverification = new(level, afterMinutes) }), Is.EqualTo(expected));
    }

    [Test]
    public void Has_Reverification_MultiFactorRequiresBothWhenSecondFactorUsed()
    {
        var auth = SignedIn() with { FactorVerificationAge = [5, 30] };
        Assert.Multiple(() =>
        {
            Assert.That(auth.Has(new ClerkCheck { Reverification = new(ReverificationLevel.MultiFactor, 10) }), Is.False);
            Assert.That(auth.Has(new ClerkCheck { Reverification = new(ReverificationLevel.MultiFactor, 60) }), Is.True);
        });
    }

    [Test]
    public void Has_Feature_IgnoresMissingClaims()
    {
        var auth = SignedIn() with { SessionClaims = JsonDocument.Parse("{}").RootElement };
        Assert.That(auth.Has(feature: "export"), Is.False);
    }

    [TestCase(ReverificationPreset.StrictMfa, ReverificationLevel.MultiFactor, 10)]
    [TestCase(ReverificationPreset.Strict, ReverificationLevel.SecondFactor, 10)]
    [TestCase(ReverificationPreset.Moderate, ReverificationLevel.SecondFactor, 60)]
    [TestCase(ReverificationPreset.Lax, ReverificationLevel.SecondFactor, 1440)]
    public void ReverificationPresets_MatchClerk(ReverificationPreset preset, ReverificationLevel level, int minutes) =>
        Assert.That(ReverificationRequirement.FromPreset(preset), Is.EqualTo(new ReverificationRequirement(level, minutes)));
}
