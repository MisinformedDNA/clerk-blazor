/**
 * clerkInterop.js
 * ----------------
 * Minimal Blazor JS interop module for Clerk authentication.
 * Wraps the @clerk/clerk-js browser SDK loaded from CDN.
 *
 * The Clerk SDK is loaded *dynamically* inside initialize() so that the CDN
 * script is never added to the page until we have a valid publishable key.
 * This prevents the "Missing publishableKey" error that newer clerk-js versions
 * throw when the browser bundle is loaded without a key configured upfront.
 *
 * Clerk SDK shape (clerk-js >= 5.x):
 *   - window.Clerk is available after the CDN script loads.
 *   - Call `await clerk.load()` before using any methods.
 *   - clerk.user          – currently signed-in User object, or null.
 *   - clerk.openSignIn()  – opens the Clerk sign-in modal/UI.
 *   - clerk.signOut()     – signs the current user out.
 *   - clerk.addListener() – fires a callback on every auth state change.
 *
 * If the Clerk API shape changes in a future SDK version, update the CDN URL
 * and the references to clerk.load(), clerk.user, etc. below.
 */

window.clerkInterop = (function () {
    /** Internal reference to the loaded Clerk instance. */
    let _clerk = null;

    /** CDN URL for the Clerk browser bundle. Update the version pin if needed. */
    const CLERK_CDN_URL =
        'https://cdn.jsdelivr.net/npm/@clerk/clerk-js@5/dist/clerk.browser.js';

    /**
     * Dynamically load the Clerk SDK from CDN and initialise it with the key.
     *
     * The script is appended to <head> only when this function is called, so
     * the SDK never executes without a publishable key being available.
     *
     * @param {string} publishableKey  Your Clerk Publishable Key
     *   (starts with "pk_test_" for development or "pk_live_" for production).
     * @returns {Promise<boolean>} true when Clerk is ready.
     */
    async function initialize(publishableKey) {
        // Load the SDK script only once.
        if (!window.Clerk) {
            await new Promise(function (resolve, reject) {
                var script = document.createElement('script');
                script.src = CLERK_CDN_URL;
                script.async = true;
                script.crossOrigin = 'anonymous';
                script.type = 'text/javascript';
                script.onload = resolve;
                script.onerror = function () {
                    reject(new Error(
                        'Failed to load the Clerk SDK from CDN (' + CLERK_CDN_URL + '). ' +
                        'Check your internet connection and Content Security Policy settings.'
                    ));
                };
                document.head.appendChild(script);
            });
        }

        if (!window.Clerk) {
            throw new Error(
                'Clerk SDK was not exposed on window after loading. ' +
                'Verify the CDN URL in clerkInterop.js points to a valid Clerk browser bundle.'
            );
        }

        // Clerk constructor accepts the publishable key directly (clerk-js >= 5).
        _clerk = new window.Clerk(publishableKey);
        await _clerk.load();
        return true;
    }

    /**
     * Open the Clerk sign-in modal (hosted UI).
     * Resolves after the user dismisses the modal, whether or not they
     * completed sign-in.  Call getUser() afterwards to check auth state.
     *
     * @returns {Promise<void>}
     */
    async function openSignIn() {
        _assertInitialized();
        // openSignIn() shows the hosted sign-in component inside the page.
        // You can pass appearance/redirect options as the first argument;
        // see https://clerk.com/docs/references/javascript/clerk/clerk#open-sign-in
        await _clerk.openSignIn();
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

        // addListener fires on every auth state update (sign-in, sign-out, token refresh).
        // https://clerk.com/docs/references/javascript/clerk/clerk#add-listener
        const unsubscribe = _clerk.addListener(async ({ user }) => {
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
            // Invoke the C# method – must be marked [JSInvokable] in C#.
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
