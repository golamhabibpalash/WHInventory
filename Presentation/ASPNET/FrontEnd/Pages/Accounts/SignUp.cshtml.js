const App = {
    setup() {
        const state = Vue.reactive({
            organizationName: '',
            slug: '',
            slugEdited: false,
            firstName: '',
            lastName: '',
            email: '',
            password: '',
            confirmPassword: '',
            isSubmitting: false,
            done: false,
            result: {},
            errors: {
                organizationName: '', slug: '', firstName: '', lastName: '',
                email: '', password: '', confirmPassword: ''
            }
        });

        // Turns "Acme Ltd." into "acme-ltd" so the address is filled in for you, until you edit it.
        const toSlug = (value) => (value ?? '')
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .slice(0, 50);

        Vue.watch(() => state.organizationName, (value) => {
            if (!state.slugEdited) {
                state.slug = toSlug(value);
            }
        });

        const handleSlugInput = () => {
            state.slugEdited = true;
            state.errors.slug = '';
        };

        const validateForm = () => {
            Object.keys(state.errors).forEach(k => state.errors[k] = '');
            let isValid = true;

            if (!state.organizationName) {
                state.errors.organizationName = 'Organisation name is required.';
                isValid = false;
            }
            if (!state.slug) {
                state.errors.slug = 'Workspace address is required.';
                isValid = false;
            } else if (!/^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/.test(state.slug)) {
                state.errors.slug = 'Use lowercase letters, digits and hyphens only.';
                isValid = false;
            }
            if (!state.firstName) {
                state.errors.firstName = 'First name is required.';
                isValid = false;
            }
            if (!state.lastName) {
                state.errors.lastName = 'Last name is required.';
                isValid = false;
            }
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
            if (state.password !== state.confirmPassword) {
                state.errors.confirmPassword = 'Passwords do not match.';
                isValid = false;
            }

            return isValid;
        };

        const handleSubmit = async () => {
            try {
                state.isSubmitting = true;

                if (!validateForm()) return;

                const response = await AxiosManager.post('/Tenant/SignUp', {
                    organizationName: state.organizationName,
                    slug: state.slug,
                    firstName: state.firstName,
                    lastName: state.lastName,
                    email: state.email,
                    password: state.password,
                    confirmPassword: state.confirmPassword
                });

                if (response.data.code === 200) {
                    state.result = response.data.content ?? {};
                    state.done = true;
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Could not create the organisation',
                        text: response.data.message ?? 'Please check your details.',
                        confirmButtonText: 'Try Again'
                    });
                }
            } catch (error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Could not create the organisation',
                    text: error.response?.data?.message ?? 'Please try again.',
                    confirmButtonText: 'OK'
                });
            } finally {
                state.isSubmitting = false;
            }
        };

        return { state, handleSubmit, handleSlugInput };
    }
};

Vue.createApp(App).mount('#app');
