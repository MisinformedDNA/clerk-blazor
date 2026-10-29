// Clerk.Blazor headless bridge.
//
// Loads the pinned, headless ClerkJS browser bundle and exposes a small, generic
// surface to .NET. ClerkJS only creates its (React-based) UI when `load()` is given
// a `ui.ClerkUI` constructor; this bridge never loads `@clerk/ui` and never passes
// that option, and it refuses every mount/open UI method. All UI is rendered by
// native Razor components.

export const CLERK_JS_VERSION = '6.42.0';

// Methods that would require Clerk's React UI, or lazily download a chunk that
// embeds React (the Base Account SDK used by Base / Coinbase wallets).
const FORBIDDEN_METHOD = /^(mount|unmount|open|close)[A-Z]|^__internal_(?!loadStripeJs$)|^authenticateWith(Base|CoinbaseWallet)$/;
const FORBIDDEN_STRATEGIES = new Set(['web3_base_signature', 'web3_coinbase_wallet_signature']);
const MAX_HANDLES = 1000;

let clerk = null;
let dotnet = null;
let options = {};
let seq = 0;
let disposers = [];
let loadPromise = null;
const handles = new Map();
let nextHandle = 1;

// ── Loading ──────────────────────────────────────────────────────────────────

export function frontendApiFromKey(publishableKey) {
    const match = /^pk_(test|live)_(.+)$/.exec(publishableKey || '');
    if (!match) throw clerkError('invalid_publishable_key', 'The Clerk publishable key is missing or malformed.');
    const standard = match[2].replace(/-/g, '+').replace(/_/g, '/');
    const padded = standard + '='.repeat((4 - (standard.length % 4)) % 4);
    return atob(padded).replace(/\$+$/, '');
}

export function scriptUrl(opts) {
    if (opts.clerkJSUrl) return opts.clerkJSUrl;
    const version = opts.clerkJSVersion || CLERK_JS_VERSION;
    const base = opts.proxyUrl ? opts.proxyUrl.replace(/\/$/, '') : 'https://' + frontendApiFromKey(opts.publishableKey);
    return `${base}/npm/@clerk/clerk-js@${version}/dist/clerk.browser.js`;
}

function injectScript(src, opts) {
    return new Promise((resolve, reject) => {
        const existing = document.querySelector('script[data-clerk-blazor]');
        if (existing && window.Clerk) { resolve(); return; }
        const script = existing || document.createElement('script');
        script.setAttribute('data-clerk-blazor', '');
        script.setAttribute('data-clerk-publishable-key', opts.publishableKey);
        if (opts.proxyUrl) script.setAttribute('data-clerk-proxy-url', opts.proxyUrl);
        if (opts.domain) script.setAttribute('data-clerk-domain', opts.domain);
        if (opts.nonce) script.nonce = opts.nonce;
        script.crossOrigin = 'anonymous';
        script.async = true;
        script.addEventListener('load', () => resolve(), { once: true });
        script.addEventListener('error', () => reject(clerkError('clerkjs_failed_to_load', `Failed to load ClerkJS from ${src}.`)), { once: true });
        if (!existing) { script.src = src; document.head.appendChild(script); }
    });
}

function injectStyles(href) {
    if (!href || document.querySelector('link[data-clerk-blazor-styles]')) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    link.setAttribute('data-clerk-blazor-styles', '');
    document.head.prepend(link);
}

/**
 * Loads ClerkJS headlessly and wires its listeners to the .NET ClerkService.
 * Idempotent: subsequent calls return the in-flight or completed load.
 */
export function load(opts, dotnetRef) {
    if (loadPromise) {
        dotnet = dotnetRef;
        return loadPromise.then(() => snapshot());
    }
    options = opts || {};
    dotnet = dotnetRef;
    loadPromise = (async () => {
        injectStyles(options.stylesheetUrl);
        if (!window.Clerk) await injectScript(scriptUrl(options), options);
        if (!window.Clerk) throw clerkError('clerkjs_not_initialized', 'ClerkJS did not initialize. Verify the publishable key.');
        clerk = typeof window.Clerk === 'function'
            ? new window.Clerk(options.publishableKey, { proxyUrl: options.proxyUrl, domain: options.domain })
            : window.Clerk;

        const loadOptions = {
            ...(options.loadOptions || {}),
            routerPush: (to) => navigate(to, false),
            routerReplace: (to) => navigate(to, true),
        };
        // Never allow the UI constructor through, whatever the caller supplied.
        delete loadOptions.ui;
        delete loadOptions.clerkUICtor;
        delete loadOptions.clerkUiCtor;
        await clerk.load(loadOptions);

        disposers.push(clerk.addListener(() => emit()));
        if (typeof clerk.on === 'function') {
            const onStatus = () => emit();
            clerk.on('status', onStatus);
            disposers.push(() => clerk.off?.('status', onStatus));
        }
        return snapshot();
    })();
    loadPromise.catch(() => { loadPromise = null; });
    return loadPromise;
}

