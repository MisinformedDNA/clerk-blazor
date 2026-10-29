namespace Clerk.Blazor.Components;

/// <summary>Options for the checkout drawer opened by <see cref="PricingTable"/> (<c>checkoutProps</c>).</summary>
public sealed record PricingTableCheckoutProps
{
    /// <summary>Extra CSS class for the checkout drawer.</summary>
    public string? Class { get; init; }
}
