using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;

namespace Clerk.Blazor;

// ── Typed resource snapshots ────────────────────────────────────────────────

public sealed record SignInFactor
{
    public string Strategy { get; init; } = "";
    public string? SafeIdentifier { get; init; }
    public string? EmailAddressId { get; init; }
    public string? PhoneNumberId { get; init; }
    public string? Web3WalletId { get; init; }
    public bool Primary { get; init; }
    public bool Default { get; init; }
}

public sealed record SignInUserData
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
}

/// <summary>Snapshot of <c>SignInResource</c>.</summary>
public sealed record ClerkSignInAttempt
{
    public string? Id { get; init; }
    /// <summary><c>needs_identifier</c>, <c>needs_first_factor</c>, <c>needs_second_factor</c>, <c>needs_new_password</c>, <c>needs_client_trust</c>, <c>complete</c>.</summary>
    public string? Status { get; init; }
    public string? Identifier { get; init; }
    public IReadOnlyList<SignInFactor> SupportedFirstFactors { get; init; } = [];
    public IReadOnlyList<SignInFactor> SupportedSecondFactors { get; init; } = [];
    public ClerkVerification? FirstFactorVerification { get; init; }
    public ClerkVerification? SecondFactorVerification { get; init; }
    public string? CreatedSessionId { get; init; }
    public SignInUserData? UserData { get; init; }
}

public sealed record SignUpVerification
{
    public string? Status { get; init; }
    public string? Strategy { get; init; }
    public string? NextAction { get; init; }
    public IReadOnlyList<string> SupportedStrategies { get; init; } = [];
    public string? ExternalVerificationRedirectUrl { get; init; }
    public ClerkApiError? Error { get; init; }
}

public sealed record SignUpVerifications
{
    public SignUpVerification? EmailAddress { get; init; }
    public SignUpVerification? PhoneNumber { get; init; }
    public SignUpVerification? ExternalAccount { get; init; }
    public SignUpVerification? Web3Wallet { get; init; }
}

/// <summary>Snapshot of <c>SignUpResource</c>.</summary>
public sealed record ClerkSignUpAttempt
{
    public string? Id { get; init; }
    /// <summary><c>missing_requirements</c>, <c>complete</c> or <c>abandoned</c>.</summary>
    public string? Status { get; init; }
    public IReadOnlyList<string> RequiredFields { get; init; } = [];
    public IReadOnlyList<string> OptionalFields { get; init; } = [];
    public IReadOnlyList<string> MissingFields { get; init; } = [];
    public IReadOnlyList<string> UnverifiedFields { get; init; } = [];
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Web3Wallet { get; init; }
    public SignUpVerifications? Verifications { get; init; }
    public string? CreatedSessionId { get; init; }
}

public sealed record SessionActivity
{
    public string? BrowserName { get; init; }
    public string? BrowserVersion { get; init; }
    public string? DeviceType { get; init; }
    public string? IpAddress { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public bool IsMobile { get; init; }
}

/// <summary>A user's session with device activity (<c>SessionWithActivitiesResource</c>).</summary>
public sealed record ClerkUserSession
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset? LastActiveAt { get; init; }
    public DateTimeOffset? ExpireAt { get; init; }
    public SessionActivity? LatestActivity { get; init; }
}

public sealed record TotpSecret(string? Secret, string? Uri, IReadOnlyList<string>? BackupCodes);

public sealed record OrganizationRole
{
    public string Id { get; init; } = "";
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public IReadOnlyList<JsonElement> Permissions { get; init; } = [];
}

public sealed record OrganizationInvitation
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string EmailAddress { get; init; } = "";
    public string Role { get; init; } = "";
    public string? RoleName { get; init; }
    public string Status { get; init; } = "";
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed record OrganizationMember
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Role { get; init; } = "";
    public string? RoleName { get; init; }
    public ClerkPublicUserData? PublicUserData { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed record OrganizationMembershipRequest
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public ClerkPublicUserData? PublicUserData { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed record OrganizationDomain
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? EnrollmentMode { get; init; }
    public ClerkVerification? Verification { get; init; }
    public string? AffiliationEmailAddress { get; init; }
    public int TotalPendingInvitations { get; init; }
    public int TotalPendingSuggestions { get; init; }
}

public sealed record UserOrganizationInvitation
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Role { get; init; } = "";
    public string Status { get; init; } = "";
    public PublicOrganizationData? PublicOrganizationData { get; init; }
}

public sealed record OrganizationSuggestion
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public PublicOrganizationData? PublicOrganizationData { get; init; }
}

public sealed record PublicOrganizationData
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Slug { get; init; }
    public string? ImageUrl { get; init; }
    public bool HasImage { get; init; }
}

public sealed record OrganizationCreationDefaults
{
    public OrganizationCreationAdvisory? Advisory { get; init; }
    public OrganizationCreationForm? Form { get; init; }
}

