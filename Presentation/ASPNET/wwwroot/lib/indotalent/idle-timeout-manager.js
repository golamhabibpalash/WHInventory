/**
 * Signs a user out after a period of inactivity.
 *
 * The timeout is per-user: the server resolves it (user override, else the configured system
 * default) and hands it over at login and on every token refresh, so an admin's change takes
 * effect on the user's next refresh without needing them to re-login.
 *
 * Activity is shared across tabs through a localStorage timestamp, so working in one tab keeps
 * every other tab of the same session alive.
 */
const IdleTimeoutManager = (() => {
    const LAST_ACTIVITY_KEY = 'idleLastActivityAt';
    const DEFAULT_TIMEOUT_MINUTES = 30;
    const WARNING_SECONDS = 60;
    const TICK_MS = 1000;
    // Avoid writing to localStorage on every mousemove.
    const WRITE_THROTTLE_MS = 5000;

    const ACTIVITY_EVENTS = [
        'mousedown', 'mousemove', 'wheel', 'touchstart', 'touchmove', 'keydown', 'scroll', 'click'
    ];

    let timeoutMs = DEFAULT_TIMEOUT_MINUTES * 60 * 1000;
    let tickHandle = null;
    let lastWriteAt = 0;
    let warningShown = false;
    let expired = false;
    let started = false;

    const now = () => Date.now();

    const readLastActivity = () => {
        try {
            const raw = window.localStorage.getItem(LAST_ACTIVITY_KEY);
            const parsed = raw ? parseInt(raw, 10) : NaN;
            return isNaN(parsed) ? now() : parsed;
        } catch (e) {
            return now();
        }
    };

    const writeLastActivity = (stamp) => {
        try {
            window.localStorage.setItem(LAST_ACTIVITY_KEY, String(stamp));
        } catch (e) { /* private mode / quota — timer still works in-tab */ }
    };

    const isAuthenticated = () => {
        try {
            return !!StorageManager.getUserId();
        } catch (e) {
            return false;
        }
    };

    const resolveTimeoutMs = () => {
        const minutes = Number(StorageManager.getSessionTimeoutMinutes());
        const safe = Number.isFinite(minutes) && minutes > 0 ? minutes : DEFAULT_TIMEOUT_MINUTES;
        return safe * 60 * 1000;
    };

    const recordActivity = (force) => {
        if (expired) return;
        const stamp = now();
        if (!force && stamp - lastWriteAt < WRITE_THROTTLE_MS) return;
        lastWriteAt = stamp;
        writeLastActivity(stamp);
    };

    const dismissWarning = () => {
        if (warningShown && typeof Swal !== 'undefined' && Swal.isVisible()) {
            Swal.close();
        }
        warningShown = false;
    };

    const staySignedIn = () => {
        dismissWarning();
        recordActivity(true);
    };

    const showWarning = (secondsLeft) => {
        if (warningShown || typeof Swal === 'undefined') return;
        warningShown = true;

        Swal.fire({
            icon: 'warning',
            title: 'Still there?',
            html: `You will be signed out in <b id="idleCountdown">${secondsLeft}</b> seconds due to inactivity.`,
            showConfirmButton: true,
            confirmButtonText: 'Stay signed in',
            allowOutsideClick: false,
            allowEscapeKey: false
        }).then((result) => {
            // Any dismissal that is not the logout closing it counts as "I'm here".
            if (!expired && result.isConfirmed) {
                staySignedIn();
            }
        });
    };

    const updateCountdown = (secondsLeft) => {
        const el = document.getElementById('idleCountdown');
        if (el) el.textContent = String(Math.max(secondsLeft, 0));
    };

    const signOut = () => {
        if (expired) return;
        expired = true;
        stop();
        dismissWarning();

        // Per design this clears only this browser; other devices keep their sessions and the
        // orphaned refresh token still expires on its own schedule server-side.
        try {
            StorageManager.clearSession();
            window.localStorage.removeItem(LAST_ACTIVITY_KEY);
        } catch (e) { /* ignore */ }

        const target = '/Accounts/Login?reason=idle';
        if (window.location.pathname !== '/Accounts/Login') {
            window.location.href = target;
        }
    };

    const tick = () => {
        if (!isAuthenticated()) {
            stop();
            return;
        }

        const idleFor = now() - readLastActivity();
        const remainingMs = timeoutMs - idleFor;

        if (remainingMs <= 0) {
            signOut();
            return;
        }

        const remainingSeconds = Math.ceil(remainingMs / 1000);

        if (remainingSeconds <= WARNING_SECONDS) {
            showWarning(remainingSeconds);
            updateCountdown(remainingSeconds);
        } else if (warningShown) {
            // Another tab reported activity — take the warning back down.
            dismissWarning();
        }
    };

    const onActivity = () => recordActivity(false);

    const onStorage = (event) => {
        if (event.key === LAST_ACTIVITY_KEY && warningShown) {
            dismissWarning();
        }
    };

    const start = () => {
        if (started || !isAuthenticated()) return;
        started = true;

        timeoutMs = resolveTimeoutMs();

        // Treat a page load as activity so a fresh navigation never lands mid-countdown.
        recordActivity(true);

        ACTIVITY_EVENTS.forEach((name) =>
            window.addEventListener(name, onActivity, { passive: true }));
        window.addEventListener('storage', onStorage);

        tickHandle = window.setInterval(tick, TICK_MS);
    };

    const stop = () => {
        started = false;
        if (tickHandle) {
            window.clearInterval(tickHandle);
            tickHandle = null;
        }
        ACTIVITY_EVENTS.forEach((name) => window.removeEventListener(name, onActivity));
        window.removeEventListener('storage', onStorage);
    };

    /** Re-read the timeout after a token refresh, so admin changes apply without a re-login. */
    const refreshSettings = () => {
        timeoutMs = resolveTimeoutMs();
    };

    return {
        start,
        stop,
        refreshSettings,
        recordActivity: () => recordActivity(true),
        getTimeoutMinutes: () => Math.round(timeoutMs / 60000)
    };
})();

document.addEventListener('DOMContentLoaded', () => IdleTimeoutManager.start());
