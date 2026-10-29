namespace Clerk.Blazor;

// Strings for billing, API keys, waitlist and OAuth consent components.
public partial class ClerkLocalization
{
    // Pricing table
    public string BillingPeriodLabel { get; set; } = "Billing period";
    public string BillingNoPlans { get; set; } = "No plans are available yet.";
    public string BillingSwitchToAnnual { get; set; } = "Switch to annual";
    public string BillingSwitchToMonthly { get; set; } = "Switch to monthly";
    public string BillingResubscribe { get; set; } = "Resubscribe";
    public string BillingManage { get; set; } = "Manage";
    public string BillingShowFeatures { get; set; } = "See all features";
    public string BillingHideFeatures { get; set; } = "Hide features";
    public string BillingFreeTrial { get; set; } = "Free trial";
    public string BillingSignInToSubscribe { get; set; } = "Sign in to subscribe";

    // Checkout
    public string BillingCheckoutSuccess { get; set; } = "Your new subscription is all set.";
    public string BillingTrialSuccess { get; set; } = "Your free trial has started.";
    public string BillingPlanPeriod { get; set; } = "Billing";
    public string BillingCredit { get; set; } = "Credit from current plan";
    public string BillingPastDue { get; set; } = "Past due";
    public string BillingTax { get; set; } = "Tax";
    public string BillingTrialEndsOn { get; set; } = "Trial ends {0}";
    public string BillingPaymentMethod { get; set; } = "Payment method";
    public string BillingCardEnding { get; set; } = "{0} ending in {1}";
    public string BillingCardExpires { get; set; } = "Expires {0}";
    public string BillingConfirmFree { get; set; } = "Confirm";
    public string BillingPaymentFormLoading { get; set; } = "Loading payment form…";
    public string BillingSignedInRequired { get; set; } = "Sign in to continue to checkout.";

    // Subscription details
    public string BillingSubscriptionDetails { get; set; } = "Subscription details";
    public string BillingPlanDetails { get; set; } = "Plan details";
    public string BillingNextPayment { get; set; } = "Next payment";
    public string BillingNextPaymentOn { get; set; } = "{0} on {1}";
    public string BillingSubscribedOn { get; set; } = "Subscribed {0}";
    public string BillingStartsOn { get; set; } = "Starts {0}";
    public string BillingStatusActive { get; set; } = "Active";
    public string BillingStatusUpcoming { get; set; } = "Upcoming";
    public string BillingStatusPastDue { get; set; } = "Past due";
    public string BillingStatusCanceled { get; set; } = "Canceled";
    public string BillingStatusEnded { get; set; } = "Ended";
    public string BillingStatusPaid { get; set; } = "Paid";
    public string BillingStatusFailed { get; set; } = "Failed";
    public string BillingStatusPending { get; set; } = "Pending";
    public string BillingStatusOpen { get; set; } = "Open";
    public string BillingStatusClosed { get; set; } = "Closed";
    public string BillingCancelConfirm { get; set; } = "Cancel your {0} subscription? You keep access until {1}.";
    public string BillingCancelConfirmNow { get; set; } = "Cancel your {0} subscription?";
    public string BillingKeepSubscription { get; set; } = "Keep subscription";
    public string BillingNoSubscription { get; set; } = "You don't have a subscription yet.";
    public string BillingSwitchPlans { get; set; } = "Switch plans";

    // Billing page
    public string BillingSubscriptions { get; set; } = "Subscriptions";
    public string BillingDate { get; set; } = "Date";
    public string BillingAmount { get; set; } = "Amount";
    public string BillingStatus { get; set; } = "Status";
    public string BillingPlan { get; set; } = "Plan";
    public string BillingTotal { get; set; } = "Total";
    public string BillingNoStatements { get; set; } = "No statements yet.";
    public string BillingNoPayments { get; set; } = "No payments yet.";
    public string BillingNoPaymentMethods { get; set; } = "No payment methods yet.";
    public string BillingRemovePaymentMethodConfirm { get; set; } = "Remove {0}? It can't be used for future payments.";
    public string BillingExpired { get; set; } = "Expired";
    public string BillingNoOrganization { get; set; } = "Select an organization to manage its billing.";
    public string BillingActions { get; set; } = "Actions";

    // API keys
    public string ApiKeysSearch { get; set; } = "Search keys";
    public string ApiKeysCreate { get; set; } = "Create key";
    public string ApiKeysNamePlaceholder { get; set; } = "e.g. Production server";
    public string ApiKeysExpires { get; set; } = "Expires";
    public string ApiKeysRevoked { get; set; } = "Revoked";
    public string ApiKeysExpired { get; set; } = "Expired";
    public string ApiKeysEmpty { get; set; } = "No API keys yet.";
    public string ApiKeysNoMatches { get; set; } = "No keys match your search.";
    public string ApiKeysRevokeWarning { get; set; } = "Revoking {0} immediately stops every request that uses it. This can't be undone.";
    public string ApiKeysSecretLabel { get; set; } = "Secret key";
    public string ApiKeysDone { get; set; } = "Done";
    public string ApiKeysNeverUsed { get; set; } = "Never";
    public string ApiKeysOneDay { get; set; } = "1 day";
    public string ApiKeysDays { get; set; } = "{0} days";
    public string ApiKeysOneYear { get; set; } = "1 year";

    // Waitlist
    public string WaitlistSuccessTitle { get; set; } = "Thanks for joining";

    // OAuth consent
    public string OAuthConsentSignedInAs { get; set; } = "Signed in as {0}";
    public string OAuthConsentRedirectNotice { get; set; } = "Allowing access redirects you to {0}.";
    public string OAuthConsentMissingClient { get; set; } = "This authorization request is missing a client ID.";
}
