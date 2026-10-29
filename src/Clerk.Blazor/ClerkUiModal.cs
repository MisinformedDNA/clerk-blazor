namespace Clerk.Blazor;

/// <summary>Clerk modal entry points. Billing and reverification use version-pinned internal Clerk APIs.</summary>
public enum ClerkUiModal
{
    SignIn, SignUp, UserProfile, OrganizationProfile, CreateOrganization, Waitlist,
    Checkout, PlanDetails, SubscriptionDetails, Reverification,
}
