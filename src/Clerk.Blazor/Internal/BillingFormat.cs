using System.Globalization;

namespace Clerk.Blazor.Internal;

/// <summary>Formatting helpers shared by the billing components.</summary>
internal static class BillingFormat
{
    public static string Date(DateTimeOffset? value) =>
        value is { } d ? d.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture) : "—";

    public static string Money(BillingMoney? money) => money is null ? "—" : $"{money.CurrencySymbol}{money.AmountFormatted}";

    public static bool IsOrganization(string? payer) => string.Equals(payer, "organization", StringComparison.OrdinalIgnoreCase) || string.Equals(payer, "org", StringComparison.OrdinalIgnoreCase);

    public static string PayerType(string? payer) => IsOrganization(payer) ? "organization" : "user";

    public static string Card(BillingPaymentMethod method, ClerkLocalization l)
    {
        var brand = !string.IsNullOrEmpty(method.WalletType) ? Title(method.WalletType) : Title(method.CardType ?? "Card");
        return string.IsNullOrEmpty(method.Last4) ? brand : string.Format(CultureInfo.CurrentCulture, l.BillingCardEnding, brand, method.Last4);
    }

    public static string? Expiry(BillingPaymentMethod method) =>
        method.ExpiryMonth is { } m && method.ExpiryYear is { } y ? $"{m:00}/{y % 100:00}" : null;

    public static bool HasAnnual(BillingPlan plan) => plan.AnnualMonthlyFee is { Amount: > 0 } || plan.AnnualFee is { Amount: > 0 };

    /// <summary>Badge text and modifier for a subscription item, payment or statement status.</summary>
    public static (string Text, string Kind) Status(string? status, ClerkLocalization l, DateTimeOffset? canceledAt = null) => status switch
    {
        _ when canceledAt is not null && status is "active" or "upcoming" => (l.BillingStatusCanceled, "warning"),
        "active" => (l.BillingStatusActive, "success"),
        "upcoming" => (l.BillingStatusUpcoming, "primary"),
        "past_due" => (l.BillingStatusPastDue, "danger"),
        "canceled" or "abandoned" => (l.BillingStatusCanceled, "warning"),
        "ended" or "expired" => (l.BillingStatusEnded, "neutral"),
        "paid" => (l.BillingStatusPaid, "success"),
        "failed" => (l.BillingStatusFailed, "danger"),
        "pending" or "incomplete" => (l.BillingStatusPending, "warning"),
        "open" => (l.BillingStatusOpen, "primary"),
        "closed" => (l.BillingStatusClosed, "neutral"),
        _ => (Title(status ?? ""), "neutral"),
    };

    public static string BadgeClass(string kind) => kind == "neutral" ? "cb-badge" : $"cb-badge cb-badge--{kind}";

    private static string Title(string value) =>
        value.Length == 0 ? value : char.ToUpper(value[0], CultureInfo.CurrentCulture) + value[1..].Replace('_', ' ');
}
