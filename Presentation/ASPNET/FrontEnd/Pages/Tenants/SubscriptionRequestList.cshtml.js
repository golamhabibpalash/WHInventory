const App = {
    setup() {
        const state = Vue.reactive({
            rows: [],
            pendingOnly: true,
            isSubmitting: false
        });

        // SubscriptionRequestStatus enum (see Domain/Enums/SubscriptionRequestStatus.cs).
        const statusMeta = {
            0: { label: 'Pending', cls: 'bg-warning text-dark' },
            1: { label: 'Approved', cls: 'bg-success' },
            2: { label: 'Rejected', cls: 'bg-danger' },
            3: { label: 'Cancelled', cls: 'bg-secondary' }
        };

        const services = {
            getList: async (pendingOnly) =>
                await AxiosManager.get(`/SubscriptionRequest/GetSubscriptionRequestList?pendingOnly=${pendingOnly}`, {}),
            approve: async (id, decisionNote, updatedById) =>
                await AxiosManager.post('/SubscriptionRequest/ApproveSubscriptionRequest', { id, decisionNote, updatedById }),
            reject: async (id, decisionNote, updatedById) =>
                await AxiosManager.post('/SubscriptionRequest/RejectSubscriptionRequest', { id, decisionNote, updatedById })
        };

        const methods = {
            load: async () => {
                const response = await services.getList(state.pendingOnly);
                state.rows = response?.data?.content?.data ?? [];
            }
        };

        const handler = {
            reload: async () => { await methods.load(); },
            statusLabel: (s) => statusMeta[s]?.label ?? 'Unknown',
            statusClass: (s) => statusMeta[s]?.cls ?? 'bg-secondary',
            formatDate: (value) => {
                if (!value) return '—';
                const d = new Date(value);
                const dd = String(d.getDate()).padStart(2, '0');
                const mm = String(d.getMonth() + 1).padStart(2, '0');
                return `${dd}/${mm}/${d.getFullYear()}`;
            },
            approve: async (row) => {
                const confirm = await Swal.fire({
                    icon: 'question',
                    title: `Approve ${row.requestedPlanName}?`,
                    text: `This applies the plan to ${row.tenantName || 'the tenant'} immediately.`,
                    input: 'text',
                    inputPlaceholder: 'Optional note',
                    showCancelButton: true,
                    confirmButtonText: 'Approve'
                });
                if (!confirm.isConfirmed) return;
                await handler.decide(() => services.approve(row.id, confirm.value || null, StorageManager.getUserId()), 'Request approved');
            },
            reject: async (row) => {
                const confirm = await Swal.fire({
                    icon: 'warning',
                    title: `Reject this request?`,
                    input: 'text',
                    inputPlaceholder: 'Optional reason',
                    showCancelButton: true,
                    confirmButtonText: 'Reject',
                    confirmButtonColor: '#dc3545'
                });
                if (!confirm.isConfirmed) return;
                await handler.decide(() => services.reject(row.id, confirm.value || null, StorageManager.getUserId()), 'Request rejected');
            },
            decide: async (action, successTitle) => {
                try {
                    state.isSubmitting = true;
                    const response = await action();
                    if (response.data.code === 200) {
                        await methods.load();
                        Swal.fire({ icon: 'success', title: successTitle, timer: 1200, showConfirmButton: false });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Action failed',
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
                await SecurityManager.authorizePage(['Tenants']);
                await SecurityManager.validateToken();
                await methods.load();
            } catch (e) {
            }
        });

        return { state, handler, t: I18n.t };
    }
};

Vue.createApp(App).mount('#app');