public sealed record OrganizationCreationAdvisory(string? Code, string? Severity);
public sealed record OrganizationCreationForm(string? Name, string? Slug, string? Logo);

public sealed record BillingMoney
{
    public long Amount { get; init; }
    public string AmountFormatted { get; init; } = "";
    public string Currency { get; init; } = "";
    public string CurrencySymbol { get; init; } = "";
    public override string ToString() => $"{CurrencySymbol}{AmountFormatted}";
}

public sealed record BillingFeature
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
}

public sealed record BillingPlan
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Description { get; init; }
    public BillingMoney? Fee { get; init; }
    public BillingMoney? AnnualFee { get; init; }
    public BillingMoney? AnnualMonthlyFee { get; init; }
    public bool IsDefault { get; init; }
    public bool IsRecurring { get; init; }
    public bool HasBaseFee { get; init; }
    public string ForPayerType { get; init; } = "user";
    public bool PubliclyVisible { get; init; }
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<BillingFeature> Features { get; init; } = [];
    public bool FreeTrialEnabled { get; init; }
    public int? FreeTrialDays { get; init; }
    public bool IsFree => Fee is null || Fee.Amount == 0;
}

public sealed record BillingSubscriptionItem
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public BillingPlan? Plan { get; init; }
    public string PlanPeriod { get; init; } = "month";
    public string Status { get; init; } = "";
    public DateTimeOffset? PeriodStart { get; init; }
    public DateTimeOffset? PeriodEnd { get; init; }
    public DateTimeOffset? CanceledAt { get; init; }
    public BillingMoney? Amount { get; init; }
    public bool IsFreeTrial { get; init; }
}

public sealed record BillingNextPayment(BillingMoney? Amount, DateTimeOffset? Date);

public sealed record BillingSubscription
{
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset? ActiveAt { get; init; }
    public DateTimeOffset? PastDueAt { get; init; }
    public BillingNextPayment? NextPayment { get; init; }
    public IReadOnlyList<BillingSubscriptionItem> SubscriptionItems { get; init; } = [];
    public bool EligibleForFreeTrial { get; init; }
}

public sealed record BillingPaymentMethod
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string? CardType { get; init; }
    public string? Last4 { get; init; }
    public string? WalletType { get; init; }
    public int? ExpiryMonth { get; init; }
    public int? ExpiryYear { get; init; }
    public bool IsDefault { get; init; }
    public bool IsRemovable { get; init; }
    public string? Status { get; init; }
}

public sealed record BillingTotals
{
    public BillingMoney? Subtotal { get; init; }
    public BillingMoney? GrandTotal { get; init; }
    public BillingMoney? TaxTotal { get; init; }
    public BillingMoney? TotalDueNow { get; init; }
    public BillingMoney? Credit { get; init; }
    public BillingMoney? PastDue { get; init; }
}

public sealed record BillingCheckout
{
    [JsonPropertyName("$handle")] public string? Handle { get; init; }
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public BillingPlan? Plan { get; init; }
    public string PlanPeriod { get; init; } = "month";
    public BillingTotals? Totals { get; init; }
    public bool IsImmediatePlanChange { get; init; }
    public bool NeedsPaymentMethod { get; init; }
    public DateTimeOffset? FreeTrialEndsAt { get; init; }
    public BillingPaymentMethod? PaymentMethod { get; init; }
}

public sealed record BillingPayment
{
    public string Id { get; init; } = "";
    public BillingMoney? Amount { get; init; }
    public string Status { get; init; } = "";
    public string? ChargeType { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public DateTimeOffset? FailedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public BillingPaymentMethod? PaymentMethod { get; init; }
    public BillingSubscriptionItem? SubscriptionItem { get; init; }
}

public sealed record BillingStatementGroup(string Id, DateTimeOffset? Timestamp, IReadOnlyList<BillingPayment> Items);

public sealed record BillingStatement
{
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset? Timestamp { get; init; }
    public BillingTotals? Totals { get; init; }
    public IReadOnlyList<BillingStatementGroup> Groups { get; init; } = [];
}

public sealed record ClerkApiKey
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? Subject { get; init; }
    public IReadOnlyList<string> Scopes { get; init; } = [];
    public bool Revoked { get; init; }
    public string? RevocationReason { get; init; }
    public bool Expired { get; init; }
    public DateTimeOffset? Expiration { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    /// <summary>Only present immediately after creation.</summary>
    public string? Secret { get; init; }
}

public sealed record OAuthScope(string Scope, string? Description, bool RequiresConsent);

public sealed record OAuthConsentInfo
{
    public string OauthApplicationName { get; init; } = "";
    public string? OauthApplicationLogoUrl { get; init; }
    public string? OauthApplicationUrl { get; init; }
    public string ClientId { get; init; } = "";
    public string? State { get; init; }
    public string? RedirectDomain { get; init; }
    public IReadOnlyList<OAuthScope> Scopes { get; init; } = [];
}

public sealed record SessionVerification
{
    public string Status { get; init; } = "";
    public string? Level { get; init; }
    public IReadOnlyList<SignInFactor> SupportedFirstFactors { get; init; } = [];
    public IReadOnlyList<SignInFactor> SupportedSecondFactors { get; init; } = [];
    public ClerkVerification? FirstFactorVerification { get; init; }
    public ClerkVerification? SecondFactorVerification { get; init; }
}

// ── Parameter records ───────────────────────────────────────────────────────

public sealed record SignUpFields
{
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Strategy { get; init; }
    public string? Ticket { get; init; }
    public bool? LegalAccepted { get; init; }
    public bool? Transfer { get; init; }
    public object? UnsafeMetadata { get; init; }

