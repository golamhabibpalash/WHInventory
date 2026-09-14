const SecurityManager = {
    authorizePage: async (requiredRoles) => {
        const userRoles = StorageManager.getUserRoles();
        const isAuthorized = userRoles?.some(role => requiredRoles.includes(role));
        if (!isAuthorized) {
            SecurityManager.forceReLogin('Unauthorized');
            throw new Error('Unauthorized');
        }
    },

    // Calls the server to validate the current access token. If invalid or revoked the user is
    // redirected to login. Unlike before, this now truly gates rendering — callers that `await`
    // this will not proceed until the check completes.
    validateToken: async () => {
        try {
            const response = await AxiosManager.post('/Security/ValidateToken', {});
            if (response?.data?.code !== 200) {
                SecurityManager.forceReLogin('Token not valid');
                throw new Error('Token not valid');
            }
        } catch (error) {
            if (error.message === 'Token not valid') throw error;
            SecurityManager.forceReLogin(error?.response?.data?.message || 'Error validating token');
            throw error;
        }
    },

    // Re-fetches the current user's roles from the server and updates localStorage + sidebar.
    // Call after any role change to keep the admin's own state fresh.
    refreshSession: async () => {
        try {
            const refreshToken = StorageManager.getRefreshToken();
            if (!refreshToken) return;
            const response = await AxiosManager.post('/Security/RefreshToken', { refreshToken });
            if (response?.data?.code === 200) {
                StorageManager.saveLoginResult(response?.data);
                SecurityManager.reloadSidebar();
            }
        } catch (e) {
            // Silent — refresh failed, will self-heal on next 498
        }
    },

    // Re-renders the sidebar using the current (possibly updated) roles from localStorage.
    reloadSidebar: () => {
        document.dispatchEvent(new CustomEvent('i18n:locale-changed'));
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