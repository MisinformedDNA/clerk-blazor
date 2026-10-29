namespace Clerk.Blazor;

public partial class ClerkLocalization
{
    public string UserRefreshDevices { get; set; } = "Refresh devices";
    public string UserLinked { get; set; } = "Linked";
    public string UserConfirmRemove { get; set; } = "Confirm removal";
    public string UserDisableAuthenticator { get; set; } = "Remove authenticator app";
    public string UserAuthenticatorInstructions { get; set; } = "Enter this secret in your authenticator app, then enter its verification code.";
    public string UserAuthenticatorUri { get; set; } = "Authenticator setup link";
    public string UserSmsSetup { get; set; } = "Use SMS verification";
    public string UserDisableSms { get; set; } = "Remove SMS verification";
    public string UserVerifiedPhoneRequired { get; set; } = "Add and verify a phone number in your account to enable SMS verification.";
    public string UserDownloadCodes { get; set; } = "Download backup codes";
    public string UserLastActive { get; set; } = "Last active: {0}";
    public string UserDevice { get; set; } = "Device";
    public string UserPasskeyName { get; set; } = "Passkey name";
    public string UserSignInRequired { get; set; } = "Sign in to manage your account.";
    public string UserMfaUnavailable { get; set; } = "No verification methods are enabled. Contact your administrator.";
    public string UserClipboardFailed { get; set; } = "Unable to copy. Select and copy the text manually.";
    public string UserPasswordUpdated { get; set; } = "Your password has been updated.";
}