    internal Dictionary<string, object?> ToArgs() => ClerkArgs.Of(
        ("emailAddress", EmailAddress), ("phoneNumber", PhoneNumber), ("username", Username), ("password", Password),
        ("firstName", FirstName), ("lastName", LastName), ("strategy", Strategy), ("ticket", Ticket),
        ("legalAccepted", LegalAccepted), ("transfer", Transfer), ("unsafeMetadata", UnsafeMetadata));
}

// ── APIs ────────────────────────────────────────────────────────────────────

/// <summary>Base for feature APIs.</summary>
public abstract class ClerkApiBase(ClerkService clerk)
{
    protected ClerkService Clerk { get; } = clerk;

    protected async Task<T> Call<T>(string path, params object?[] args) => (await Clerk.InvokeAsync(path, args)).As<T>();

    protected Task<ClerkResource> Call(string path, params object?[] args) => Clerk.InvokeAsync(path, args);

    protected async Task<ClerkPage<T>> Page<T>(string path, object? args)
    {
        var res = await Clerk.InvokeAsync(path, args);
        return new ClerkPage<T>(res.Items().Select(i => i.As<T>()).ToList(), res.TotalCount);
    }

    protected static object Paging(int page, int pageSize, params (string, object?)[] extra) =>
        ClerkArgs.Of([("initialPage", page), ("pageSize", pageSize), .. extra]);
}

/// <summary>Sign-in operations (<c>useSignIn</c>). All state is held by ClerkJS's <c>client.signIn</c>.</summary>
public sealed class SignInApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    /// <summary>Current sign-in attempt.</summary>
    public Task<ClerkSignInAttempt> GetCurrentAsync() => Call<ClerkSignInAttempt>("client.signIn");

    public Task<ClerkSignInAttempt> CreateAsync(string? identifier = null, string? password = null, string? strategy = null, string? ticket = null, bool? transfer = null) =>
        Call<ClerkSignInAttempt>("client.signIn.create", ClerkArgs.Of(("identifier", identifier), ("password", password), ("strategy", strategy), ("ticket", ticket), ("transfer", transfer)));

    public Task<ClerkSignInAttempt> PrepareFirstFactorAsync(SignInFactor factor, string? redirectUrl = null) =>
        Call<ClerkSignInAttempt>("client.signIn.prepareFirstFactor", ClerkArgs.Of(
            ("strategy", factor.Strategy), ("emailAddressId", factor.EmailAddressId), ("phoneNumberId", factor.PhoneNumberId),
            ("web3WalletId", factor.Web3WalletId), ("redirectUrl", redirectUrl)));

    public Task<ClerkSignInAttempt> AttemptFirstFactorAsync(string strategy, string? code = null, string? password = null) =>
        Call<ClerkSignInAttempt>("client.signIn.attemptFirstFactor", ClerkArgs.Of(("strategy", strategy), ("code", code), ("password", password)));

    public Task<ClerkSignInAttempt> PrepareSecondFactorAsync(SignInFactor factor) =>
        Call<ClerkSignInAttempt>("client.signIn.prepareSecondFactor", ClerkArgs.Of(("strategy", factor.Strategy), ("phoneNumberId", factor.PhoneNumberId), ("emailAddressId", factor.EmailAddressId)));

    public Task<ClerkSignInAttempt> AttemptSecondFactorAsync(string strategy, string code) =>
        Call<ClerkSignInAttempt>("client.signIn.attemptSecondFactor", ClerkArgs.Of(("strategy", strategy), ("code", code)));

    public Task<ClerkSignInAttempt> ResetPasswordAsync(string password, bool signOutOfOtherSessions = true) =>
        Call<ClerkSignInAttempt>("client.signIn.resetPassword", ClerkArgs.Of(("password", password), ("signOutOfOtherSessions", signOutOfOtherSessions)));