export function dispose() {
    for (const d of disposers) { try { typeof d === 'function' && d(); } catch { /* ignore */ } }
    disposers = [];
    handles.clear();
    dotnet = null;
    loadPromise = null;
}

async function navigate(to, replace) {
    if (!dotnet) { replace ? location.replace(to) : location.assign(to); return; }
    const href = typeof to === 'string' ? to : String(to);
    await dotnet.invokeMethodAsync('Navigate', href, replace);
}

function emit() {
    if (!dotnet || !clerk) return;
    const json = JSON.stringify(snapshot());
    dotnet.invokeMethodAsync('OnStateChanged', json).catch((e) => console.error('[Clerk.Blazor] state dispatch failed', e));
}

// ── Snapshots ────────────────────────────────────────────────────────────────

const iso = (d) => (d instanceof Date && !isNaN(d) ? d.toISOString() : d ?? null);

function verification(v) {
    if (!v) return null;
    return {
        status: v.status ?? null,
        strategy: v.strategy ?? null,
        attempts: v.attempts ?? null,
        expireAt: iso(v.expireAt),
        externalVerificationRedirectUrl: v.externalVerificationRedirectURL?.href ?? null,
        error: v.error ? plainError(v.error) : null,
        nonce: v.nonce ?? null,
    };
}

function orgSnap(o) {
    if (!o) return null;
    return {
        id: o.id, name: o.name, slug: o.slug ?? null, imageUrl: o.imageUrl ?? null, hasImage: !!o.hasImage,
        membersCount: o.membersCount ?? 0, pendingInvitationsCount: o.pendingInvitationsCount ?? 0,
        adminDeleteEnabled: !!o.adminDeleteEnabled, maxAllowedMemberships: o.maxAllowedMemberships ?? 0,
        publicMetadata: o.publicMetadata ?? {}, createdAt: iso(o.createdAt), updatedAt: iso(o.updatedAt),
    };
}

function userSnap(u) {
    if (!u) return null;
    return {
        id: u.id, externalId: u.externalId ?? null, username: u.username ?? null,
        firstName: u.firstName ?? null, lastName: u.lastName ?? null, fullName: u.fullName ?? null,
        imageUrl: u.imageUrl ?? null, hasImage: !!u.hasImage,
        primaryEmailAddressId: u.primaryEmailAddressId ?? null,
        primaryPhoneNumberId: u.primaryPhoneNumberId ?? null,
        primaryWeb3WalletId: u.primaryWeb3WalletId ?? null,
        emailAddresses: (u.emailAddresses || []).map((e) => ({
            id: e.id, emailAddress: e.emailAddress, verification: verification(e.verification),
            linkedTo: (e.linkedTo || []).map((l) => ({ id: l.id, type: l.type })),
        })),
        phoneNumbers: (u.phoneNumbers || []).map((p) => ({
            id: p.id, phoneNumber: p.phoneNumber, verification: verification(p.verification),
            reservedForSecondFactor: !!p.reservedForSecondFactor, defaultSecondFactor: !!p.defaultSecondFactor,
        })),
        web3Wallets: (u.web3Wallets || []).map((w) => ({ id: w.id, web3Wallet: w.web3Wallet, verification: verification(w.verification) })),
        externalAccounts: (u.externalAccounts || []).map((x) => ({
            id: x.id, provider: x.provider, providerUserId: x.providerUserId ?? null, emailAddress: x.emailAddress ?? null,
            username: x.username ?? null, firstName: x.firstName ?? null, lastName: x.lastName ?? null,
            imageUrl: x.imageUrl ?? null, approvedScopes: x.approvedScopes ?? '', verification: verification(x.verification),
        })),
        enterpriseAccounts: (u.enterpriseAccounts || []).map((x) => ({
            id: x.id, provider: x.provider ?? null, emailAddress: x.emailAddress ?? null, active: !!x.active,
            name: x.enterpriseConnection?.name ?? null,
        })),
        passkeys: (u.passkeys || []).map((p) => ({ id: p.id, name: p.name ?? null, createdAt: iso(p.createdAt), lastUsedAt: iso(p.lastUsedAt) })),
        organizationMemberships: (u.organizationMemberships || []).map(membershipSnap),
        passwordEnabled: !!u.passwordEnabled, totpEnabled: !!u.totpEnabled, backupCodeEnabled: !!u.backupCodeEnabled,
        twoFactorEnabled: !!u.twoFactorEnabled, deleteSelfEnabled: !!u.deleteSelfEnabled,
        createOrganizationEnabled: !!u.createOrganizationEnabled, createOrganizationsLimit: u.createOrganizationsLimit ?? null,
        publicMetadata: u.publicMetadata ?? {}, unsafeMetadata: u.unsafeMetadata ?? {},
        lastSignInAt: iso(u.lastSignInAt), createdAt: iso(u.createdAt), updatedAt: iso(u.updatedAt),
    };
}

