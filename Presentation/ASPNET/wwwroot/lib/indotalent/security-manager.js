const SecurityManager = {
    authorizePage: async (requiredRoles) => {
        const userRoles = StorageManager.getUserRoles();
        const isAuthorized = userRoles?.some(role => requiredRoles.includes(role));
        if (!isAuthorized) {
            SecurityManager.forceReLogin('Unauthorized');
        }
    },

    // Every page's onMounted awaits this before it may render anything. The check itself is a
    // network round trip the page doesn't otherwise need: AxiosManager already refreshes an
    // expired access token silently on a 498 response (see axios-manager.js), so a token that is
    // merely expired self-heals on the page's own first API call. This only needs to catch a
    // token that is invalid beyond refresh (revoked, wrong tenant, etc.) — that's rare, so it
    // runs in the background instead of gating first paint. Callers keep `await`ing it; the
    // returned promise just resolves immediately rather than after the round trip.
    validateToken: async () => {
        AxiosManager.post('/Security/ValidateToken', {})
            .then((response) => {
                if (response?.data?.code !== 200) {
                    SecurityManager.forceReLogin('Token not valid');
                }
            })
            .catch((error) => {
                SecurityManager.forceReLogin(error?.response?.data?.message || 'Error validating token');
            });

        return true;
    },

    forceReLogin: (title) => {
        Swal.fire({
            icon: 'error',
            title: title,
            text: 'You are being redirected...',
            timer: 2000,
            showConfirmButton: false
        });
        setTimeout(() => {
            window.location.href = '/Accounts/Login';
        }, 2000);
    }

};