    /// <summary>Starts an OAuth or enterprise SSO redirect.</summary>
    public Task AuthenticateWithRedirectAsync(string strategy, string redirectUrl, string redirectUrlComplete, string? identifier = null) =>
        Call("client.signIn.authenticateWithRedirect", ClerkArgs.Of(("strategy", strategy), ("redirectUrl", redirectUrl), ("redirectUrlComplete", redirectUrlComplete), ("identifier", identifier)));

    public Task<ClerkSignInAttempt> AuthenticateWithPasskeyAsync(string? flow = null) =>
        Call<ClerkSignInAttempt>("client.signIn.authenticateWithPasskey", ClerkArgs.Of(("flow", flow)));

    /// <summary>Web3 sign-in/up (MetaMask, OKX Wallet, Solana). Base and Coinbase Wallet are not supported.</summary>
    public Task AuthenticateWithWeb3Async(string strategy, string? redirectUrl = null, string? walletName = null) =>
        Call("authenticateWithWeb3", ClerkArgs.Of(("strategy", strategy), ("redirectUrl", redirectUrl), ("walletName", walletName),
            ("signUpContinueUrl", $"{Clerk.Options.SignUpUrl.TrimEnd('/')}/continue"), ("secondFactorUrl", $"{Clerk.Options.SignInUrl.TrimEnd('/')}/factor-two")));

    /// <summary>Sends a magic link and waits until it is opened (in any tab).</summary>
    public async Task<ClerkSignInAttempt> SignInWithEmailLinkAsync(string emailAddressId, string redirectUrl) =>
        (await Clerk.CallBridgeAsync("signInWithEmailLink", emailAddressId, redirectUrl)).Deserialize<ClerkSignInAttempt>(ClerkJson.Options)!;
}

/// <summary>Sign-up operations (<c>useSignUp</c>).</summary>
public sealed class SignUpApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task<ClerkSignUpAttempt> GetCurrentAsync() => Call<ClerkSignUpAttempt>("client.signUp");

    public Task<ClerkSignUpAttempt> CreateAsync(SignUpFields fields) => Call<ClerkSignUpAttempt>("client.signUp.create", fields.ToArgs());

    public Task<ClerkSignUpAttempt> UpdateAsync(SignUpFields fields) => Call<ClerkSignUpAttempt>("client.signUp.update", fields.ToArgs());

    public Task<ClerkSignUpAttempt> PrepareEmailVerificationAsync(string strategy = "email_code", string? redirectUrl = null) =>
        Call<ClerkSignUpAttempt>("client.signUp.prepareEmailAddressVerification", ClerkArgs.Of(("strategy", strategy), ("redirectUrl", redirectUrl)));

    public Task<ClerkSignUpAttempt> AttemptEmailVerificationAsync(string code) =>
        Call<ClerkSignUpAttempt>("client.signUp.attemptEmailAddressVerification", ClerkArgs.Of(("code", code)));

    public Task<ClerkSignUpAttempt> PreparePhoneVerificationAsync(string strategy = "phone_code") =>
        Call<ClerkSignUpAttempt>("client.signUp.preparePhoneNumberVerification", ClerkArgs.Of(("strategy", strategy)));

    public Task<ClerkSignUpAttempt> AttemptPhoneVerificationAsync(string code) =>
        Call<ClerkSignUpAttempt>("client.signUp.attemptPhoneNumberVerification", ClerkArgs.Of(("code", code)));

    public Task AuthenticateWithRedirectAsync(string strategy, string redirectUrl, string redirectUrlComplete, bool? legalAccepted = null, object? unsafeMetadata = null) =>
        Call("client.signUp.authenticateWithRedirect", ClerkArgs.Of(("strategy", strategy), ("redirectUrl", redirectUrl), ("redirectUrlComplete", redirectUrlComplete),
            ("legalAccepted", legalAccepted), ("unsafeMetadata", unsafeMetadata), ("continueSignUp", true)));

    public async Task<ClerkSignUpAttempt> SignUpWithEmailLinkAsync(string redirectUrl) =>
        (await Clerk.CallBridgeAsync("signUpWithEmailLink", redirectUrl)).Deserialize<ClerkSignUpAttempt>(ClerkJson.Options)!;
}