function membershipSnap(m) {
    return {
        id: m.id, role: m.role, roleName: m.roleName ?? null, permissions: m.permissions ?? [],
        organization: orgSnap(m.organization), createdAt: iso(m.createdAt),
        publicUserData: m.publicUserData ? {
            userId: m.publicUserData.userId ?? null, identifier: m.publicUserData.identifier ?? null,
            firstName: m.publicUserData.firstName ?? null, lastName: m.publicUserData.lastName ?? null,
            imageUrl: m.publicUserData.imageUrl ?? null, hasImage: !!m.publicUserData.hasImage,
        } : null,
    };
}

function sessionSnap(s) {
    if (!s) return null;
    const u = s.user;
    return {
        id: s.id, status: s.status, expireAt: iso(s.expireAt), abandonAt: iso(s.abandonAt), lastActiveAt: iso(s.lastActiveAt),
        lastActiveOrganizationId: s.lastActiveOrganizationId ?? null, actor: s.actor ?? null,
        factorVerificationAge: s.factorVerificationAge ?? null,
        currentTask: s.currentTask ? { key: s.currentTask.key } : null,
        claims: s.lastActiveToken?.jwt?.claims ?? null,
        user: u ? {
            id: u.id, fullName: u.fullName ?? null, imageUrl: u.imageUrl ?? null, hasImage: !!u.hasImage,
            identifier: u.primaryEmailAddress?.emailAddress ?? u.primaryPhoneNumber?.phoneNumber ?? u.username ?? null,
        } : null,
    };
}

function attribute(a) {
    a = a || {};
    return {
        enabled: !!a.enabled, required: !!a.required,
        usedForFirstFactor: !!(a.used_for_first_factor ?? a.usedForFirstFactor),
        firstFactors: a.first_factors ?? a.firstFactors ?? [],
        usedForSecondFactor: !!(a.used_for_second_factor ?? a.usedForSecondFactor),
        secondFactors: a.second_factors ?? a.secondFactors ?? [],
        verifications: a.verifications ?? [],
    };
}

