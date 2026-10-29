using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Clerk.Blazor.Components;

/// <summary>Opens the billing section of Clerk's official profile, rather than recreating billing settings.</summary>
public sealed class BillingPage : ComponentBase
{
    [Parameter] public string For { get; set; } = "user";
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (For is not ("user" or "organization")) throw new ArgumentException("For must be 'user' or 'organization'.");
        builder.OpenComponent(0, For == "organization" ? typeof(OrganizationProfile) : typeof(UserProfile));
        builder.AddAttribute(1, "Options", new Dictionary<string, object?> { ["__experimental_startPath"] = "/billing" });
        builder.CloseComponent();
    }
}