/// <summary>Operations on the signed-in user (<c>useUser</c>).</summary>
public sealed class UserApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task UpdateAsync(string? firstName = null, string? lastName = null, string? username = null, object? unsafeMetadata = null) =>
        Call("user.update", ClerkArgs.Of(("firstName", firstName), ("lastName", lastName), ("username", username), ("unsafeMetadata", unsafeMetadata)));

    public Task UpdateMetadataAsync(object unsafeMetadata) => Call("user.updateMetadata", ClerkArgs.Of(("unsafeMetadata", unsafeMetadata)));

    public Task ReloadAsync() => Call("user.reload");

    /// <summary>Uploads the file selected in an <c>&lt;input type=file&gt;</c>.</summary>
    public Task SetProfileImageAsync(ElementReference fileInput) => Clerk.CallBridgeAsync("uploadImage", "user", fileInput);

    public Task RemoveProfileImageAsync() => Call("user.setProfileImage", new Dictionary<string, object?> { ["file"] = null });

    // Email addresses
    public Task<ClerkResource> CreateEmailAddressAsync(string email) => Call("user.createEmailAddress", ClerkArgs.Of(("email", email)));
    public Task PrepareEmailVerificationAsync(string id, string strategy = "email_code") => Call($"user.emailAddresses[{id}].prepareVerification", ClerkArgs.Of(("strategy", strategy)));
    public Task AttemptEmailVerificationAsync(string id, string code) => Call($"user.emailAddresses[{id}].attemptVerification", ClerkArgs.Of(("code", code)));
    public Task RemoveEmailAddressAsync(string id) => Call($"user.emailAddresses[{id}].destroy");
    public Task SetPrimaryEmailAddressAsync(string id) => Call("user.update", ClerkArgs.Of(("primaryEmailAddressId", id)));

    // Phone numbers
    public Task<ClerkResource> CreatePhoneNumberAsync(string phoneNumber) => Call("user.createPhoneNumber", ClerkArgs.Of(("phoneNumber", phoneNumber)));
    public Task PreparePhoneVerificationAsync(string id) => Call($"user.phoneNumbers[{id}].prepareVerification");
    public Task AttemptPhoneVerificationAsync(string id, string code) => Call($"user.phoneNumbers[{id}].attemptVerification", ClerkArgs.Of(("code", code)));
    public Task RemovePhoneNumberAsync(string id) => Call($"user.phoneNumbers[{id}].destroy");
    public Task SetPrimaryPhoneNumberAsync(string id) => Call("user.update", ClerkArgs.Of(("primaryPhoneNumberId", id)));
    public Task SetPhoneReservedForSecondFactorAsync(string id, bool reserved) => Call($"user.phoneNumbers[{id}].setReservedForSecondFactor", ClerkArgs.Of(("reserved", reserved)));
    public Task MakeDefaultSecondFactorAsync(string id) => Call($"user.phoneNumbers[{id}].makeDefaultSecondFactor");

    // Connected accounts
    /// <summary>Connects an OAuth account and returns the provider URL to navigate to.</summary>
    public async Task<string?> ConnectExternalAccountAsync(string strategy, string redirectUrl, IEnumerable<string>? additionalScopes = null)
    {
        var res = await Call("user.createExternalAccount", ClerkArgs.Of(("strategy", strategy), ("redirectUrl", redirectUrl), ("additionalScopes", additionalScopes?.ToArray())));
        return res.As<ClerkExternalAccount>().Verification?.ExternalVerificationRedirectUrl;
    }

    public async Task<string?> ReauthorizeExternalAccountAsync(string id, string redirectUrl, IEnumerable<string>? additionalScopes = null)
    {
        var res = await Call($"user.externalAccounts[{id}].reauthorize", ClerkArgs.Of(("redirectUrl", redirectUrl), ("additionalScopes", additionalScopes?.ToArray())));
        return res.As<ClerkExternalAccount>().Verification?.ExternalVerificationRedirectUrl;
    }

    public Task RemoveExternalAccountAsync(string id) => Call($"user.externalAccounts[{id}].destroy");
    public Task RemoveWeb3WalletAsync(string id) => Call($"user.web3Wallets[{id}].destroy");

    // Password
    public Task UpdatePasswordAsync(string newPassword, string? currentPassword = null, bool signOutOfOtherSessions = true) =>
        Call("user.updatePassword", ClerkArgs.Of(("newPassword", newPassword), ("currentPassword", currentPassword), ("signOutOfOtherSessions", signOutOfOtherSessions)));

    public Task RemovePasswordAsync(string currentPassword) => Call("user.removePassword", ClerkArgs.Of(("currentPassword", currentPassword)));

    // MFA
    public Task<TotpSecret> CreateTotpAsync() => Call<TotpSecret>("user.createTOTP");
    public Task<TotpSecret> VerifyTotpAsync(string code) => Call<TotpSecret>("user.verifyTOTP", ClerkArgs.Of(("code", code)));
    public Task DisableTotpAsync() => Call("user.disableTOTP");

    public async Task<IReadOnlyList<string>> CreateBackupCodesAsync()
    {
        var res = await Call("user.createBackupCode");
        return res.Data.TryGetProperty("codes", out var codes) ? codes.Deserialize<List<string>>(ClerkJson.Options)! : [];
    }

    // Passkeys
    public Task<ClerkPasskey> CreatePasskeyAsync() => Call<ClerkPasskey>("user.createPasskey");
    public Task RenamePasskeyAsync(string id, string name) => Call($"user.passkeys[{id}].update", ClerkArgs.Of(("name", name)));
    public Task RemovePasskeyAsync(string id) => Call($"user.passkeys[{id}].delete");

    // Sessions / devices
    public async Task<IReadOnlyList<ClerkUserSession>> GetSessionsAsync() =>
        (await Call("user.getSessions")).Items().Select(i => i.As<ClerkUserSession>()).ToList();

    public Task RevokeSessionAsync(ClerkUserSession session) =>
        session.Handle is null ? Task.CompletedTask : Clerk.InvokeHandleAsync(session.Handle, "revoke");

    public Task DeleteAsync() => Call("user.delete");

    // Organization relationships
    public Task<ClerkPage<UserOrganizationInvitation>> GetOrganizationInvitationsAsync(int page = 1, int pageSize = 10, string status = "pending") =>
        Page<UserOrganizationInvitation>("user.getOrganizationInvitations", Paging(page, pageSize, ("status", status)));

    public Task<ClerkPage<OrganizationSuggestion>> GetOrganizationSuggestionsAsync(int page = 1, int pageSize = 10) =>
        Page<OrganizationSuggestion>("user.getOrganizationSuggestions", Paging(page, pageSize, ("status", new[] { "pending", "accepted" })));

    public Task<ClerkPage<ClerkOrganizationMembership>> GetOrganizationMembershipsAsync(int page = 1, int pageSize = 10) =>
        Page<ClerkOrganizationMembership>("user.getOrganizationMemberships", Paging(page, pageSize));

    public Task AcceptInvitationAsync(UserOrganizationInvitation invitation) => Clerk.InvokeHandleAsync(invitation.Handle!, "accept");

    public Task AcceptSuggestionAsync(OrganizationSuggestion suggestion) => Clerk.InvokeHandleAsync(suggestion.Handle!, "accept");
}

