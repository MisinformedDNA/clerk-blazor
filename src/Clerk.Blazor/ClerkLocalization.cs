namespace Clerk.Blazor;

/// <summary>
/// UI strings used by the built-in components. Override any property to localize
/// (equivalent to Clerk React's <c>localization</c> prop).
/// </summary>
public partial class ClerkLocalization
{
    // Shared
    public string Continue { get; set; } = "Continue";
    public string Cancel { get; set; } = "Cancel";
    public string Save { get; set; } = "Save";
    public string Remove { get; set; } = "Remove";
    public string Back { get; set; } = "Back";
    public string Close { get; set; } = "Close";
    public string Loading { get; set; } = "Loading…";
    public string Or { get; set; } = "or";
    public string Resend { get; set; } = "Resend code";
    public string Verify { get; set; } = "Verify";
    public string Copy { get; set; } = "Copy";
    public string Copied { get; set; } = "Copied";
    public string Previous { get; set; } = "Previous";
    public string Next { get; set; } = "Next";
    public string Search { get; set; } = "Search";
    public string NoResults { get; set; } = "Nothing here yet.";
    public string SomethingWentWrong { get; set; } = "Something went wrong. Try again.";
    public string Offline { get; set; } = "You appear to be offline. Check your connection and try again.";
    public string ClerkFailed { get; set; } = "Authentication is unavailable right now.";
    public string DevelopmentMode { get; set; } = "Development mode";

    // Fields
    public string EmailAddress { get; set; } = "Email address";
    public string PhoneNumber { get; set; } = "Phone number";
    public string Username { get; set; } = "Username";
    public string EmailOrUsername { get; set; } = "Email address or username";
    public string Identifier { get; set; } = "Email, phone or username";
    public string Password { get; set; } = "Password";
    public string NewPassword { get; set; } = "New password";
    public string CurrentPassword { get; set; } = "Current password";
    public string ConfirmPassword { get; set; } = "Confirm password";
    public string FirstName { get; set; } = "First name";
    public string LastName { get; set; } = "Last name";
    public string Code { get; set; } = "Verification code";
    public string Optional { get; set; } = "Optional";
    public string ShowPassword { get; set; } = "Show password";
    public string HidePassword { get; set; } = "Hide password";
    public string PasswordsDoNotMatch { get; set; } = "Passwords don't match.";

    // Sign in
    public string SignInTitle { get; set; } = "Sign in";
    public string SignInSubtitle { get; set; } = "Welcome back. Sign in to continue to {0}.";
    public string SignInButton { get; set; } = "Sign in";
    public string ContinueWith { get; set; } = "Continue with {0}";
    public string NoAccount { get; set; } = "Don't have an account?";
    public string SignUpLink { get; set; } = "Sign up";
    public string ForgotPassword { get; set; } = "Forgot password?";
    public string UseAnotherMethod { get; set; } = "Use another method";
    public string UsePasskey { get; set; } = "Sign in with a passkey";
    public string CheckEmail { get; set; } = "Check your email";
    public string CheckPhone { get; set; } = "Check your phone";
    public string CodeSentTo { get; set; } = "Enter the code sent to {0}.";
    public string EmailLinkSentTo { get; set; } = "We sent a sign-in link to {0}. Open it to continue; this tab updates automatically.";
    public string EnterPassword { get; set; } = "Enter your password";
    public string TwoStepTitle { get; set; } = "Two-step verification";
    public string TotpPrompt { get; set; } = "Enter the code from your authenticator app.";
    public string BackupCodePrompt { get; set; } = "Enter one of your backup codes.";
    public string UseBackupCode { get; set; } = "Use a backup code";
    public string UseAuthenticator { get; set; } = "Use your authenticator app";
    public string UseSmsCode { get; set; } = "Send a code by SMS";
    public string ResetPasswordTitle { get; set; } = "Set a new password";
    public string SignOutOtherSessions { get; set; } = "Sign out of all other devices";
    public string ClientTrustTitle { get; set; } = "Verify it's you";
    public string ClientTrustPrompt { get; set; } = "This is a new device. Enter the code we sent to confirm it's you.";
    public string ChooseAccount { get; set; } = "Choose an account";
    public string AddAccount { get; set; } = "Add account";

