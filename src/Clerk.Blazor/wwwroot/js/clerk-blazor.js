// Clerk.Blazor bridge. Clerk owns the DOM inside mounted UI islands; Blazor owns
// their containers and the C# authentication/state integration.

export const CLERK_JS_VERSION = '6.42.0';
export const CLERK_UI_VERSION = '1.42.1';

// UI calls go through lifecycle-aware helpers, not the JSON resource API.
const FORBIDDEN_METHOD = /^(mount|unmount|open|close)[A-Z]|^__internal_(?!loadStripeJs$)/;
const MAX_HANDLES = 1000;

let clerk = null;
let dotnet = null;
let options = {};
let seq = 0;
let disposers = [];
let loadPromise = null;
const handles = new Map();
let nextHandle = 1;
const uiMounts = new Set();
const uiComponents = new Set([
    'SignIn', 'SignUp', 'UserAvatar', 'UserButton', 'UserProfile',
    'OrganizationProfile', 'CreateOrganization', 'OrganizationSwitcher',
    'OrganizationList', 'Waitlist', 'PricingTable', 'OAuthConsent', 'APIKeys',
    'TaskChooseOrganization', 'TaskResetPassword', 'TaskSetupMFA', 'GoogleOneTap',
]);
const uiModals = {
    SignIn: ['openSignIn', 'closeSignIn'],
    SignUp: ['openSignUp', 'closeSignUp'],
    UserProfile: ['openUserProfile', 'closeUserProfile'],
    OrganizationProfile: ['openOrganizationProfile', 'closeOrganizationProfile'],
    CreateOrganization: ['openCreateOrganization', 'closeCreateOrganization'],
    Waitlist: ['openWaitlist', 'closeWaitlist'],
    Checkout: ['__internal_openCheckout', '__internal_closeCheckout'],
    PlanDetails: ['__internal_openPlanDetails', '__internal_closePlanDetails'],
    SubscriptionDetails: ['__internal_openSubscriptionDetails', '__internal_closeSubscriptionDetails'],
    Reverification: ['__internal_openReverification', '__internal_closeReverification'],
};

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

export function uiScriptUrl(opts) {
    if (opts.clerkUIUrl) return opts.clerkUIUrl;
    const base = opts.proxyUrl ? opts.proxyUrl.replace(/\/$/, '') : 'https://' + frontendApiFromKey(opts.publishableKey);
    return `${base}/npm/@clerk/ui@${opts.clerkUIVersion || CLERK_UI_VERSION}/dist/ui.browser.js`;
}

