namespace Clerk.Blazor;

public partial class ClerkLocalization
{
    public string OrgLogo { get; set; } = "Organization logo";
    public string OrgSkipInvitations { get; set; } = "Skip for now";
    public string OrgCreationAdvisory { get; set; } = "Review the suggested organization details before continuing.";
    public string OrgCreationUnavailable { get; set; } = "Organization creation is not available for your account.";
    public string OrgCreationSubtitle { get; set; } = "Set up a shared workspace for your team.";
    public string OrgSuggestions { get; set; } = "Organizations you can join";
    public string OrgLoadMore { get; set; } = "Load more";
    public string OrgCurrent { get; set; } = "Current";
    public string OrgProfile { get; set; } = "Organization profile";
    public string OrgSelectFirst { get; set; } = "Choose an organization to manage its settings.";
    public string OrgDomainName { get; set; } = "Domain name";
    public string OrgAffiliationEmail { get; set; } = "Email address at this domain";
    public string OrgSendCode { get; set; } = "Send verification code";
    public string OrgEnrollment { get; set; } = "Enrollment mode";
    public string OrgEnrollmentNone { get; set; } = "No automatic enrollment";
    public string OrgEnrollmentInvitation { get; set; } = "Automatically invite";
    public string OrgEnrollmentSuggestion { get; set; } = "Automatically suggest";
    public string OrgVerified { get; set; } = "Verified";
    public string OrgUnverified { get; set; } = "Not verified";
    public string OrgLeaveWarning { get; set; } = "You will lose access to this organization's resources.";
    public string OrgDeleteWarning { get; set; } = "This permanently deletes the organization and its associated data. This cannot be undone.";
    public string OrgRemoveMemberWarning { get; set; } = "Remove {0} from this organization?";
    public string OrgRemoveDomainWarning { get; set; } = "Remove the domain {0}?";
    public string OrgRejectWarning { get; set; } = "Reject the membership request for {0}?";
    public string OrgRevokeWarning { get; set; } = "Revoke the invitation for {0}?";
    public string OrgSearchMembers { get; set; } = "Search members";
    public string OrgEmails { get; set; } = "Email addresses";
    public string OrgInvitationsSent { get; set; } = "Invitations sent.";
    public string OrgNoMemberships { get; set; } = "You don't belong to an organization yet. Accept an invitation or create an organization to get started.";
    public string OrgNoMembers { get; set; } = "No members match your search.";
    public string OrgNoInvitations { get; set; } = "There are no pending invitations. Invite teammates to work together.";
    public string OrgNoRequests { get; set; } = "There are no pending membership requests.";
    public string OrgNoDomains { get; set; } = "Add and verify a domain to help teammates find your organization.";
    public string OrgMember { get; set; } = "Member";
    public string OrgMemberRole { get; set; } = "Role for {0}";
    public string OrgActions { get; set; } = "Actions";
    public string OrgPermissionRequired { get; set; } = "You don't have permission to manage this section.";
    public string OrgMemberLimit { get; set; } = "This organization has reached its membership limit.";
    public string OrgApiKeys { get; set; } = "API keys";
    public string OrgBilling { get; set; } = "Billing";
    public string OrgRetry { get; set; } = "Try again";
}