function environmentSnap(env) {
    if (!env) return null;
    const us = env.userSettings || {};
    const attrs = us.attributes || {};
    const ds = env.displayConfig || {};
    const os = env.organizationSettings || {};
    const billing = env.commerceSettings?.billing || {};
    const keys = env.apiKeysSettings || {};
    const social = Object.values(us.social || {})
        .filter((p) => p && p.enabled && (p.authenticatable ?? true) && !(p.not_selectable ?? p.notSelectable))
        .map((p) => ({ strategy: p.strategy, name: p.name ?? p.strategy?.replace(/^oauth_(custom_)?/, ''), logoUrl: p.logo_url ?? p.logoUrl ?? null }));
    const attributeNames = ['email_address', 'phone_number', 'username', 'first_name', 'last_name', 'password', 'web3_wallet', 'passkey', 'authenticator_app', 'backup_code', 'legal_accepted'];
    return {
        applicationName: ds.applicationName ?? '', logoImageUrl: ds.logoImageUrl ?? null, faviconImageUrl: ds.faviconImageUrl ?? null,
        homeUrl: ds.homeUrl ?? null, signInUrl: ds.signInUrl ?? null, signUpUrl: ds.signUpUrl ?? null,
        userProfileUrl: ds.userProfileUrl ?? null, waitlistUrl: ds.waitlistUrl ?? null, branded: !!ds.branded,
        privacyPolicyUrl: ds.privacyPolicyUrl || null, termsUrl: ds.termsUrl || null, supportEmail: ds.supportEmail || null,
        googleOneTapClientId: ds.googleOneTapClientId ?? null, preferredSignInStrategy: ds.preferredSignInStrategy ?? 'password',
        instanceEnvironmentType: ds.instanceEnvironmentType ?? null,
        signUpMode: us.signUp?.mode ?? 'public', legalConsentEnabled: !!us.signUp?.legal_consent_enabled,
        attributes: Object.fromEntries(attributeNames.map((n) => [n, attribute(attrs[n])])),
        socialProviders: social,
        web3Strategies: (us.web3FirstFactors || []).filter((s) => !FORBIDDEN_STRATEGIES.has(s)),
        enterpriseSsoEnabled: !!us.enterpriseSSO?.enabled,
        singleSessionMode: env.authConfig?.singleSessionMode ?? true,
        passwordMinLength: us.passwordSettings?.min_length ?? 8, passwordMaxLength: us.passwordSettings?.max_length ?? 72,
        deleteSelfEnabled: !!us.actions?.delete_self, createOrganizationEnabled: !!us.actions?.create_organization,
        organizations: {
            enabled: !!os.enabled, maxAllowedMemberships: os.maxAllowedMemberships ?? 0,
            forceOrganizationSelection: !!os.forceOrganizationSelection, adminDeleteEnabled: !!os.actions?.adminDelete,
            domainsEnabled: !!os.domains?.enabled, domainEnrollmentModes: os.domains?.enrollmentModes ?? [],
            slugDisabled: !!os.slug?.disabled, creationDefaultsEnabled: !!os.organizationCreationDefaults?.enabled,
        },
        billing: {
            userEnabled: !!billing.user?.enabled, userHasPaidPlans: !!billing.user?.hasPaidPlans,
            organizationEnabled: !!billing.organization?.enabled, organizationHasPaidPlans: !!billing.organization?.hasPaidPlans,
            stripePublishableKey: billing.stripePublishableKey ?? null,
        },
        apiKeys: {
            userEnabled: !!(keys.user_api_keys_enabled ?? keys.userApiKeysEnabled),
            organizationEnabled: !!(keys.orgs_api_keys_enabled ?? keys.orgsApiKeysEnabled),
        },
    };
}

export function snapshot() {
    if (!clerk) return { seq: ++seq, status: 'loading', loaded: false };
    const s = clerk.session;
    const org = clerk.organization;
    const membership = org && clerk.user ? (clerk.user.organizationMemberships || []).find((m) => m.organization?.id === org.id) : null;
    let environment = null;
    try { environment = environmentSnap(clerk.environment ?? clerk.__internal_environment); } catch { /* not yet available */ }
    return {
        seq: ++seq,
        status: clerk.status ?? (clerk.loaded ? 'ready' : 'loading'),
        loaded: !!clerk.loaded,
        version: clerk.version ?? CLERK_JS_VERSION,
        client: clerk.client ? {
            id: clerk.client.id ?? null,
            sessions: (clerk.client.sessions || []).map(sessionSnap),
            lastActiveSessionId: clerk.client.lastActiveSessionId ?? null,
            signInStatus: clerk.client.signIn?.status ?? null,
            signUpStatus: clerk.client.signUp?.status ?? null,
        } : null,
        session: sessionSnap(s),
        user: userSnap(clerk.user),
        organization: orgSnap(org),
        membership: membership ? membershipSnap(membership) : null,
        environment,
    };
}

// ── Generic invocation ──────────────────────────────────────────────────────

function clerkError(code, message, extra) {
    const e = new Error(message);
    e.clerkBlazor = { code, message, ...extra };
    return e;
}

function plainError(e) {
    if (!e) return null;
    return {
        code: e.code ?? null, message: e.message ?? null, longMessage: e.longMessage ?? e.long_message ?? null,
        paramName: e.meta?.paramName ?? e.meta?.param_name ?? null,
    };
}