    // Sign up
    public string SignUpTitle { get; set; } = "Create your account";
    public string SignUpSubtitle { get; set; } = "Get started with {0}.";
    public string SignUpButton { get; set; } = "Create account";
    public string HaveAccount { get; set; } = "Already have an account?";
    public string SignInLink { get; set; } = "Sign in";
    public string LegalAccept { get; set; } = "I agree to the Terms of Service and Privacy Policy";
    public string MissingRequirements { get; set; } = "A few more details";
    public string RestrictedSignUp { get; set; } = "Sign-ups are currently by invitation only.";
    public string WaitlistSignUp { get; set; } = "Sign-ups are currently limited. Join the waitlist to get access.";

    // Waitlist
    public string WaitlistTitle { get; set; } = "Join the waitlist";
    public string WaitlistSubtitle { get; set; } = "Enter your email and we'll let you know when your spot is ready.";
    public string WaitlistButton { get; set; } = "Join the waitlist";
    public string WaitlistSuccess { get; set; } = "You're on the list. We'll email you when your spot opens.";

    // Session / user button
    public string SignOut { get; set; } = "Sign out";
    public string SignOutAll { get; set; } = "Sign out of all accounts";
    public string ManageAccount { get; set; } = "Manage account";
    public string OpenUserMenu { get; set; } = "Open user menu";

    // User profile
    public string AccountTitle { get; set; } = "Account";
    public string SecurityTitle { get; set; } = "Security";
    public string BillingTitle { get; set; } = "Billing";
    public string ApiKeysTitle { get; set; } = "API keys";
    public string ProfileSection { get; set; } = "Profile";
    public string UpdateProfile { get; set; } = "Update profile";
    public string UploadImage { get; set; } = "Upload image";
    public string RemoveImage { get; set; } = "Remove image";
    public string EmailAddresses { get; set; } = "Email addresses";
    public string AddEmail { get; set; } = "Add email address";
    public string PhoneNumbers { get; set; } = "Phone numbers";
    public string AddPhone { get; set; } = "Add phone number";
    public string ConnectedAccounts { get; set; } = "Connected accounts";
    public string Connect { get; set; } = "Connect";
    public string Reconnect { get; set; } = "Reconnect";
    public string Web3Wallets { get; set; } = "Web3 wallets";
    public string EnterpriseAccounts { get; set; } = "Enterprise accounts";
    public string Primary { get; set; } = "Primary";
    public string Unverified { get; set; } = "Unverified";
    public string MakePrimary { get; set; } = "Make primary";
    public string SetPassword { get; set; } = "Set password";
    public string UpdatePassword { get; set; } = "Update password";
    public string RemovePassword { get; set; } = "Remove password";
    public string Passkeys { get; set; } = "Passkeys";
    public string AddPasskey { get; set; } = "Add a passkey";
    public string Rename { get; set; } = "Rename";
    public string TwoStepVerification { get; set; } = "Two-step verification";
    public string AddAuthenticator { get; set; } = "Add authenticator app";
    public string ScanQr { get; set; } = "Scan this QR code with your authenticator app, or enter the secret manually.";
    public string BackupCodes { get; set; } = "Backup codes";
    public string RegenerateBackupCodes { get; set; } = "Regenerate backup codes";
    public string BackupCodesHint { get; set; } = "Store these somewhere safe. Each code works once.";
    public string ActiveDevices { get; set; } = "Active devices";
    public string ThisDevice { get; set; } = "This device";
    public string SignOutDevice { get; set; } = "Sign out of device";
    public string DeleteAccount { get; set; } = "Delete account";
    public string DeleteAccountWarning { get; set; } = "This permanently deletes your account and all associated data.";
    public string TypeToConfirm { get; set; } = "Type \"{0}\" to confirm";

    // Reverification
    public string ReverifyTitle { get; set; } = "Confirm it's you";
    public string ReverifySubtitle { get; set; } = "This action requires recent verification.";

