const App = {
    setup() {
        const state = Vue.reactive({
            data: {
                total: 0, new: 0, open: 0, inProgress: 0, pending: 0, resolved: 0, closed: 0,
                overdue: 0, critical: 0, unassigned: 0, myAssigned: 0
            }
        });

        const services = {
            getDashboard: async () => AxiosManager.get('/Ticket/GetTicketDashboard', {}),
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TicketAgent']);
                await SecurityManager.validateToken();

                const response = await services.getDashboard();
                state.data = response?.data?.content ?? state.data;
            } catch (e) {
            }
        });

        return { state };
    }
};

Vue.createApp(App).mount('#app');
