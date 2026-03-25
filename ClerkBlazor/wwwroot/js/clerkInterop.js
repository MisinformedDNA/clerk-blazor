/**
 * clerkInterop.js
 * ----------------
 * Minimal Blazor JS interop module for Clerk authentication.
 * Wraps the @clerk/clerk-js browser SDK loaded from the Clerk Frontend API CDN.
 *
 * Loading strategy (clerk-js ≥ 5.x):
 *   The recommended way to embed clerk-js@5 in a non-bundled app is to load the
 *   script from the Clerk Frontend API URL (derived from the publishable key) and
 *   set the key as a `data-clerk-publishable-key` attribute on the script element.
 *   The SDK auto-initialises itself from that attribute; `window.Clerk` is then the
 *   ready-to-use Clerk instance (not the class constructor).
 *   We call `await window.Clerk.load()` to complete initialization, after which
 *   all SDK methods are available.
 *
 * Reference: https://clerk.com/docs/quickstarts/javascript
 *
 * Clerk SDK shape (clerk-js ≥ 5.x):
 *   - window.Clerk             – initialized Clerk instance.
 *   - clerk.user               – currently signed-in User object, or null.
 *   - clerk.openSignIn()       – opens the Clerk sign-in modal/UI.
 *   - clerk.signOut()          – signs the current user out.
 *   - clerk.addListener()      – fires a callback on every auth state change.
 */