    // Organizations
    public string PersonalAccount { get; set; } = "Personal account";
    public string CreateOrganization { get; set; } = "Create organization";
    public string OrganizationName { get; set; } = "Name";
    public string OrganizationSlug { get; set; } = "Slug";
    public string ManageOrganization { get; set; } = "Manage organization";
    public string SwitchOrganization { get; set; } = "Switch organization";
    public string OrganizationListTitle { get; set; } = "Choose an organization";
    public string Join { get; set; } = "Join";
    public string RequestToJoin { get; set; } = "Request to join";
    public string Requested { get; set; } = "Requested";
    public string Members { get; set; } = "Members";
    public string Invitations { get; set; } = "Invitations";
    public string Requests { get; set; } = "Requests";
    public string InviteMembers { get; set; } = "Invite members";
    public string InviteHint { get; set; } = "Separate email addresses with commas or spaces.";
    public string Role { get; set; } = "Role";
    public string SendInvitations { get; set; } = "Send invitations";
    public string Revoke { get; set; } = "Revoke";
    public string Accept { get; set; } = "Accept";
    public string Reject { get; set; } = "Reject";
    public string Joined { get; set; } = "Joined";
    public string General { get; set; } = "General";
    public string Domains { get; set; } = "Verified domains";
    public string AddDomain { get; set; } = "Add domain";
    public string LeaveOrganization { get; set; } = "Leave organization";
    public string DeleteOrganization { get; set; } = "Delete organization";
    public string DangerZone { get; set; } = "Danger zone";

    // Tasks
    public string ChooseOrganizationTask { get; set; } = "Choose an organization to continue";
    public string ResetPasswordTask { get; set; } = "Your password needs to be reset";
    public string SetupMfaTask { get; set; } = "Set up two-step verification to continue";

    // Billing
    public string Monthly { get; set; } = "Monthly";
    public string Annually { get; set; } = "Annually";
    public string PerMonth { get; set; } = "/month";
    public string BilledAnnually { get; set; } = "billed annually";
    public string Free { get; set; } = "Free";
    public string Subscribe { get; set; } = "Subscribe";
    public string SwitchPlan { get; set; } = "Switch to this plan";
    public string CurrentPlan { get; set; } = "Current plan";
    public string StartTrial { get; set; } = "Start {0}-day free trial";
    public string Checkout { get; set; } = "Checkout";
    public string PayNow { get; set; } = "Pay {0}";
    public string TotalDueToday { get; set; } = "Total due today";
    public string Subtotal { get; set; } = "Subtotal";
    public string PaymentMethods { get; set; } = "Payment methods";
    public string AddPaymentMethod { get; set; } = "Add payment method";
    public string UseNewPaymentMethod { get; set; } = "Use a new payment method";
    public string MakeDefault { get; set; } = "Make default";
    public string Default { get; set; } = "Default";
    public string CheckoutComplete { get; set; } = "Payment successful";
    public string Subscription { get; set; } = "Subscription";
    public string CancelSubscription { get; set; } = "Cancel subscription";
    public string Statements { get; set; } = "Statements";
    public string Payments { get; set; } = "Payments";
    public string RenewsOn { get; set; } = "Renews {0}";
    public string EndsOn { get; set; } = "Ends {0}";
    public string Features { get; set; } = "Features";

    // API keys
    public string AddApiKey { get; set; } = "Add new key";
    public string ApiKeyName { get; set; } = "Name";
    public string ApiKeyDescription { get; set; } = "Description";
    public string Expiration { get; set; } = "Expiration";
    public string Never { get; set; } = "Never";
    public string LastUsed { get; set; } = "Last used";
    public string Created { get; set; } = "Created";
    public string ApiKeySecretHint { get; set; } = "Copy this key now. You won't be able to see it again.";
    public string RevokeKey { get; set; } = "Revoke key";

    // OAuth consent
    public string OAuthConsentTitle { get; set; } = "{0} wants to access your account";
    public string OAuthConsentHint { get; set; } = "This will allow {0} to:";
    public string Allow { get; set; } = "Allow";
    public string Deny { get; set; } = "Deny";
}
