const App = {
    setup() {
        const SEVERITY_STYLE = {
            Info: { icon: 'fas fa-info-circle', color: '#1b84ff' },
            Success: { icon: 'fas fa-check-circle', color: '#22c55e' },
            Warning: { icon: 'fas fa-exclamation-triangle', color: '#f59e0b' },
            Error: { icon: 'fas fa-times-circle', color: '#ef4444' }
        };

        const state = Vue.reactive({
            items: [],
            unreadCount: 0,
            onlyUnread: false,
            isLoading: false,
            isLive: false,
            loadError: ''
        });

        const services = {
            getList: async (onlyUnread) => {
                const response = await AxiosManager.get(`/Notification/GetNotificationList?onlyUnread=${onlyUnread}&take=100`, {});
                return response?.data?.content;
            }
        };

        const methods = {
            severityIcon: (severity) => (SEVERITY_STYLE[severity] || SEVERITY_STYLE.Info).icon,
            severityColor: (severity) => (SEVERITY_STYLE[severity] || SEVERITY_STYLE.Info).color,

            timeAgo: (iso) => {
                if (!iso) return '';
                const seconds = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
                if (seconds < 60) return 'just now';
                const minutes = Math.floor(seconds / 60);
                if (minutes < 60) return `${minutes}m ago`;
                const hours = Math.floor(minutes / 60);
                if (hours < 24) return `${hours}h ago`;
                const days = Math.floor(hours / 24);
                if (days < 30) return `${days}d ago`;
                return new Date(iso).toLocaleDateString();
            },

            applySnapshot: (snapshot) => {
                state.items = state.onlyUnread
                    ? snapshot.items.filter((x) => !x.isRead)
                    : snapshot.items;
                state.unreadCount = snapshot.unreadCount;
                state.isLive = NotificationManager.isLive();
            },

            refresh: async () => {
                state.isLoading = true;
                state.loadError = '';
                try {
                    await NotificationManager.refreshList();
                    const snapshot = {
                        items: NotificationManager.getItems(),
                        unreadCount: NotificationManager.getUnreadCount()
                    };
                    // The bell cache holds the latest few; fetch the full page list.
                    const content = await services.getList(state.onlyUnread);
                    state.items = content?.data || snapshot.items;
                    state.unreadCount = content?.unreadCount ?? snapshot.unreadCount;
                    state.isLive = NotificationManager.isLive();
                } catch (e) {
                    console.error('Notifications load failed:', e);
                    state.loadError = e?.response?.data?.message || e?.message || 'Unable to load notifications.';
                } finally {
                    state.isLoading = false;
                }
            },

            setFilter: async (onlyUnread) => {
                state.onlyUnread = onlyUnread;
                await methods.refresh();
            },

            open: async (n) => {
                try {
                    await NotificationManager.markRead(n);
                    state.items = state.onlyUnread
                        ? state.items.filter((x) => x.id !== n.id)
                        : state.items.map((x) => x.id === n.id ? { ...x, isRead: true } : x);
                    state.unreadCount = NotificationManager.getUnreadCount();
                } catch (e) {
                    console.error('Open notification failed:', e);
                }
            },

            markAllRead: async () => {
                try {
                    await AxiosManager.post('/Notification/MarkAllNotificationsRead', {});
                    state.items = state.items.map((x) => ({ ...x, isRead: true }));
                    if (state.onlyUnread) state.items = [];
                    state.unreadCount = 0;
                } catch (e) {
                    console.error('Mark all read failed:', e);
                    Swal.fire({ icon: 'error', title: 'Failed', text: e?.response?.data?.message || e?.message });
                }
            },

            remove: async (n) => {
                const confirmed = await Swal.fire({
                    icon: 'warning',
                    title: 'Delete notification?',
                    showCancelButton: true,
                    confirmButtonText: 'Delete'
                });
                if (!confirmed.isConfirmed) return;
                try {
                    await AxiosManager.post('/Notification/DeleteNotification', { id: n.id });
                    state.items = state.items.filter((x) => x.id !== n.id);
                    if (!n.isRead) state.unreadCount = Math.max(0, state.unreadCount - 1);
                    Swal.fire({ icon: 'success', title: 'Deleted' });
                } catch (e) {
                    console.error('Delete notification failed:', e);
                    Swal.fire({ icon: 'error', title: 'Failed', text: e?.response?.data?.message || e?.message });
                }
            }
        };

        let unsubscribe = null;

        Vue.onMounted(async () => {
            try {
                // 'Profiles' is the role every signed-in user holds — this page is for everyone.
                await SecurityManager.authorizePage(['Profiles']);
                await SecurityManager.validateToken();

                unsubscribe = NotificationManager.subscribe((snapshot) => {
                    state.items = state.onlyUnread
                        ? snapshot.items.filter((x) => !x.isRead)
                        : snapshot.items;
                    state.unreadCount = snapshot.unreadCount;
                    state.isLive = NotificationManager.isLive();
                });

                await methods.refresh();
            } catch (e) {
                console.error('Notifications initialisation failed:', e);
                state.loadError = e?.response?.data?.message || e?.message || 'Unable to load notifications.';
            }
        });

        Vue.onUnmounted(() => {
            if (unsubscribe) unsubscribe();
        });

        return {
            state,
            methods,
            t: I18n.t
        };
    }
};

Vue.createApp(App).mount('#app');