function errorPayload(err) {
    if (err?.clerkBlazor) return { error: err.clerkBlazor };
    const errors = Array.isArray(err?.errors) ? err.errors.map(plainError) : [];
    const first = errors[0];
    return {
        error: {
            code: first?.code ?? err?.code ?? err?.name ?? 'unknown_error',
            message: first?.longMessage ?? first?.message ?? err?.message ?? String(err),
            status: err?.status ?? null,
            clerkTraceId: err?.clerkTraceId ?? null,
            errors,
            isReverification: !!(first?.code === 'session_reverification_required' || err?.clerkError?.type === 'forbidden_reverification' || err?.clerk_error?.type === 'forbidden_reverification'),
        },
    };
}

function guardArgs(args) {
    for (const a of args) {
        if (a && typeof a === 'object' && FORBIDDEN_STRATEGIES.has(a.strategy)) {
            throw clerkError('forbidden_strategy', `The '${a.strategy}' strategy downloads a React-based wallet SDK and is not supported by Clerk.Blazor.`);
        }
    }
}

function resolveTarget(root, path) {
    const parts = path.split('.');
    const name = parts.pop();
    if (FORBIDDEN_METHOD.test(name)) throw clerkError('forbidden_method', `'${name}' requires Clerk's React UI and is not available in Clerk.Blazor.`);
    let target = root;
    for (const p of parts) {
        if (p.startsWith('__') || target == null) throw clerkError('invalid_path', `Cannot resolve '${path}'.`);
        // `collection[id]` selects the item with that id, e.g. `user.emailAddresses[idn_123]`.
        const m = /^([A-Za-z0-9_]+)\[(.+)\]$/.exec(p);
        target = m ? (target[m[1]] || []).find((x) => x?.id === m[2]) : target[p];
    }
    if (target == null) throw clerkError('invalid_path', `Cannot resolve '${path}'.`);
    return { target, name };
}

function isResource(v) {
    return v && typeof v === 'object' && !Array.isArray(v) && !(v instanceof Date) && Object.getPrototypeOf(v) !== Object.prototype;
}

function register(obj) {
    const id = 'h' + nextHandle++;
    handles.set(id, obj);
    if (handles.size > MAX_HANDLES) handles.delete(handles.keys().next().value);
    return id;
}

/** Converts a ClerkJS value into JSON-safe data; resources carry a `$handle`. */
function toPlain(v, depth = 0, seen = new WeakSet(), withHandle = true) {
    if (v == null || typeof v !== 'object') return typeof v === 'function' || typeof v === 'symbol' ? undefined : v;
    if (v instanceof Date) return iso(v);
    if (v instanceof URL) return v.href;
    if (seen.has(v) || depth > 6) return undefined;
    seen.add(v);
    if (Array.isArray(v)) return v.map((x) => toPlain(x, depth + 1, seen, withHandle));
    const out = {};
    if (isResource(v) && withHandle && depth <= 2) out.$handle = register(v);
    const keys = new Set(Object.keys(v));
    for (let p = Object.getPrototypeOf(v); p && p !== Object.prototype; p = Object.getPrototypeOf(p)) {
        for (const [k, d] of Object.entries(Object.getOwnPropertyDescriptors(p))) if (d.get) keys.add(k);
    }
    for (const k of keys) {
        if (k.startsWith('_') || k === 'pathRoot' || k === 'internalClerk') continue;
        let val;
        try { val = v[k]; } catch { continue; }
        const plain = toPlain(val, depth + 1, seen, withHandle);
        if (plain !== undefined) out[k] = plain;
    }
    return out;
}

async function call(root, path, args) {
    try {
        args = args || [];
        guardArgs(args);
        const { target, name } = resolveTarget(root, path);
        const member = target[name];
        const result = typeof member === 'function' ? await member.apply(target, args) : member;
        return { value: toPlain(result) ?? null };
    } catch (err) {
        return errorPayload(err);
    }
}

function requireClerk() {
    if (!clerk) throw clerkError('clerk_not_loaded', 'Clerk has not finished loading.');
    return clerk;
}

/** Invokes `path` (e.g. "client.signIn.create") on the Clerk instance. */
export async function invoke(path, args) {
    try { requireClerk(); } catch (e) { return errorPayload(e); }
    return call(clerk, path, args);
}

