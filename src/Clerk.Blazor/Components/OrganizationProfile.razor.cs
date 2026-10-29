using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Clerk.Blazor.Components;

/// <summary>Native organization settings with permission-gated management, custom pages, and optional feature tabs.</summary>
public partial class OrganizationProfile
{
    /// <summary>Path routing requires the hosting application to expose a catch-all route beneath Path.</summary>
    [Parameter] public ClerkRouting Routing { get; set; } = ClerkRouting.Virtual;
    [Parameter] public string? Path { get; set; }
    [Parameter] public string? AfterLeaveOrganizationUrl { get; set; }
    [Parameter] public RenderFragment? BillingContent { get; set; }
    [Parameter] public RenderFragment? ApiKeysContent { get; set; }
    /// <summary>OrganizationProfilePage and OrganizationProfileLink declarations registered in this profile's navigation.</summary>
    [Parameter] public RenderFragment? CustomPages { get; set; }
    [Parameter] public EventCallback OnLeave { get; set; }
    private readonly OrganizationProfileRegistration _registration = new();
    private readonly Dictionary<string, ElementReference> _tabElements = [];
    private readonly Dictionary<string, ElementReference> _memberTabElements = [];
    private readonly string _panelId = "cb-org-profile-" + Guid.NewGuid().ToString("N");
    private string? _orgId, _removeMember, _removeDomain, _revokeInvitation, _rejectRequest;
    private string _tab = "general", _memberTab = "members", _name = "", _slug = "", _deleteName = "", _query = "", _emails = "", _inviteRole = "";
    private string _domainName = "", _affiliationEmail = "", _code = "";
    private int _memberPage = 1, _invitationPage = 1, _requestPage = 1, _domainPage = 1;
    private bool _loading, _confirmLeave, _codeSent, _invitesSent, _disposed;
    private ElementReference _logo;
    private OrganizationDomain? _verifying;
    private IReadOnlyList<OrganizationRole> _roles = [];
    private ClerkPage<OrganizationMember> _members = new([], 0);
    private ClerkPage<OrganizationInvitation> _invitations = new([], 0);
    private ClerkPage<OrganizationMembershipRequest> _requests = new([], 0);
    private ClerkPage<OrganizationDomain> _domains = new([], 0);
    private string LeaveRedirect => AfterLeaveOrganizationUrl ??
        (OrganizationUi.Query(Navigation.ToAbsoluteUri(Navigation.Uri), "after_leave_organization_url") is { Length: > 0 } query ? Clerk.ResolveRedirect(query, Clerk.Options.SignInFallbackRedirectUrl) : Clerk.Options.SignInFallbackRedirectUrl);
    private bool ContextMatches => _orgId is not null && State.Organization?.Id == _orgId;
    private bool CanProfile => ContextMatches && Auth.Has(permission: "org:sys_profile:manage");
    private bool CanMembers => ContextMatches && Auth.Has(permission: "org:sys_memberships:manage");
    private bool CanDomains => ContextMatches && Auth.Has(permission: "org:sys_domains:manage");
    private bool CanReadDomains => CanDomains || Auth.Has(permission: "org:sys_domains:read");
    private bool CanDelete => ContextMatches && Auth.Has(permission: "org:sys_profile:delete") && (State.Organization?.AdminDeleteEnabled == true || Env.Organizations.AdminDeleteEnabled);
    private bool AtMemberLimit => State.Organization is { MaxAllowedMemberships: > 0 } org && org.MembersCount + org.PendingInvitationsCount >= org.MaxAllowedMemberships;
    private IEnumerable<string> EnrollmentModes => new[] { "none", "automatic_invitation", "automatic_suggestion" }.Where(m => m == "none" || Env.Organizations.DomainEnrollmentModes.Count == 0 || Env.Organizations.DomainEnrollmentModes.Contains(m));
    private IEnumerable<(string Key, string Label, RenderFragment? Icon)> Tabs
    {
        get {
            yield return ("general", L.General, null); yield return ("members", L.Members, null);
            if (BillingContent is not null) yield return ("billing", L.OrgBilling, null);
            if (ApiKeysContent is not null) yield return ("api-keys", L.OrgApiKeys, null);
            foreach (var page in _registration.Pages.Where(p => !string.IsNullOrWhiteSpace(p.Url) && p.Url is not ("general" or "members" or "billing" or "api-keys"))) yield return (page.Url, page.Label, page.Icon);
        }
    }
    private IEnumerable<(string Key, string Label)> MemberTabs
    {
        get {
            yield return ("members", L.Members);
            if (CanMembers) { yield return ("invitations", L.Invitations); if (Env.Organizations.DomainsEnabled) yield return ("requests", L.Requests); }
        }
    }
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _registration.Changed = () => { if (!_disposed) _ = InvokeAsync(StateHasChanged); };
        Navigation.LocationChanged += LocationChanged;
        if (Routing == ClerkRouting.Path) {
            var path = Navigation.ToAbsoluteUri(Navigation.Uri).AbsolutePath;
            var root = Navigation.ToAbsoluteUri(Path ?? Clerk.Options.OrganizationProfileUrl).AbsolutePath.TrimEnd('/');
            if (path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) _tab = Uri.UnescapeDataString(path[(root.Length + 1)..].Trim('/'));
        }
    }
    private void LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        if (Routing != ClerkRouting.Path || _disposed) return;
        var root = Navigation.ToAbsoluteUri(Path ?? Clerk.Options.OrganizationProfileUrl).AbsolutePath.TrimEnd('/');
        var path = Navigation.ToAbsoluteUri(args.Location).AbsolutePath.TrimEnd('/');
        if (!path.Equals(root, StringComparison.OrdinalIgnoreCase) && !path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) return;
        var tab = path.Length == root.Length ? "general" : Uri.UnescapeDataString(path[(root.Length + 1)..]);
        if (tab == _tab) return;
        _ = InvokeAsync(async () => {
            _tab = tab; Error = null;
            if (_tab == "members") await RunAsync(LoadMemberTabCoreAsync);
            StateHasChanged();
        });
    }
    protected override async Task OnInitializedAsync() => await LoadOrganizationAsync();
    protected override void OnClerkStateChanged(ClerkState state)
    {
        base.OnClerkStateChanged(state);
        if (!_loading && state.Organization?.Id != _orgId) _ = InvokeAsync(LoadOrganizationAsync);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_disposed && !Busy && !_loading && State.Organization?.Id != _orgId) await LoadOrganizationAsync();
    }
    private async Task LoadOrganizationAsync()
    {
        if (_loading || Busy) return;
        _loading = true;
        var org = State.Organization;
        _orgId = org?.Id;
        _name = org?.Name ?? ""; _slug = org?.Slug ?? ""; _deleteName = _query = _emails = "";
        _removeMember = _removeDomain = _revokeInvitation = _rejectRequest = null;
        _verifying = null; _confirmLeave = _codeSent = _invitesSent = false;
        _memberPage = _invitationPage = _requestPage = _domainPage = 1;
        _members = new([], 0); _invitations = new([], 0); _requests = new([], 0); _domains = new([], 0); _roles = [];
        if (org is not null) await RunAsync(async () => {
            if (CanMembers) {
                _roles = await Clerk.Organization.GetRolesAsync();
                _inviteRole = _roles.FirstOrDefault(r => r.Key == "org:member")?.Key ?? _roles.FirstOrDefault()?.Key ?? "";
            }
            if (Env.Organizations.DomainsEnabled && CanReadDomains) _domains = await Clerk.Organization.GetDomainsAsync();
            if (_tab == "members") await LoadMemberTabCoreAsync();
        });
        _loading = false;
        StateHasChanged();
        if (!_disposed && State.Organization?.Id != _orgId) await LoadOrganizationAsync();
    }
    private async Task TabKeyAsync(KeyboardEventArgs args, string current, bool members)
    {
        if (Busy) return;
        var keys = members ? MemberTabs.Select(t => t.Key).ToList() : Tabs.Select(t => t.Key).ToList();
        var index = keys.IndexOf(current);
        var next = args.Key switch {
            "ArrowRight" or "ArrowDown" => (index + 1) % keys.Count,
            "ArrowLeft" or "ArrowUp" => (index + keys.Count - 1) % keys.Count,
            "Home" => 0,
            "End" => keys.Count - 1,
            _ => -1
        };
        if (next < 0) return;
        var key = keys[next];
        if (members) await SelectMemberTabAsync(key); else await SelectTabAsync(key);
        var elements = members ? _memberTabElements : _tabElements;
        if (elements.TryGetValue(key, out var element)) await RunAsync(async () => {
            var module = await Clerk.BridgeModuleAsync();
            await module.InvokeVoidAsync("focus", element);
        });
    }
    private string TabId(string key) => _panelId + "-tab-" + Uri.EscapeDataString(key);
    private async Task SelectTabAsync(string tab)
    {
        if (Busy) return;
        _tab = tab; Error = null;
        if (Routing == ClerkRouting.Path) Go((Path ?? Clerk.Options.OrganizationProfileUrl).TrimEnd('/') + "/" + tab.Trim('/') + Navigation.ToAbsoluteUri(Navigation.Uri).Query);
        if (_tab == "members") await RunAsync(LoadMemberTabCoreAsync);
    }
    private async Task SelectMemberTabAsync(string tab) { if (Busy) return; _memberTab = tab; await RunAsync(LoadMemberTabCoreAsync); }
    private async Task LoadMemberTabCoreAsync()
    {
        if (!ContextMatches) return;
        if (_memberTab == "members") _members = await Clerk.Organization.GetMembershipsAsync(_memberPage, query: string.IsNullOrWhiteSpace(_query) ? null : _query.Trim());
        else if (_memberTab == "invitations" && CanMembers) _invitations = await Clerk.Organization.GetInvitationsAsync(_invitationPage);
        else if (_memberTab == "requests" && CanMembers && Env.Organizations.DomainsEnabled) _requests = await Clerk.Organization.GetMembershipRequestsAsync(_requestPage);
    }
    private Task MemberPageAsync(int page) { if (Busy) return Task.CompletedTask; _memberPage = page; return RunAsync(LoadMemberTabCoreAsync); }
    private Task InvitationPageAsync(int page) { if (Busy) return Task.CompletedTask; _invitationPage = page; return RunAsync(LoadMemberTabCoreAsync); }
    private Task RequestPageAsync(int page) { if (Busy) return Task.CompletedTask; _requestPage = page; return RunAsync(LoadMemberTabCoreAsync); }
    private Task DomainPageAsync(int page) { if (Busy) return Task.CompletedTask; _domainPage = page; return RunAsync(async () => _domains = await Clerk.Organization.GetDomainsAsync(page)); }
    private Task SearchAsync() { _memberPage = 1; return RunAsync(LoadMemberTabCoreAsync); }
    private Task SaveProfileAsync() => CanProfile ? RunAsync(() => Clerk.Organization.UpdateAsync(_name.Trim(), Env.Organizations.SlugDisabled ? null : _slug.Trim())) : Task.CompletedTask;
    private Task UploadLogoAsync() => CanProfile ? RunAsync(() => Clerk.Organization.SetLogoAsync(_logo)) : Task.CompletedTask;
    private Task RemoveLogoAsync() => CanProfile ? RunAsync(() => Clerk.Organization.RemoveLogoAsync()) : Task.CompletedTask;
    private async Task UpdateMemberAsync(OrganizationMember member, string? role)
    {
        var id = member.PublicUserData?.UserId;
        if (!CanMembers || id is null || id == State.User?.Id || role is null || !_roles.Any(r => r.Key == role)) return;
        await RunAsync(async () => { await Clerk.Organization.UpdateMemberAsync(id, role); await LoadMemberTabCoreAsync(); });
    }
    private async Task RemoveMemberAsync(OrganizationMember member)
    {
        var id = member.PublicUserData?.UserId;
        if (!CanMembers || id is null || id == State.User?.Id || _removeMember != member.Id) return;
        await RunAsync(async () => {
            await Clerk.Organization.RemoveMemberAsync(id); _removeMember = null;
            if (_members.Data.Count == 1 && _memberPage > 1) _memberPage--;
            await LoadMemberTabCoreAsync();
        });
    }
    private async Task InviteAsync()
    {
        var emails = OrganizationUi.Emails(_emails);
        if (!CanMembers || AtMemberLimit || emails.Length == 0 || string.IsNullOrEmpty(_inviteRole)) return;
        await RunAsync(async () => { await Clerk.Organization.InviteMembersAsync(emails, _inviteRole); _emails = ""; _invitesSent = true; _invitationPage = 1; await LoadMemberTabCoreAsync(); });
    }
    private Task RevokeAsync(OrganizationInvitation invitation) => !CanMembers || _revokeInvitation != invitation.Id ? Task.CompletedTask : RunAsync(async () => {
        await Clerk.Organization.RevokeInvitationAsync(invitation); _revokeInvitation = null;
        if (_invitations.Data.Count == 1 && _invitationPage > 1) _invitationPage--;
        await LoadMemberTabCoreAsync();
    });
    private Task RequestAsync(OrganizationMembershipRequest request, bool accept) => !CanMembers || (!accept && _rejectRequest != request.Id) ? Task.CompletedTask : RunAsync(async () => {
        if (accept) await Clerk.Organization.AcceptMembershipRequestAsync(request); else await Clerk.Organization.RejectMembershipRequestAsync(request);
        _rejectRequest = null;
        if (_requests.Data.Count == 1 && _requestPage > 1) _requestPage--;
        await LoadMemberTabCoreAsync();
    });
    private Task AddDomainAsync() => !CanDomains || string.IsNullOrWhiteSpace(_domainName) ? Task.CompletedTask : RunAsync(async () => {
        var domain = await Clerk.Organization.CreateDomainAsync(_domainName.Trim()); _domainName = "";
        StartVerification(domain); _domainPage = 1; _domains = await Clerk.Organization.GetDomainsAsync();
    });
    private void StartVerification(OrganizationDomain domain) { _verifying = domain; _affiliationEmail = domain.AffiliationEmailAddress ?? ""; _code = ""; _codeSent = false; Error = null; }
    private void CancelVerification() { _verifying = null; _codeSent = false; Error = null; }
    private Task VerifyDomainAsync() => !CanDomains || _verifying is null ? Task.CompletedTask : RunAsync(async () => {
        if (!_codeSent) { await Clerk.Organization.PrepareDomainVerificationAsync(_verifying, _affiliationEmail.Trim()); _codeSent = true; }
        else { await Clerk.Organization.AttemptDomainVerificationAsync(_verifying, _code.Trim()); _verifying = null; _domains = await Clerk.Organization.GetDomainsAsync(_domainPage); }
    });
    private static string EnrollmentValue(OrganizationDomain domain) => domain.EnrollmentMode is null or "manual_invitation" ? "none" : domain.EnrollmentMode;
    private string EnrollmentLabel(string mode) => mode switch { "automatic_invitation" => L.OrgEnrollmentInvitation, "automatic_suggestion" => L.OrgEnrollmentSuggestion, _ => L.OrgEnrollmentNone };
    private Task EnrollmentAsync(OrganizationDomain domain, string? mode) => !CanDomains || mode is null || !EnrollmentModes.Contains(mode) ? Task.CompletedTask : RunAsync(async () => {
        await Clerk.Organization.UpdateDomainEnrollmentModeAsync(domain, mode == "none" ? "manual_invitation" : mode); _domains = await Clerk.Organization.GetDomainsAsync(_domainPage);
    });
    private Task DeleteDomainAsync(OrganizationDomain domain) => !CanDomains || _removeDomain != domain.Id ? Task.CompletedTask : RunAsync(async () => {
        await Clerk.Organization.DeleteDomainAsync(domain); _removeDomain = null;
        if (_verifying?.Id == domain.Id) CancelVerification();
        if (_domains.Data.Count == 1 && _domainPage > 1) _domainPage--;
        _domains = await Clerk.Organization.GetDomainsAsync(_domainPage);
    });
    private async Task LeaveAsync()
    {
        if (!_confirmLeave || !ContextMatches || _orgId is null) return;
        if (await RunAsync(async () => {
            await Clerk.Organization.LeaveAsync(_orgId);
            await Clerk.Organization.SetActiveAsync(null, LeaveRedirect);
        })) await OnLeave.InvokeAsync();
    }
    private async Task DeleteAsync()
    {
        if (!CanDelete || State.Organization is not { } org || _deleteName != org.Name) return;
        if (await RunAsync(async () => {
            await Clerk.Organization.DestroyAsync();
            await Clerk.Organization.SetActiveAsync(null, LeaveRedirect);
        })) await OnLeave.InvokeAsync();
    }
    public override void Dispose() { Navigation.LocationChanged -= LocationChanged; _disposed = true; _registration.Changed = null; base.Dispose(); }
}