/// <summary>Organization operations (<c>useOrganization</c>, <c>useOrganizationList</c>, <c>useOrganizationCreationDefaults</c>).</summary>
public sealed class OrganizationApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task<ClerkOrganization> CreateAsync(string name, string? slug = null) =>
        Call<ClerkOrganization>("createOrganization", ClerkArgs.Of(("name", name), ("slug", string.IsNullOrWhiteSpace(slug) ? null : slug)));

    /// <summary>Activates an organization, or the personal account when <paramref name="organizationId"/> is null.</summary>
    public Task SetActiveAsync(string? organizationId, string? redirectUrl = null) =>
        Clerk.SetActiveAsync(new SetActiveOptions { OrganizationId = organizationId, PersonalAccount = organizationId is null, RedirectUrl = redirectUrl });

    public Task<OrganizationCreationDefaults> GetCreationDefaultsAsync() => Call<OrganizationCreationDefaults>("user.getOrganizationCreationDefaults");

    public Task UpdateAsync(string name, string? slug = null) => Call("organization.update", ClerkArgs.Of(("name", name), ("slug", slug)));

    public Task SetLogoAsync(ElementReference fileInput) => Clerk.CallBridgeAsync("uploadImage", "organization", fileInput);

    public Task RemoveLogoAsync() => Call("organization.setLogo", new Dictionary<string, object?> { ["file"] = null });

    public Task DestroyAsync() => Call("organization.destroy");

    public Task LeaveAsync(string organizationId) => Call("user.leaveOrganization", organizationId);

    public Task<ClerkPage<OrganizationMember>> GetMembershipsAsync(int page = 1, int pageSize = 10, string? query = null, string[]? roles = null) =>
        Page<OrganizationMember>("organization.getMemberships", Paging(page, pageSize, ("query", query), ("role", roles)));

    public async Task<IReadOnlyList<OrganizationRole>> GetRolesAsync()
    {
        var res = await Call("organization.getRoles", ClerkArgs.Of(("pageSize", 50)));
        return res.Items().Select(i => i.As<OrganizationRole>()).ToList();
    }

    public Task UpdateMemberAsync(string userId, string role) => Call("organization.updateMember", ClerkArgs.Of(("userId", userId), ("role", role)));

    public Task RemoveMemberAsync(string userId) => Call("organization.removeMember", userId);

    public Task<ClerkPage<OrganizationInvitation>> GetInvitationsAsync(int page = 1, int pageSize = 10, string status = "pending") =>
        Page<OrganizationInvitation>("organization.getInvitations", Paging(page, pageSize, ("status", new[] { status })));

    public Task InviteMembersAsync(IEnumerable<string> emailAddresses, string role) =>
        Call("organization.inviteMembers", ClerkArgs.Of(("emailAddresses", emailAddresses.ToArray()), ("role", role)));

    public Task RevokeInvitationAsync(OrganizationInvitation invitation) => Clerk.InvokeHandleAsync(invitation.Handle!, "revoke");

    public Task<ClerkPage<OrganizationMembershipRequest>> GetMembershipRequestsAsync(int page = 1, int pageSize = 10, string status = "pending") =>
        Page<OrganizationMembershipRequest>("organization.getMembershipRequests", Paging(page, pageSize, ("status", status)));

    public Task AcceptMembershipRequestAsync(OrganizationMembershipRequest request) => Clerk.InvokeHandleAsync(request.Handle!, "accept");

    public Task RejectMembershipRequestAsync(OrganizationMembershipRequest request) => Clerk.InvokeHandleAsync(request.Handle!, "reject");

    public Task<ClerkPage<OrganizationDomain>> GetDomainsAsync(int page = 1, int pageSize = 10) =>
        Page<OrganizationDomain>("organization.getDomains", Paging(page, pageSize));

    public Task<OrganizationDomain> CreateDomainAsync(string name) => Call<OrganizationDomain>("organization.createDomain", name);

    public Task PrepareDomainVerificationAsync(OrganizationDomain domain, string affiliationEmailAddress) =>
        Clerk.InvokeHandleAsync(domain.Handle!, "prepareAffiliationVerification", ClerkArgs.Of(("affiliationEmailAddress", affiliationEmailAddress)));

    public Task AttemptDomainVerificationAsync(OrganizationDomain domain, string code) =>
        Clerk.InvokeHandleAsync(domain.Handle!, "attemptAffiliationVerification", ClerkArgs.Of(("code", code)));

    public Task UpdateDomainEnrollmentModeAsync(OrganizationDomain domain, string enrollmentMode, bool deletePending = false) =>
        Clerk.InvokeHandleAsync(domain.Handle!, "updateEnrollmentMode", ClerkArgs.Of(("enrollmentMode", enrollmentMode), ("deletePending", deletePending)));

    public Task DeleteDomainAsync(OrganizationDomain domain) => Clerk.InvokeHandleAsync(domain.Handle!, "delete");
}

