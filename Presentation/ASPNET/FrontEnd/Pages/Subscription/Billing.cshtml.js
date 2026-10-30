const App = {
    setup() {
        const state = Vue.reactive({
            subscription: null,
            usage: null,
            usageRows: [],
            plans: [],
            pendingRequest: null,
            isReadOnly: false,
            isSubmitting: false
        });

        // SubscriptionStatus enum (see Domain/Enums/SubscriptionStatus.cs).
        const statusMeta = {
            0: { label: 'Trialing', cls: 'bg-info' },
            1: { label: 'Active', cls: 'bg-success' },
            2: { label: 'Past Due', cls: 'bg-warning text-dark' },
            3: { label: 'Suspended', cls: 'bg-secondary' },
            4: { label: 'Canceled', cls: 'bg-danger' },
            5: { label: 'Expired', cls: 'bg-danger' }
        };

        const services = {
            getMySubscription: async () => await AxiosManager.get('/Subscription/GetMySubscription', {}),
            getPlans: async () => await AxiosManager.get('/SubscriptionPlan/GetSubscriptionPlanList?activeOnly=true&publicOnly=true', {}),
            getMyRequests: async () => await AxiosManager.get('/SubscriptionRequest/GetMySubscriptionRequestList', {}),
            createRequest: async (requestedPlanId, createdById) => await AxiosManager.post('/SubscriptionRequest/CreateSubscriptionRequest', { requestedPlanId, createdById }),
            cancelRequest: async (id, updatedById) => await AxiosManager.post('/SubscriptionRequest/CancelSubscriptionRequest', { id, updatedById })
        };

        const methods = {
            load: async () => {
                const sub = await services.getMySubscription();
                const content = sub?.data?.content;
                state.subscription = content?.subscription ?? null;
                state.usage = content?.usage ?? null;
                state.isReadOnly = content?.isReadOnly ?? false;

                const plans = await services.getPlans();
                state.plans = plans?.data?.content?.data ?? [];

                const requests = await services.getMyRequests();
                const list = requests?.data?.content?.data ?? [];
                // Status 0 = Pending (see Domain/Enums/SubscriptionRequestStatus.cs).
                state.pendingRequest = list.find(r => r.status === 0) ?? null;

                methods.buildUsageRows();
            },
            buildUsageRows: () => {
                if (!state.usage) {
                    state.usageRows = [];
                    return;
                }
                const sub = state.subscription;
                const row = (label, used, limit) => {
                    const unlimited = limit === null || limit === undefined;
                    const percent = unlimited || limit === 0 ? 0 : Math.min(100, Math.round((used / limit) * 100));
                    return {
                        label,
                        used,
                        limitLabel: unlimited ? 'Unlimited' : limit,
                        percent,
                        danger: !unlimited && used >= limit
                    };
                };
                state.usageRows = [
                    row('Users', state.usage.users, sub?.maxUsers),
                    row('Warehouses', state.usage.warehouses, sub?.maxWarehouses),
                    row('Products', state.usage.products, sub?.maxProducts)
                ];
            }
        };

        const handler = {
            statusLabel: (status) => statusMeta[status]?.label ?? 'Unknown',
            statusClass: (status) => statusMeta[status]?.cls ?? 'bg-secondary',
            limitLabel: (limit) => (limit === null || limit === undefined) ? 'Unlimited' : limit,
            formatDate: (value) => {
                if (!value) return '—';
                const d = new Date(value);
                const dd = String(d.getDate()).padStart(2, '0');
                const mm = String(d.getMonth() + 1).padStart(2, '0');
                const yyyy = d.getFullYear();
                return `${dd}/${mm}/${yyyy}`;
            },
            formatMoney: (amount, currency) => {
                const value = Number(amount || 0).toFixed(2);
                return `${currency || 'USD'} ${value}`;
            },
            requestPlan: async (plan) => {
                const confirm = await Swal.fire({
                    icon: 'question',
                    title: `Request the ${plan.name} plan?`,
                    text: 'Your platform administrator will review and approve the change.',
                    showCancelButton: true,
                    confirmButtonText: 'Send request',
                    cancelButtonText: 'Cancel'
                });
                if (!confirm.isConfirmed) return;

                try {
                    state.isSubmitting = true;
                    const response = await services.createRequest(plan.id, StorageManager.getUserId());
                    if (response.data.code === 200) {
                        await methods.load();
                        Swal.fire({
                            icon: 'success',
                            title: 'Request submitted',
                            text: 'An administrator will review your request shortly.',
                            timer: 1500,
                            showConfirmButton: false
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Could not submit request',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            cancelRequest: async (request) => {
                try {
                    state.isSubmitting = true;
                    const response = await services.cancelRequest(request.id, StorageManager.getUserId());
                    if (response.data.code === 200) {
                        await methods.load();
                        Swal.fire({ icon: 'success', title: 'Request cancelled', timer: 1200, showConfirmButton: false });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Could not cancel request',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Subscription', 'Profiles']);
                await SecurityManager.validateToken();
                await methods.load();
            } catch (e) {
            }
        });

        return {
            state,
            handler,
            t: I18n.t
        };
    }
};

Vue.createApp(App).mount('#app');
