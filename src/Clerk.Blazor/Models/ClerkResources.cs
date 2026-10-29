using System.Text.Json;

namespace Clerk.Blazor;

/// <summary>Clerk load status, equivalent to <c>clerk.status</c>.</summary>
public enum ClerkStatus
{
    /// <summary>ClerkJS is loading.</summary>
    Loading,
    /// <summary>ClerkJS is ready.</summary>
    Ready,
    /// <summary>ClerkJS is running from cached resources because the Frontend API is unreachable.</summary>
    Degraded,
    /// <summary>ClerkJS failed to load.</summary>
    Error,
}

public sealed record ClerkVerification
{
    public string? Status { get; init; }
    public string? Strategy { get; init; }
    public int? Attempts { get; init; }
    public DateTimeOffset? ExpireAt { get; init; }
    public string? ExternalVerificationRedirectUrl { get; init; }
    public ClerkApiError? Error { get; init; }
    public string? Nonce { get; init; }
    public bool IsVerified => Status == "verified";
}

public sealed record ClerkEmailAddress
{
    public string Id { get; init; } = "";
    public string EmailAddress { get; init; } = "";
    public ClerkVerification? Verification { get; init; }
    public IReadOnlyList<ClerkLinkedIdentification> LinkedTo { get; init; } = [];
}

public sealed record ClerkLinkedIdentification(string Id, string Type);

public sealed record ClerkPhoneNumber
{
    public string Id { get; init; } = "";
    public string PhoneNumber { get; init; } = "";
    public ClerkVerification? Verification { get; init; }
    public bool ReservedForSecondFactor { get; init; }
    public bool DefaultSecondFactor { get; init; }
}

public sealed record ClerkWeb3Wallet
{
    public string Id { get; init; } = "";
    public string Web3Wallet { get; init; } = "";
    public ClerkVerification? Verification { get; init; }
}