/// <summary>Billing operations (<c>usePlans</c>, <c>useSubscription</c>, <c>useCheckout</c>, <c>usePaymentMethods</c>, <c>usePaymentAttempts</c>, <c>useStatements</c>).</summary>
public sealed class BillingApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    /// <summary>Lists plans for <c>user</c> or <c>organization</c> payers.</summary>
    public Task<ClerkPage<BillingPlan>> GetPlansAsync(string payerType = "user", int page = 1, int pageSize = 25) =>
        Page<BillingPlan>("billing.getPlans", Paging(page, pageSize, ("for", payerType)));

    public Task<BillingPlan> GetPlanAsync(string id) => Call<BillingPlan>("billing.getPlan", ClerkArgs.Of(("id", id)));

    public async Task<BillingSubscription?> GetSubscriptionAsync(string? orgId = null)
    {
        try { return await Call<BillingSubscription>("billing.getSubscription", ClerkArgs.Of(("orgId", orgId))); }
        catch (ClerkException e) when (e.Status == 404) { return null; }
    }

    public Task<ClerkPage<BillingStatement>> GetStatementsAsync(string? orgId = null, int page = 1, int pageSize = 10) =>
        Page<BillingStatement>("billing.getStatements", Paging(page, pageSize, ("orgId", orgId)));

    public Task<ClerkPage<BillingPayment>> GetPaymentAttemptsAsync(string? orgId = null, int page = 1, int pageSize = 10) =>
        Page<BillingPayment>("billing.getPaymentAttempts", Paging(page, pageSize, ("orgId", orgId)));

    public Task<ClerkPage<BillingPaymentMethod>> GetPaymentMethodsAsync(bool forOrganization = false, int page = 1, int pageSize = 10) =>
        Page<BillingPaymentMethod>(forOrganization ? "organization.getPaymentMethods" : "user.getPaymentMethods", Paging(page, pageSize));

    public Task AddPaymentMethodAsync(string paymentToken, bool forOrganization = false) =>
        Call(forOrganization ? "organization.addPaymentMethod" : "user.addPaymentMethod", ClerkArgs.Of(("gateway", "stripe"), ("paymentToken", paymentToken)));

    public Task RemovePaymentMethodAsync(BillingPaymentMethod method, string? orgId = null) =>
        Clerk.InvokeHandleAsync(method.Handle!, "remove", ClerkArgs.Of(("orgId", orgId)));

    public Task MakeDefaultPaymentMethodAsync(BillingPaymentMethod method, string? orgId = null) =>
        Clerk.InvokeHandleAsync(method.Handle!, "makeDefault", ClerkArgs.Of(("orgId", orgId)));

    /// <summary>Starts a checkout for a plan (<c>useCheckout().start</c>).</summary>
    public Task<BillingCheckout> StartCheckoutAsync(string planId, string planPeriod = "month", string? orgId = null) =>
        Call<BillingCheckout>("billing.startCheckout", ClerkArgs.Of(("planId", planId), ("planPeriod", planPeriod), ("orgId", orgId)));

    /// <summary>Confirms a checkout with a saved payment method or a new payment token.</summary>
    public async Task<BillingCheckout> ConfirmCheckoutAsync(BillingCheckout checkout, string? paymentMethodId = null, string? paymentToken = null)
    {
        var args = paymentToken is not null
            ? ClerkArgs.Of(("gateway", "stripe"), ("paymentToken", paymentToken))
            : ClerkArgs.Of(("paymentMethodId", paymentMethodId));
        return (await Clerk.InvokeHandleAsync(checkout.Handle!, "confirm", args)).As<BillingCheckout>();
    }

    public Task CancelSubscriptionItemAsync(BillingSubscriptionItem item, string? orgId = null) =>
        Clerk.InvokeHandleAsync(item.Handle!, "cancel", ClerkArgs.Of(("orgId", orgId)));
}

