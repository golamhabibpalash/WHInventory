// Turns "jane.doe@acme.com" into "Jane Doe" for the greeting.
const displayName = (email) => {
    const local = (email || '').split('@')[0].replace(/[._-]+/g, ' ').trim();
    return local.replace(/\b\w/g, (c) => c.toUpperCase()) || email;
};

// Prefer the auth layout's slide-in toast; fall back to SweetAlert elsewhere.
const notify = ({ variant, title, text, duration }) => {
    if (typeof window.showAuthToast === 'function') {
        window.showAuthToast({ variant, title, text, duration });
        return;
    }
    Swal.fire({
        icon: variant === 'error' ? 'error' : 'success',
        title,
        text,
        timer: variant === 'error' ? undefined : (duration || 2000),
        showConfirmButton: variant === 'error'
    });
};

const App = {
    setup() {
        const state = Vue.reactive({
            email: '',
            password: '',
            rememberMe: false,
            showPassword: false,
            isSubmitting: false,
            errors: {
                email: '',
                password: ''
            }
        });

        const validateForm = () => {
            state.errors.email = '';
            state.errors.password = '';
            let isValid = true;

            if (!state.email) {
                state.errors.email = 'Email is required.';
                isValid = false;
            } else if (!/\S+@\S+\.\S+/.test(state.email)) {
                state.errors.email = 'Please enter a valid email address.';
                isValid = false;
            }

            if (!state.password) {
                state.errors.password = 'Password is required.';
                isValid = false;
            } else if (state.password.length < 6) {
                state.errors.password = 'Password must be at least 6 characters.';
                isValid = false;
            }

            return isValid;
        };

        const handleSubmit = async () => {

            try {
                state.isSubmitting = true;
                await new Promise(resolve => setTimeout(resolve, 300));

                if (!validateForm()) {
                    return;
                }

                const response = await AxiosManager.post('/Security/Login', {
                    email: state.email,
                    password: state.password
                });

                if (response.data.code === 200) {
                    if (state.rememberMe) {
                        localStorage.setItem('rememberedEmail', state.email);
                    } else {
                        localStorage.removeItem('rememberedEmail');
                    }
                    // Drives the "Last login" line on the sign-in screen's branding panel.
                    localStorage.setItem('lastLoginAt', new Date().toISOString());
                    StorageManager.saveLoginResult(response.data);

                    notify({
                        variant: 'success',
                        title: 'Login Successful!',
                        text: `Welcome back, ${displayName(state.email)}`,
                        duration: 2000
                    });

                    setTimeout(() => {
                        window.location.href = '/Dashboards/DefaultDashboard';
                    }, 1600);
                } else {
                    notify({
                        variant: 'error',
                        title: 'Login Failed',
                        text: response.data.message || 'Please check your credentials.'
                    });
                }
            } catch (error) {
                notify({
                    variant: 'error',
                    title: 'An Error Occurred',
                    text: error.response?.data?.message || 'Please try again.'
                });
            } finally {
                state.isSubmitting = false;
            }
        };

        Vue.onMounted(() => {
            // Explain an automatic sign-out rather than dropping the user here silently.
            if (new URLSearchParams(window.location.search).get('reason') === 'idle') {
                notify({
                    variant: 'error',
                    title: 'Signed out',
                    text: 'You were signed out due to inactivity. Please sign in again.',
                    duration: 6000
                });
            }

            const rememberedEmail = localStorage.getItem('rememberedEmail');
            if (rememberedEmail) {
                state.email = rememberedEmail;
                state.rememberMe = true;
            }
        });

        return {
            state,
            handleSubmit
        };
    }
};

Vue.createApp(App).mount('#app');