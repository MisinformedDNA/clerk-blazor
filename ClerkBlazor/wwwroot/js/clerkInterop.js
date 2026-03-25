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

        // Redirect to a hash-only URL after sign-in so the browser performs a
        // hash-fragment navigation instead of a full-page reload.  A hash change
        // keeps the Blazor WASM runtime alive and in memory.
        const hashRedirectUrl =
            window.location.pathname + window.location.search + '#';

        _clerk.openSignIn({
            forceRedirectUrl: hashRedirectUrl,
            signUpForceRedirectUrl: hashRedirectUrl,
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

        // addListener fires on auth state updates.
        const unsubscribe = _clerk.addListener(async ({ user, session }) => {
            // When a new session exists but has not been made active yet, activate it.
            // setActive() triggers a second listener invocation; return early here so
            // the second call (where _clerk.user is populated) emits to .NET instead.
            if (!_clerk.isSignedIn && session?.id) {
                try {
                    await _clerk.setActive({ session: session.id });
                } catch {
                    // Fall through and emit with the best available state.
                }
                return;
            }

            const resolvedUser = _clerk.user ?? user ?? null;

            let userJson = null;
            if (resolvedUser) {
                userJson = JSON.stringify({
                    id: resolvedUser.id,
                    email: resolvedUser.primaryEmailAddress?.emailAddress ?? null,
                    firstName: resolvedUser.firstName ?? null,
                    lastName: resolvedUser.lastName ?? null,
                    imageUrl: resolvedUser.imageUrl ?? null
                });
            }

            await dotNetRef.invokeMethodAsync('OnAuthStateChanged', userJson);
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
