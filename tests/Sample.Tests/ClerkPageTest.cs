using Microsoft.Playwright;

namespace Sample.Tests;

/// <summary>
/// Base class for browser tests. Installs an in-memory fake of the ClerkJS <c>window.Clerk</c>
/// global before the app boots, so the real <c>clerk-blazor.js</c> bridge and Razor components
/// run end-to-end without contacting Clerk. Tests drive the fake through <c>window.__fakeClerk</c>.
/// </summary>
public abstract class ClerkPageTest : PageTest
{
    protected readonly List<string> ConsoleErrors = [];
    protected readonly List<string> Requests = [];

    private const string FakeClerkScript = """
        (() => {
          const listeners = new Set();
          const now = new Date();
          const email = { id: 'idn_1', emailAddress: 'ada@example.com', verification: { status: 'verified', strategy: 'email_code' }, linkedTo: [] };
          const user = {
            id: 'user_1', firstName: 'Ada', lastName: 'Lovelace', fullName: 'Ada Lovelace', username: 'ada',
            imageUrl: '', hasImage: false, primaryEmailAddressId: 'idn_1', primaryEmailAddress: email,
            emailAddresses: [email], phoneNumbers: [], web3Wallets: [], externalAccounts: [], enterpriseAccounts: [],
            passkeys: [], organizationMemberships: [], passwordEnabled: true, createdAt: now, updatedAt: now,
          };
          const session = { id: 'sess_1', status: 'active', user, factorVerificationAge: [0, -1],
                            lastActiveToken: { jwt: { claims: { sub: 'user_1' } } },
                            getToken: async () => 'fake.jwt.token' };
          const attr = (enabled, extra = {}) => ({ enabled, required: enabled, used_for_first_factor: enabled, first_factors: [], ...extra });
          const environment = {
            displayConfig: { applicationName: 'Sample', preferredSignInStrategy: 'password', instanceEnvironmentType: 'development' },
            userSettings: {
              attributes: { email_address: attr(true, { first_factors: ['email_code'], verifications: ['email_code'] }), password: attr(true) },
              social: {}, signUp: { mode: 'public' }, passwordSettings: { min_length: 8, max_length: 72 }, actions: {},
            },
            organizationSettings: { enabled: false }, authConfig: { singleSessionMode: true },
          };

          class FakeClerk {
            constructor(key) { this.publishableKey = key; this.loaded = false; this.version = '6.42.0-fake'; this.environment = environment; this.calls = []; window.__fakeClerk = this; }
            get session() { return this.client.sessions[0] ?? null; }
            get user() { return this.session?.user ?? null; }
            get organization() { return null; }
            async load(options) {
              this.loadOptions = options;
              this.client = { id: 'client_1', sessions: window.__fakeClerkSignedIn ? [session] : [] };
              this.loaded = true;
              this.status = 'ready';
            }
            addListener(fn) { listeners.add(fn); return () => listeners.delete(fn); }
            emit() { listeners.forEach((fn) => fn({ client: this.client, session: this.session, user: this.user })); }
            signIn() { this.client = { ...this.client, sessions: [session] }; this.emit(); }
            async signOut() { this.calls.push('signOut'); this.client = { ...this.client, sessions: [] }; this.emit(); }
            async setActive() { this.emit(); }
            mountSignIn() { this.calls.push('mountSignIn'); }
            openSignIn() { this.calls.push('openSignIn'); }
          }
          window.Clerk = FakeClerk;
        })();
        """;

    [SetUp]
    public async Task InstallFakeClerkAsync()
    {
        ConsoleErrors.Clear();
        Requests.Clear();
        Page.Console += (_, msg) => { if (msg.Type == "error") ConsoleErrors.Add($"[error] {msg.Text}"); };
        Page.PageError += (_, err) => ConsoleErrors.Add($"[uncaught] {err}");
        Page.Request += (_, req) => Requests.Add(req.Url);

        await Page.AddInitScriptAsync(FakeClerkScript);
    }

    protected Task GotoAsync(string path = "") => Page.GotoAsync($"{BlazorDevServer.BaseUrl}/{path.TrimStart('/')}");

    protected void AssertNoConsoleErrors() =>
        Assert.That(ConsoleErrors, Is.Empty, "Unexpected JavaScript errors:\n" + string.Join("\n", ConsoleErrors));
}