/// <summary>API key operations (<c>useAPIKeys</c>).</summary>
public sealed class ApiKeysApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task<ClerkPage<ClerkApiKey>> GetAllAsync(string? subject = null, int page = 1, int pageSize = 10, string? query = null) =>
        Page<ClerkApiKey>("apiKeys.getAll", Paging(page, pageSize, ("subject", subject), ("query", query)));

    public Task<ClerkApiKey> CreateAsync(string name, string? description = null, int? secondsUntilExpiration = null, string? subject = null) =>
        Call<ClerkApiKey>("apiKeys.create", ClerkArgs.Of(("name", name), ("description", description), ("secondsUntilExpiration", secondsUntilExpiration), ("subject", subject)));

    public Task<ClerkApiKey> RevokeAsync(string apiKeyId, string? reason = null) =>
        Call<ClerkApiKey>("apiKeys.revoke", ClerkArgs.Of(("apiKeyID", apiKeyId), ("revocationReason", reason)));
}

/// <summary>Waitlist operations (<c>useWaitlist</c>).</summary>
public sealed class WaitlistApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task<ClerkResource> JoinAsync(string emailAddress) => Call("joinWaitlist", ClerkArgs.Of(("emailAddress", emailAddress)));
}

/// <summary>OAuth consent operations (<c>useOAuthConsent</c>).</summary>
public sealed class OAuthApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task<OAuthConsentInfo> GetConsentInfoAsync(string clientId, string? scope = null, string? redirectUri = null) =>
        Call<OAuthConsentInfo>("oauthApplication.getConsentInfo", ClerkArgs.Of(("oauthClientId", clientId), ("scope", scope), ("redirectUri", redirectUri)));

    public async Task<string> BuildConsentActionUrlAsync(string clientId) =>
        (await Call("oauthApplication.buildConsentActionUrl", ClerkArgs.Of(("clientId", clientId)))).Data.GetString()!;
}

/// <summary>Session operations, including step-up reverification (<c>useSession</c>, <c>useReverification</c>).</summary>
public sealed class SessionApi(ClerkService clerk) : ClerkApiBase(clerk)
{
    public Task SwitchAsync(string sessionId, string? redirectUrl = null) => Clerk.SetActiveAsync(new SetActiveOptions { SessionId = sessionId, RedirectUrl = redirectUrl });

    public Task TouchAsync() => Call("session.touch");

    public Task<SessionVerification> StartVerificationAsync(ReverificationLevel level) =>
        Call<SessionVerification>("session.startVerification", ClerkArgs.Of(("level", level switch
        {
            ReverificationLevel.FirstFactor => "first_factor",
            ReverificationLevel.SecondFactor => "second_factor",
            _ => "multi_factor",
        })));

    public Task<SessionVerification> PrepareFirstFactorVerificationAsync(SignInFactor factor) =>
        Call<SessionVerification>("session.prepareFirstFactorVerification", ClerkArgs.Of(("strategy", factor.Strategy), ("emailAddressId", factor.EmailAddressId), ("phoneNumberId", factor.PhoneNumberId)));

    public Task<SessionVerification> AttemptFirstFactorVerificationAsync(string strategy, string? code = null, string? password = null) =>
        Call<SessionVerification>("session.attemptFirstFactorVerification", ClerkArgs.Of(("strategy", strategy), ("code", code), ("password", password)));

    public Task<SessionVerification> PrepareSecondFactorVerificationAsync(SignInFactor factor) =>
        Call<SessionVerification>("session.prepareSecondFactorVerification", ClerkArgs.Of(("strategy", factor.Strategy), ("phoneNumberId", factor.PhoneNumberId)));

    public Task<SessionVerification> AttemptSecondFactorVerificationAsync(string strategy, string code) =>
        Call<SessionVerification>("session.attemptSecondFactorVerification", ClerkArgs.Of(("strategy", strategy), ("code", code)));
}