window.clerkInterop = (function () {
    /** Internal reference to the loaded Clerk instance. */
    let _clerk = null;

    /**
     * Derive the Clerk Frontend API base URL from a publishable key.
     *
     * Publishable keys have the form:
     *   pk_test_<BASE64URL(domain + "$")>   (development)
     *   pk_live_<BASE64URL(domain + "$")>   (production)
     *
     * @param {string} publishableKey
     * @returns {string}  e.g. "https://stirring-reptile-19.clerk.accounts.dev"
     */
    function getFrontendApiUrl(publishableKey) {
        const prefix = publishableKey.startsWith('pk_live_') ? 'pk_live_' : 'pk_test_';
        const encoded = publishableKey.slice(prefix.length);
        // Clerk uses URL-safe base64 (RFC 4648 §5).  atob() expects standard
        // base64, so replace the URL-safe chars and add any missing padding.
        const standard = encoded.replace(/-/g, '+').replace(/_/g, '/');
        const padded = standard + '='.repeat((4 - standard.length % 4) % 4);
        const domain = atob(padded).replace(/\$+$/, '');
        return 'https://' + domain;
    }

    /**
     * Load the Clerk SDK from the Clerk Frontend API CDN and initialize it.
     *
     * The script element is given a `data-clerk-publishable-key` attribute so
     * that the SDK can auto-initialize from it when the script executes.
     * After the script loads, `window.Clerk` is the ready instance; we call
     * `load()` on it to complete initialization.
     *
     * @param {string} publishableKey  Your Clerk Publishable Key
     *   (starts with "pk_test_" for development or "pk_live_" for production).
     * @returns {Promise<boolean>} true when Clerk is ready.
     */
    async function initialize(publishableKey) {
        // Load the SDK script only once.
        if (!window.Clerk) {
            const frontendApiUrl = getFrontendApiUrl(publishableKey);
            const cdnUrl = frontendApiUrl + '/npm/@clerk/clerk-js@5/dist/clerk.browser.js';

            await new Promise(function (resolve, reject) {
                const script = document.createElement('script');
                script.src = cdnUrl;
                // Provide the key as a data attribute.  The clerk-js IIFE reads
                // document.currentScript.dataset.clerkPublishableKey at load time
                // and auto-constructs the Clerk instance, assigning it to window.Clerk.
                script.setAttribute('data-clerk-publishable-key', publishableKey);
                script.async = true;
                script.crossOrigin = 'anonymous';
                script.type = 'text/javascript';
                script.onload = resolve;
                script.onerror = function () {
                    reject(new Error(
                        'Failed to load the Clerk SDK from ' + cdnUrl + '. ' +
                        'Check your internet connection and verify your publishable key.'
                    ));
                };
                document.head.appendChild(script);
            });
        }

        if (!window.Clerk) {
            throw new Error(
                'Clerk SDK was not initialized after loading from the Clerk Frontend API. ' +
                'Verify your publishable key is correct and that the Clerk application is active.'
            );
        }

        // window.Clerk is the auto-initialized instance (not the class).
        // Call load() to complete SDK initialization before using any methods.
        _clerk = window.Clerk;
        await _clerk.load();

        // Signal that Clerk is fully ready.  Tests (and other JS code) can
        // await this flag before calling openSignIn() / getUser() etc.
        window.__clerkInteropReady = true;

        return true;
    }

    /**
     * Open the Clerk sign-in modal (hosted UI).
     * Note: openSignIn() opens UI and returns immediately; the JS addListener
     * callback (registered via onAuthChange) fires when authentication completes.
     *
     * @returns {Promise<void>}
     */
    async function openSignIn() {
        _assertInitialized();

        // Use documented SignInProps only.
        // Keep redirect on the current document by targeting a hash URL.
        // This prevents a full-page reload after sign-in while keeping the
        // Blazor WASM runtime alive; auth state is propagated via addListener.
        const sameDocumentRedirectUrl =
            window.location.pathname + window.location.search + '#auth-complete';

        _clerk.openSignIn({
            routing: 'hash',
            fallbackRedirectUrl: sameDocumentRedirectUrl,
            forceRedirectUrl: sameDocumentRedirectUrl,
            signUpFallbackRedirectUrl: sameDocumentRedirectUrl,
            signUpForceRedirectUrl: sameDocumentRedirectUrl
        });
    }

    /**
     * Return a minimal user DTO for the currently signed-in user.
     *
     * @returns {Promise<{id: string, email: string, firstName: string|null, lastName: string|null, imageUrl: string|null}|null>}
     *   null when no user is signed in.
     */
    async function getUser() {
        _assertInitialized();
        const user = _clerk.user;
        if (!user) return null;

        // primaryEmailAddress?.emailAddress is the canonical way to get the
        // email from clerk-js >= 5. Adjust if you target an older SDK version.
        return {
            id: user.id,
            email: user.primaryEmailAddress?.emailAddress ?? null,
            firstName: user.firstName ?? null,
            lastName: user.lastName ?? null,
            imageUrl: user.imageUrl ?? null
        };
    }

    /**
     * Sign the current user out.
     *
     * @returns {Promise<void>}
     */
    async function signOut() {
        _assertInitialized();
        await _clerk.signOut();
    }

    /** True while a setActive call is in flight; prevents re-entrant activations. */
    let _activating = false;

    /**
     * Register a .NET callback that fires whenever the Clerk auth state changes.
     * The callback is invoked with the serialised user object (or null on sign-out).
     *
     * @param {DotNet.DotNetObjectReference} dotNetRef  A DotNetObjectReference to your
     *   C# class that implements a method named OnAuthStateChanged(string? userJson).
     * @returns {Function} A disposal function that removes the listener.
     *   (Not easily callable from Blazor, but retained here for completeness.)
     */
    function onAuthChange(dotNetRef) {
        _assertInitialized();

        // addListener fires on every auth state change (sign-in, sign-out,
        // session switch, token refresh).
        //
        // Per Clerk docs, the user/session params passed to the callback reflect
        // the new state — reading _clerk.user synchronously or calling
        // _clerk.load() inside the callback introduces race conditions and should
        // be avoided.
        //
        // In Clerk's hash-routing flow, after sign-in completes:
        //   • Clerk fires the listener with { user: null/undefined, session: undefined }
        //     (the session has been created on the server but not yet activated
        //     client-side in _clerk.user / _clerk.session).
        //   • _clerk.client.activeSessions contains the new session object.
        //   • Calling setActive() activates the session and populates _clerk.user.
        //
        // State legend (callback params):
        //   user === undefined, session === undefined, activeSessions empty
        //     → Clerk is still initialising; skip this tick.
        //   user === null, session resolved or activeSessions non-empty
        //     → Transitional / sign-out; call setActive if a session is pending,
        //       otherwise emit null (anonymous).
        //   user === {...}
        //     → Signed in; user data is ready.
        const unsubscribe = _clerk.addListener(async ({ user, session }) => {
            // Pure loading: Clerk hasn't determined session state yet — skip.
            if (user === undefined && !session?.id && !_clerk.client?.activeSessions?.length) {
                return;
            }

            // If user isn't populated yet but a session exists (in the callback
            // param or already in _clerk.client.activeSessions), call setActive so
            // _clerk.user becomes available.  The _activating flag prevents
            // re-entrant calls while setActive is in flight.
            if (!user && !_activating) {
                const pendingSession = session?.id
                    ? session
                    : _clerk.client?.activeSessions?.[0];
                if (pendingSession?.id) {
                    _activating = true;
                    try {
                        await _clerk.setActive({ session: pendingSession.id });
                        user = _clerk.user;
                    } catch {
                        user = _clerk.user ?? null;
                    } finally {
                        _activating = false;
                    }
                }
            }

            let userJson = null;
            if (user) {
                userJson = JSON.stringify({
                    id: user.id,
                    email: user.primaryEmailAddress?.emailAddress ?? null,
                    firstName: user.firstName ?? null,
                    lastName: user.lastName ?? null,
                    imageUrl: user.imageUrl ?? null
                });
            }

            if (window.location.hash === '#auth-complete') {
                window.history.replaceState({}, '', window.location.pathname + window.location.search);
            }

            try {
                await dotNetRef.invokeMethodAsync('OnAuthStateChanged', userJson);
            } catch (err) {
                console.error('[clerkInterop] OnAuthStateChanged failed:', err?.message ?? err);
            }
        });

        return unsubscribe;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    function _assertInitialized() {
        if (!_clerk) {
            throw new Error(
                'Clerk is not initialized. Call clerkInterop.initialize(publishableKey) first.'
            );
        }
    }

    // Public API
    return { initialize, openSignIn, getUser, signOut, onAuthChange };
}());
