/**
 * clerkInterop.js
 * ----------------
 * Minimal Blazor JS interop module for Clerk authentication.
 * Wraps the @clerk/clerk-js browser SDK loaded via CDN (see index.html).
 *
 * Clerk SDK shape (clerk-js >= 5.x):
 *   - window.Clerk is available after the CDN script loads.
 *   - Call `await clerk.load()` before using any methods.
 *   - clerk.user          – currently signed-in User object, or null.
 *   - clerk.openSignIn()  – opens the Clerk sign-in modal/UI.
 *   - clerk.signOut()     – signs the current user out.
 *   - clerk.addListener() – fires a callback on every auth state change.
 *
 * If the Clerk API shape changes in a future SDK version, update the
 * references to window.Clerk, clerk.load(), clerk.user, etc. below.
 */

window.clerkInterop = (function () {
    /** Internal reference to the loaded Clerk instance. */
    let _clerk = null;

    /**
     * Returns the Clerk Publishable Key from window.__clerk_config.
     * Exposed so that App.razor can read the key without using eval().
     *
     * @returns {string|null} The publishable key or null if not configured.
     */
    function getPublishableKey() {
        return (window.__clerk_config && window.__clerk_config.publishableKey) || null;
    }

    /**
     * Initialise Clerk using the publishable key.
     * Must be called once before any other functions.
     *
     * @param {string} publishableKey  Your Clerk Publishable Key
     *   (starts with "pk_test_" for development or "pk_live_" for production).
     * @returns {Promise<boolean>} true when Clerk is ready.
     */
    async function initialize(publishableKey) {
        if (!window.Clerk) {
            throw new Error(
                'Clerk SDK not found on window. ' +
                'Make sure the <script> tag that loads @clerk/clerk-js is included ' +
                'in index.html BEFORE the Blazor framework script.'
            );
        }

        // Clerk constructor accepts the publishable key directly (clerk-js >= 5).
        // If you are using an older version adjust the instantiation accordingly.
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
                'Clerk is not initialised. Call clerkInterop.initialize(publishableKey) first.'
            );
        }
    }

    // Public API
    return { getPublishableKey, initialize, openSignIn, getUser, signOut, onAuthChange };
}());
