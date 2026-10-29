namespace Clerk.Blazor.Components;

/// <summary>Registration scope for organization profile pages and navigation links.</summary>
public sealed class OrganizationProfileRegistration
{
    internal List<OrganizationProfilePage> Pages { get; } = [];
    internal List<OrganizationProfileLink> Links { get; } = [];
    internal Action? Changed { get; set; }
    internal void Register(OrganizationProfilePage page) { if (!Pages.Contains(page)) { Pages.Add(page); Changed?.Invoke(); } }
    internal void Register(OrganizationProfileLink link) { if (!Links.Contains(link)) { Links.Add(link); Changed?.Invoke(); } }
    internal void Remove(OrganizationProfilePage page) { if (Pages.Remove(page)) Changed?.Invoke(); }
    internal void Remove(OrganizationProfileLink link) { if (Links.Remove(link)) Changed?.Invoke(); }
}