function injectScript(src, opts, attribute = 'data-clerk-blazor') {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.setAttribute(attribute, '');
        script.setAttribute('data-clerk-publishable-key', opts.publishableKey);
        if (opts.proxyUrl) script.setAttribute('data-clerk-proxy-url', opts.proxyUrl);
        if (opts.domain) script.setAttribute('data-clerk-domain', opts.domain);
        if (opts.nonce) script.nonce = opts.nonce;
        script.crossOrigin = 'anonymous';
        script.async = true;
        const timeout = setTimeout(() => fail(), 15000);
        const fail = () => {
            clearTimeout(timeout);
            script.remove();
            reject(clerkError('script_failed_to_load', `Failed to load Clerk script from ${src}. Check the network and Content Security Policy.`));
        };
        script.addEventListener('load', () => { clearTimeout(timeout); resolve(); }, { once: true });
        script.addEventListener('error', fail, { once: true });
        script.src = src;
        document.head.appendChild(script);
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
 * Loads ClerkJS and, unless opted out, Clerk UI. Wires listeners to ClerkService.
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
        await Promise.all([
            window.Clerk ? Promise.resolve() : injectScript(scriptUrl(options), options),
            options.loadUserInterface === false || window.__internal_ClerkUICtor
                ? Promise.resolve()
                : injectScript(uiScriptUrl(options), options, 'data-clerk-ui-script'),
        ]);
        if (!window.Clerk) throw clerkError('clerkjs_not_initialized', 'ClerkJS did not initialize. Verify the publishable key.');
        clerk = typeof window.Clerk === 'function'
            ? new window.Clerk(options.publishableKey, { proxyUrl: options.proxyUrl, domain: options.domain })
            : window.Clerk;

        const loadOptions = {
            ...(options.loadOptions || {}),
            routerPush: (to) => navigate(to, false),
            routerReplace: (to) => navigate(to, true),
        };
        delete loadOptions.ui;
        delete loadOptions.clerkUICtor;
        delete loadOptions.clerkUiCtor;
        if (options.loadUserInterface !== false) {
            if (!window.__internal_ClerkUICtor) throw clerkError('clerk_ui_not_initialized', 'Clerk UI did not initialize.');
            loadOptions.ui = { ClerkUI: window.__internal_ClerkUICtor };
        }
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
    for (const mount of [...uiMounts]) mount.release();
    if (clerk && options.loadUserInterface !== false) {
        closeUi();
        clerk.closeGoogleOneTap?.();
    }
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
        web3Strategies: us.web3FirstFactors || [],
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

function resolveTarget(root, path) {
    const parts = path.split('.');
    const name = parts.pop();
    if (FORBIDDEN_METHOD.test(name)) throw clerkError('forbidden_method', `Use the Clerk UI components or OpenUiAsync for '${name}', rather than the resource API.`);
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

// ── Official Clerk UI ──────────────────────────────────────────────────────

function requireUi() {
    requireClerk();
    if (options.loadUserInterface === false) {
        throw clerkError('clerk_ui_disabled', 'Clerk UI is disabled. Enable ClerkOptions.LoadUserInterface to use built-in UI components.');
    }
}

function uiProps(props, dotnetRef, isAlive) {
    const { __events = [], __customItems = [], __profilePropsKey, ...result } = props || {};
    const dispatch = (name, value) => {
        if (!isAlive()) return;
        return dotnetRef.invokeMethodAsync('OnUiEvent', name, value)
            .catch(error => console.error('[Clerk.Blazor] UI event failed', error));
    };
    for (const name of __events) {
        result[name] = (...args) => dispatch(name, toPlain(args[0]) ?? null);
    }
    const pages = [], menu = [];
    for (const item of __customItems) {
        const entry = { label: item.label };
        const attach = (id, mountKey, unmountKey) => {
            if (!id) return;
            const element = document.getElementById(id);
            if (!element) throw clerkError('missing_ui_slot', `Razor content '${id}' was not rendered.`);
            const home = element.parentNode;
            entry[mountKey] = (target) => target.appendChild(element);
            entry[unmountKey] = () => home.appendChild(element);
        };
        attach(item.iconId, 'mountIcon', 'unmountIcon');
        attach(item.contentId, 'mount', 'unmount');
        if (item.kind === 'page' || item.kind === 'link') {
            entry.url = item.url;
            pages.push(entry);
        } else {
            if (item.href) entry.href = item.href;
            if (item.open) entry.open = item.open;
            if (item.hasClick) entry.onClick = () => dispatch(item.id, null);
            menu.push(entry);
        }
    }
    if (pages.length) {
        const target = __profilePropsKey
            ? (result[__profilePropsKey] = { ...result[__profilePropsKey] })
            : result;
        target.customPages = [...(target.customPages || []), ...pages];
    }
    if (menu.length) result.customMenuItems = [...(result.customMenuItems || []), ...menu];
    return result;
}

/** Mounts only into a Blazor-owned, otherwise empty container. */
export function mountUi(name, node, props, dotnetRef) {
    requireUi();
    if (!uiComponents.has(name)) throw clerkError('invalid_ui_component', `Unsupported Clerk component '${name}'.`);
    if (!node?.isConnected) throw clerkError('invalid_ui_container', 'The Clerk UI container is no longer attached.');
    let alive = true;
    let currentProps = JSON.stringify(props);
    let oneTap;
    const mount = () => {
        const prepared = uiProps(props, dotnetRef, () => alive);
        if (name === 'GoogleOneTap') oneTap = openOneTap(prepared, node);
        else clerk['mount' + name](node, prepared);
    };
    const unmount = () => {
        if (name === 'GoogleOneTap') oneTap.release();
        else clerk['unmount' + name](node);
    };
    mount();
    const handle = {
        update(nextProps) {
            if (!alive || currentProps === JSON.stringify(nextProps)) return;
            unmount();
            props = nextProps;
            currentProps = JSON.stringify(props);
            mount();
        },
        release() {
            if (!alive) return;
            alive = false;
            unmount();
            uiMounts.delete(handle);
        },
    };
    uiMounts.add(handle);
    return handle;
}

let modalOwner = null;
let modalAlive = () => false;

/** All wrapper entry points share one modal slot, including drawers. */
export function closeUi(owner) {
    if (owner !== undefined && owner !== modalOwner) return;
    modalAlive.cancel?.();
    modalOwner = null;
    for (const [, close] of Object.values(uiModals)) clerk[close]?.();
}

export async function openUi(name, props, dotnetRef, owner) {
    try {
        requireUi();
        const methods = uiModals[name];
        if (!methods) throw clerkError('invalid_ui_modal', `Unsupported Clerk modal '${name}'.`);
        closeUi();
        modalOwner = owner ?? null;
        let alive = true;
        modalAlive = () => alive;
        modalAlive.cancel = () => { alive = false; };
        clerk[methods[0]](uiProps(props, dotnetRef, modalAlive));
        return { value: true };
    } catch (error) { return errorPayload(error); }
}

let oneTapOwner = null;
export function openOneTap(props, owner) {
    requireUi();
    clerk.closeGoogleOneTap();
    oneTapOwner = owner;
    clerk.openGoogleOneTap(props || {});
    return {
        release() {
            if (oneTapOwner !== owner) return;
            oneTapOwner = null;
            clerk.closeGoogleOneTap();
        },
    };
}

export async function reverify(level) {
    try {
        requireUi();
        closeUi();
        const verified = await new Promise((resolve) => {
            let done = false;
            const finish = (value) => {
                if (done) return;
                done = true;
                resolve(value);
            };
            modalAlive = () => !done;
            modalAlive.cancel = () => finish(false);
            clerk.__internal_openReverification({
                level,
                afterVerification: () => finish(true),
                afterVerificationCancelled: () => finish(false),
            });
        });
        return { value: verified };
    } catch (error) { return errorPayload(error); }
}

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