public sealed record ClerkExternalAccount
{
    public string Id { get; init; } = "";
    public string Provider { get; init; } = "";
    public string? ProviderUserId { get; init; }
    public string? EmailAddress { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    public string ApprovedScopes { get; init; } = "";
    public ClerkVerification? Verification { get; init; }
}

public sealed record ClerkEnterpriseAccount
{
    public string Id { get; init; } = "";
    public string? Provider { get; init; }
    public string? EmailAddress { get; init; }
    public string? Name { get; init; }
    public bool Active { get; init; }
}

public sealed record ClerkPasskey
{
    public string Id { get; init; } = "";
    public string? Name { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
}

public sealed record ClerkPublicUserData
{
    public string? UserId { get; init; }
    public string? Identifier { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } n ? n : Identifier ?? "";
}

/// <summary>Snapshot of an organization (<c>OrganizationResource</c>).</summary>
public sealed record ClerkOrganization
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Slug { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
    public int MembersCount { get; init; }
    public int PendingInvitationsCount { get; init; }
    public bool AdminDeleteEnabled { get; init; }
    public int MaxAllowedMemberships { get; init; }
    public JsonElement PublicMetadata { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>Snapshot of an organization membership.</summary>
public sealed record ClerkOrganizationMembership
{
    public string Id { get; init; } = "";
    public string Role { get; init; } = "";
    public string? RoleName { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public ClerkOrganization? Organization { get; init; }
    public ClerkPublicUserData? PublicUserData { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Snapshot of the signed-in user (<c>UserResource</c>).</summary>
public sealed record ClerkUser
{
    public string Id { get; init; } = "";
    public string? ExternalId { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
    public string? PrimaryEmailAddressId { get; init; }
    public string? PrimaryPhoneNumberId { get; init; }
    public string? PrimaryWeb3WalletId { get; init; }
    public IReadOnlyList<ClerkEmailAddress> EmailAddresses { get; init; } = [];
    public IReadOnlyList<ClerkPhoneNumber> PhoneNumbers { get; init; } = [];
    public IReadOnlyList<ClerkWeb3Wallet> Web3Wallets { get; init; } = [];
    public IReadOnlyList<ClerkExternalAccount> ExternalAccounts { get; init; } = [];
    public IReadOnlyList<ClerkEnterpriseAccount> EnterpriseAccounts { get; init; } = [];
    public IReadOnlyList<ClerkPasskey> Passkeys { get; init; } = [];
    public IReadOnlyList<ClerkOrganizationMembership> OrganizationMemberships { get; init; } = [];
    public bool PasswordEnabled { get; init; }
    public bool TotpEnabled { get; init; }
    public bool BackupCodeEnabled { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public bool DeleteSelfEnabled { get; init; }
    public bool CreateOrganizationEnabled { get; init; }
    public int? CreateOrganizationsLimit { get; init; }
    public JsonElement PublicMetadata { get; init; }
    public JsonElement UnsafeMetadata { get; init; }
    public DateTimeOffset? LastSignInAt { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public ClerkEmailAddress? PrimaryEmailAddress => EmailAddresses.FirstOrDefault(e => e.Id == PrimaryEmailAddressId);
    public ClerkPhoneNumber? PrimaryPhoneNumber => PhoneNumbers.FirstOrDefault(p => p.Id == PrimaryPhoneNumberId);
    public ClerkWeb3Wallet? PrimaryWeb3Wallet => Web3Wallets.FirstOrDefault(w => w.Id == PrimaryWeb3WalletId);

    /// <summary>Best human-readable name: full name, username, or primary identifier.</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(FullName) ? FullName!
        : !string.IsNullOrWhiteSpace(Username) ? Username!
        : PrimaryEmailAddress?.EmailAddress ?? PrimaryPhoneNumber?.PhoneNumber ?? Id;

    /// <summary>Primary identifier (email, phone, username or wallet).</summary>
    public string? Identifier => PrimaryEmailAddress?.EmailAddress ?? PrimaryPhoneNumber?.PhoneNumber ?? Username ?? PrimaryWeb3Wallet?.Web3Wallet;
}

public sealed record ClerkSessionUser
{
    public string Id { get; init; } = "";
    public string? FullName { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
    public string? Identifier { get; init; }
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName! : Identifier ?? Id;
}

public sealed record ClerkSessionTask(string Key);

/// <summary>Snapshot of a session (<c>SessionResource</c>).</summary>
public sealed record ClerkSession
{
    public string Id { get; init; } = "";
    /// <summary><c>active</c>, <c>pending</c>, <c>ended</c>, <c>expired</c>, <c>removed</c>, <c>replaced</c>, <c>revoked</c>.</summary>
    public string Status { get; init; } = "";
    public DateTimeOffset? ExpireAt { get; init; }
    public DateTimeOffset? AbandonAt { get; init; }
    public DateTimeOffset? LastActiveAt { get; init; }
    public string? LastActiveOrganizationId { get; init; }
    public JsonElement? Actor { get; init; }
    /// <summary>Minutes since first and second factor verification (<c>-1</c> when never).</summary>
    public int[]? FactorVerificationAge { get; init; }
    public ClerkSessionTask? CurrentTask { get; init; }
    public JsonElement? Claims { get; init; }
    public ClerkSessionUser? User { get; init; }
    public bool IsPending => Status == "pending";
}

public sealed record ClerkClient
{
    public string? Id { get; init; }
    public IReadOnlyList<ClerkSession> Sessions { get; init; } = [];
    public string? LastActiveSessionId { get; init; }
    public string? SignInStatus { get; init; }
    public string? SignUpStatus { get; init; }
}

public sealed record ClerkAttributeSettings
{
    public bool Enabled { get; init; }
    public bool Required { get; init; }
    public bool UsedForFirstFactor { get; init; }
    public IReadOnlyList<string> FirstFactors { get; init; } = [];
    public bool UsedForSecondFactor { get; init; }
    public IReadOnlyList<string> SecondFactors { get; init; } = [];
    public IReadOnlyList<string> Verifications { get; init; } = [];
}

public sealed record ClerkSocialProvider(string Strategy, string Name, string? LogoUrl);

public sealed record ClerkOrganizationSettings
{
    public bool Enabled { get; init; }
    public int MaxAllowedMemberships { get; init; }
    public bool ForceOrganizationSelection { get; init; }
    public bool AdminDeleteEnabled { get; init; }
    public bool DomainsEnabled { get; init; }
    public IReadOnlyList<string> DomainEnrollmentModes { get; init; } = [];
    public bool SlugDisabled { get; init; }
    public bool CreationDefaultsEnabled { get; init; }
}

public sealed record ClerkBillingSettings
{
    public bool UserEnabled { get; init; }
    public bool UserHasPaidPlans { get; init; }
    public bool OrganizationEnabled { get; init; }
    public bool OrganizationHasPaidPlans { get; init; }
    public string? StripePublishableKey { get; init; }
}

public sealed record ClerkApiKeySettings
{
    public bool UserEnabled { get; init; }
    public bool OrganizationEnabled { get; init; }
}

/// <summary>Instance configuration from the Clerk Dashboard (<c>EnvironmentResource</c>).</summary>
public sealed record ClerkEnvironment
{
    public string ApplicationName { get; init; } = "";
    public string? LogoImageUrl { get; init; }
    public string? FaviconImageUrl { get; init; }
    public string? HomeUrl { get; init; }
    public bool Branded { get; init; }
    public string? PrivacyPolicyUrl { get; init; }
    public string? TermsUrl { get; init; }
    public string? SupportEmail { get; init; }
    public string? GoogleOneTapClientId { get; init; }
    public string PreferredSignInStrategy { get; init; } = "password";
    public string? InstanceEnvironmentType { get; init; }
    /// <summary><c>public</c>, <c>restricted</c> or <c>waitlist</c>.</summary>
    public string SignUpMode { get; init; } = "public";
    public bool LegalConsentEnabled { get; init; }
    public IReadOnlyDictionary<string, ClerkAttributeSettings> Attributes { get; init; } = new Dictionary<string, ClerkAttributeSettings>();
    public IReadOnlyList<ClerkSocialProvider> SocialProviders { get; init; } = [];
    public IReadOnlyList<string> Web3Strategies { get; init; } = [];
    public bool EnterpriseSsoEnabled { get; init; }
    /// <summary><c>false</c> when the instance allows multiple signed-in accounts per browser.</summary>
    public bool SingleSessionMode { get; init; } = true;
    public int PasswordMinLength { get; init; } = 8;
    public int PasswordMaxLength { get; init; } = 72;
    public bool DeleteSelfEnabled { get; init; }
    public bool CreateOrganizationEnabled { get; init; }
    public ClerkOrganizationSettings Organizations { get; init; } = new();
    public ClerkBillingSettings Billing { get; init; } = new();
    public ClerkApiKeySettings ApiKeys { get; init; } = new();

    public ClerkAttributeSettings Attribute(string name) =>
        Attributes.TryGetValue(name, out var a) ? a : new ClerkAttributeSettings();

    public bool IsDevelopment => InstanceEnvironmentType == "development";
}

/// <summary>Raw snapshot pushed from the bridge.</summary>
internal sealed record ClerkSnapshot
{
    public long Seq { get; init; }
    public string Status { get; init; } = "loading";
    public bool Loaded { get; init; }
    public string? Version { get; init; }
    public ClerkClient? Client { get; init; }
    public ClerkSession? Session { get; init; }
    public ClerkUser? User { get; init; }
    public ClerkOrganization? Organization { get; init; }
    public ClerkOrganizationMembership? Membership { get; init; }
    public ClerkEnvironment? Environment { get; init; }
}