/** Invokes `path` on a resource previously returned to .NET. */
export async function invokeHandle(handle, path, args) {
    const obj = handles.get(handle);
    if (!obj) return errorPayload(clerkError('stale_handle', 'This Clerk resource is no longer available. Reload it and try again.'));
    return call(obj, path, args);
}

export function release(handle) { handles.delete(handle); }

// ── Specialised helpers (things that cannot cross the interop boundary) ─────

export async function getToken(tokenOptions) {
    try {
        requireClerk();
        if (!clerk.session) return { value: null };
        return { value: await clerk.session.getToken(tokenOptions || undefined) };
    } catch (err) { return errorPayload(err); }
}

export async function setActive(params) {
    try {
        requireClerk();
        const { redirectUrl, ...rest } = params || {};
        if (redirectUrl) {
            rest.navigate = ({ session } = {}) => {
                const task = session?.currentTask?.key;
                const taskUrl = task ? options.loadOptions?.taskUrls?.[task] : null;
                return navigate(taskUrl || redirectUrl, false);
            };
        }
        await clerk.setActive(rest);
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export async function signOut(params) {
    try {
        requireClerk();
        await clerk.signOut(params || undefined);
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export async function handleRedirectCallback(params) {
    try {
        requireClerk();
        await clerk.handleRedirectCallback(params || {}, (to) => navigate(to, false));
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export async function handleEmailLinkVerification(params) {
    try {
        requireClerk();
        await clerk.handleEmailLinkVerification(params || {}, (to) => navigate(to, false));
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

/** Starts an email-link flow and resolves once the link is opened elsewhere. */
export async function signInWithEmailLink(emailAddressId, redirectUrl) {
    try {
        const signIn = requireClerk().client.signIn;
        const { startEmailLinkFlow } = signIn.createEmailLinkFlow();
        const result = await startEmailLinkFlow({ emailAddressId, redirectUrl });
        return { value: toPlain(result) };
    } catch (err) { return errorPayload(err); }
}

export async function signUpWithEmailLink(redirectUrl) {
    try {
        const signUp = requireClerk().client.signUp;
        const { startEmailLinkFlow } = signUp.createEmailLinkFlow();
        const result = await startEmailLinkFlow({ redirectUrl });
        return { value: toPlain(result) };
    } catch (err) { return errorPayload(err); }
}

export async function uploadImage(target, inputElement) {
    try {
        requireClerk();
        const file = inputElement?.files?.[0];
        if (!file) return { value: null };
        const owner = target === 'organization' ? clerk.organization : clerk.user;
        await owner.setProfileImage({ file });
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export async function authenticateWithGoogleOneTap(token, params) {
    try {
        requireClerk();
        const res = await clerk.authenticateWithGoogleOneTap({ token });
        await clerk.handleGoogleOneTapCallback(res, params || {}, (to) => navigate(to, false));
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export async function initGoogleOneTap(clientId, element, dotnetRef, settings) {
    try {
        if (!window.google?.accounts?.id) {
            await new Promise((resolve, reject) => {
                const s = document.createElement('script');
                s.src = 'https://accounts.google.com/gsi/client';
                s.async = true;
                s.onload = resolve;
                s.onerror = () => reject(clerkError('gsi_failed_to_load', 'Failed to load Google Identity Services.'));
                document.head.appendChild(s);
            });
        }
        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (r) => dotnetRef.invokeMethodAsync('OnCredential', r.credential),
            cancel_on_tap_outside: settings?.cancelOnTapOutside ?? true,
            itp_support: settings?.itpSupport ?? true,
            use_fedcm_for_prompt: settings?.fedCmSupport ?? true,
        });
        window.google.accounts.id.prompt();
        return { value: true };
    } catch (err) { return errorPayload(err); }
}

export function cancelGoogleOneTap() { window.google?.accounts?.id?.cancel(); }

// ── Billing payment element (Stripe.js, which is framework-free) ────────────

const paymentElements = new Map();

export async function mountPaymentElement(checkoutHandle, forOrganization, element, appearance) {
    try {
        requireClerk();
        const env = environmentSnap(clerk.environment ?? clerk.__internal_environment);
        let init;
        if (checkoutHandle) {
            const checkout = handles.get(checkoutHandle);
            init = { externalClientSecret: checkout?.externalClientSecret, externalGatewayId: checkout?.externalGatewayId };
        } else {
            const payer = forOrganization ? clerk.organization : clerk.user;
            init = await payer.initializePaymentMethod({ gateway: 'stripe' });
        }
        if (!init?.externalClientSecret) throw clerkError('payment_not_initialized', 'Unable to initialize the payment form.');
        const loadStripe = await clerk.__internal_loadStripeJs();
        const stripe = await loadStripe(env.billing.stripePublishableKey, { stripeAccount: init.externalGatewayId });
        const elements = stripe.elements({ clientSecret: init.externalClientSecret, appearance: appearance || undefined });
        const payment = elements.create('payment', { layout: { type: 'tabs', defaultCollapsed: false } });
        payment.mount(element);
        const id = register({ stripe, elements, payment });
        paymentElements.set(id, true);
        await new Promise((resolve) => payment.on('ready', resolve));
        return { value: id };
    } catch (err) { return errorPayload(err); }
}

/** Confirms the Stripe setup intent and returns the gateway payment token. */
export async function submitPaymentElement(id) {
    try {
        const pe = handles.get(id);
        if (!pe) throw clerkError('stale_handle', 'The payment form is no longer mounted.');
        const { error: submitError } = await pe.elements.submit();
        if (submitError) throw clerkError(submitError.code || 'payment_invalid', submitError.message);
        const { setupIntent, error } = await pe.stripe.confirmSetup({ elements: pe.elements, confirmParams: { return_url: location.href }, redirect: 'if_required' });
        if (error) throw clerkError(error.code || 'payment_failed', error.message);
        return { value: { gateway: 'stripe', paymentToken: setupIntent.payment_method } };
    } catch (err) { return errorPayload(err); }
}

export function unmountPaymentElement(id) {
    const pe = handles.get(id);
    try { pe?.payment?.destroy(); } catch { /* ignore */ }
    handles.delete(id);
    paymentElements.delete(id);
}

// ── DOM helpers used by native components ──────────────────────────────────

export function focus(element) { element?.focus?.(); }

export function focusFirst(root, selector) {
    const panel = root?.querySelector(selector);
    panel?.querySelector('[role=menuitem],button,a[href],input')?.focus?.();
}

export function trapFocus(container, dotnetRef) {
    const previous = document.activeElement;
    const selector = 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';
    const onKey = (e) => {
        if (e.key === 'Escape') { e.preventDefault(); dotnetRef?.invokeMethodAsync('OnEscape'); return; }
        if (e.key !== 'Tab') return;
        const items = [...container.querySelectorAll(selector)].filter((el) => el.offsetParent !== null);
        if (!items.length) return;
        const first = items[0], last = items[items.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    };
    container.addEventListener('keydown', onKey);
    (container.querySelector('[autofocus]') || container.querySelector(selector) || container).focus();
    return { release: () => { container.removeEventListener('keydown', onKey); previous?.focus?.(); } };
}

/** Opens a native modal <dialog> (focus trap, inert background and Escape handling come from the platform). */
export function showDialog(dialog, dotnetRef) {
    if (!dialog) return null;
    const previous = document.activeElement;
    const onCancel = (e) => { e.preventDefault(); dotnetRef?.invokeMethodAsync('OnEscape'); };
    const onClick = (e) => { if (e.target === dialog) dotnetRef?.invokeMethodAsync('OnEscape'); };
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('click', onClick);
    if (!dialog.open) dialog.showModal();
    (dialog.querySelector('[autofocus]') || dialog.querySelector('input,select,textarea,button:not(.cb-dialog__close)') || dialog).focus?.();
    return {
        release: () => {
            dialog.removeEventListener('cancel', onCancel);
            dialog.removeEventListener('click', onClick);
            if (dialog.open) dialog.close();
            previous?.focus?.();
        },
    };
}

export function onOutsideClick(container, dotnetRef) {
    const handler = (e) => { if (!container.contains(e.target)) dotnetRef.invokeMethodAsync('OnOutsideClick'); };
    const onKey = (e) => { if (e.key === 'Escape') { e.stopPropagation(); dotnetRef.invokeMethodAsync('OnOutsideClick'); } };
    document.addEventListener('pointerdown', handler, true);
    container.addEventListener('keydown', onKey);
    return { release: () => { document.removeEventListener('pointerdown', handler, true); container.removeEventListener('keydown', onKey); } };
}

export async function copyToClipboard(text) {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
}

export function currentUrl() { return location.href; }
