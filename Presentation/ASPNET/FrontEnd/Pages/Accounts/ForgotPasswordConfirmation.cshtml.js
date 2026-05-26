const App = {
    setup() {
        const state = Vue.reactive({
            newPassword: '',
            confirmPassword: '',
            errors: { newPassword: '', confirmPassword: '' },
            isSubmitting: false,
        });

        const params = new URLSearchParams(window.location.search);
        const email = params.get('email') ?? '';
        const code = params.get('code') ?? '';

        const handler = {
            submit: async () => {
                state.errors = { newPassword: '', confirmPassword: '' };

                if (!state.newPassword || state.newPassword.length < 6) {
                    state.errors.newPassword = 'Password must be at least 6 characters.';
                    return;
                }
                if (state.newPassword !== state.confirmPassword) {
                    state.errors.confirmPassword = 'Passwords do not match.';
                    return;
                }
                if (!email || !code) {
                    Swal.fire({ icon: 'error', title: 'Invalid Link', text: 'The reset link is missing required parameters. Please request a new one.' });
                    return;
                }

                try {
                    state.isSubmitting = true;
                    const response = await AxiosManager.post('/Security/ForgotPasswordConfirmation', {
                        email,
                        code,
                        newPassword: state.newPassword,
                        confirmPassword: state.confirmPassword
                    });
                    if (response.data.code === 200) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Password Reset Successful',
                            text: 'You are being redirected to login...',
                            timer: 2000,
                            showConfirmButton: false
                        });
                        setTimeout(() => { window.location.href = '/Accounts/Login'; }, 2000);
                    } else {
                        Swal.fire({ icon: 'error', title: 'Reset Failed', text: response.data.message ?? 'Please try again.' });
                    }
                } catch (e) {
                    Swal.fire({ icon: 'error', title: 'Error', text: e.response?.data?.message ?? 'An unexpected error occurred.' });
                } finally {
                    state.isSubmitting = false;
                }
            }
        };

        return { state, handler };
    }
};

Vue.createApp(App).mount('#app');
