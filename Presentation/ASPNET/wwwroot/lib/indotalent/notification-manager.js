/* NotificationManager — live in-app notifications (bell + toasts + polling fallback).
 *
 * Transport strategy (robust by design, in this order):
 *  1. SignalR push on `/hubs/notifications` (JWT passed as ?access_token=, which the
 *     server's JwtBearer OnMessageReceived accepts for /hubs/*). Automatic reconnect
 *     with backoff; a successful reconnect triggers a catch-up refresh.
 *  2. Polling fallback every 60 s for the unread count + a full list refresh whenever
 *     the bell dropdown opens. If the SignalR CDN script failed to load (offline dev),
 *     polling alone keeps the bell correct — nothing throws.
 *
 * Other pages (e.g. /Notifications) can live-update via NotificationManager.subscribe(fn).
 * Subscriber fns receive { type: 'push'|'refresh', items, unreadCount }.
 */
const NotificationManager = (() => {
    const HUB_URL = '/hubs/notifications';
    const POLL_INTERVAL_MS = 60000;
    const DROPDOWN_LIMIT = 8;

    const SEVERITY = {
        Info: { icon: 'fas fa-info-circle', color: '#1b84ff', swal: 'info' },
        Success: { icon: 'fas fa-check-circle', color: '#22c55e', swal: 'success' },
        Warning: { icon: 'fas fa-exclamation-triangle', color: '#f59e0b', swal: 'warning' },
        Error: { icon: 'fas fa-times-circle', color: '#ef4444', swal: 'error' }
    };

    let started = false;
    let connection = null;
    let liveConnected = false;
    let pollTimer = null;
    let items = [];
    let unreadCount = 0;
    const subscribers = new Set();

    const severityOf = (n) => SEVERITY[n?.severity] || SEVERITY.Info;

    const escapeHtml = (value) => {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    };

    const timeAgo = (iso) => {
        if (!iso) return '';
        const seconds = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
        if (seconds < 60) return 'just now';
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return minutes + 'm ago';
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return hours + 'h ago';
        const days = Math.floor(hours / 24);
        if (days < 30) return days + 'd ago';
        return new Date(iso).toLocaleDateString();
    };

    const notifySubscribers = (type) => {
        subscribers.forEach((fn) => {
            try { fn({ type, items: [...items], unreadCount }); } catch (e) { console.error('Notification subscriber failed:', e); }
        });
    };

    // ── API ──────────────────────────────────────────────────────────────
    const fetchList = async (take) => {
        const response = await AxiosManager.get(`/Notification/GetNotificationList?take=${take || DROPDOWN_LIMIT}`, {});
        const content = response?.data?.content;
        items = content?.data || [];
        unreadCount = content?.unreadCount ?? 0;
        renderBadge();
        renderDropdown();
        notifySubscribers('refresh');
    };

    const fetchUnreadCount = async () => {
        const response = await AxiosManager.get('/Notification/GetNotificationUnreadCount', {});
        unreadCount = response?.data?.content?.unreadCount ?? 0;
        renderBadge();
        notifySubscribers('refresh');
    };

    const refreshQuietly = async () => {
        try {
            // Cheap heartbeat: count only. A mismatch with the cache means we missed a push.
            const before = unreadCount;
            await fetchUnreadCount();
            if (unreadCount !== before) {
                await fetchList();
            }
        } catch (e) {
            console.error('Notification refresh failed:', e);
        }
    };

    // ── Rendering (bell lives outside any Vue app → vanilla DOM) ─────────
    const renderBadge = () => {
        const badge = document.getElementById('notificationBadge');
        if (!badge) return;
        badge.style.display = unreadCount > 0 ? '' : 'none';
        badge.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
    };

    const renderDropdown = () => {
        const list = document.getElementById('notificationDropdownList');
        if (!list) return;
        const count = document.getElementById('notificationDropdownHeader');
        if (count) {
            if (unreadCount > 0) {
                count.textContent = unreadCount > 99 ? '99+ new' : `${unreadCount} new`;
                count.hidden = false;
            } else {
                count.hidden = true;
            }
        }

        if (items.length === 0) {
            list.innerHTML = `
                <div class="notification-dropdown__empty">
                    <span class="notification-dropdown__empty-icon"><i class="fas fa-bell-slash"></i></span>
                    <span class="notification-dropdown__empty-title">You're all caught up</span>
                    <span class="notification-dropdown__empty-text">No new notifications right now.</span>
                </div>`;
            return;
        }

        list.innerHTML = items.map((n) => {
            const sev = severityOf(n);
            const sevKey = (n.severity || 'Info').toLowerCase();
            const message = n.message
                ? `<span class="notification-item__message">${escapeHtml(n.message)}</span>`
                : '';
            return `<a href="#" class="notification-item${n.isRead ? '' : ' notification-item--unread'}" role="menuitem" data-id="${escapeHtml(n.id)}" data-link="${escapeHtml(n.linkUrl || '')}">
                <span class="notification-item__icon notification-item__icon--${sevKey}"><i class="${sev.icon}"></i></span>
                <span class="notification-item__content">
                    <span class="notification-item__title">${escapeHtml(n.title)}</span>
                    ${message}
                    <span class="notification-item__time">${escapeHtml(timeAgo(n.createdAtUtc))}</span>
                </span>
                <span class="notification-item__dot" aria-hidden="true"></span>
            </a>`;
        }).join('');
    };

    const showToast = (n) => {
        if (typeof Swal === 'undefined') return;
        const sev = severityOf(n);
        const toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 6000,
            timerProgressBar: true,
            didOpen: (el) => {
                el.addEventListener('click', () => {
                    Swal.close();
                    openNotification(n);
                });
            }
        });
        toast.fire({ icon: sev.swal, title: n.title || 'New notification', text: n.message || '' });
    };

    const openNotification = async (n) => {
        try {
            if (!n.isRead && n.id) {
                await AxiosManager.post('/Notification/MarkNotificationRead', { id: n.id });
                n.isRead = true;
                unreadCount = Math.max(0, unreadCount - 1);
                const cached = items.find((x) => x.id === n.id);
                if (cached) cached.isRead = true;
                renderBadge();
                renderDropdown();
                notifySubscribers('refresh');
            }
        } catch (e) {
            console.error('Mark notification read failed:', e);
        }
        if (n.linkUrl) window.location.href = n.linkUrl;
    };

    const onPush = (payload) => {
        if (!payload) return;
        items = [payload, ...items].slice(0, DROPDOWN_LIMIT);
        unreadCount += 1;
        renderBadge();
        renderDropdown();
        showToast(payload);
        notifySubscribers('push');
    };

    // ── SignalR ──────────────────────────────────────────────────────────
    const connectLive = async () => {
        if (typeof signalR === 'undefined') return; // CDN unreachable → polling covers us
        try {
            connection = new signalR.HubConnectionBuilder()
                .withUrl(HUB_URL, { accessTokenFactory: () => StorageManager.getAccessToken() })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();
            connection.on('ReceiveNotification', onPush);
            connection.onreconnected(() => {
                liveConnected = true;
                refreshQuietly(); // catch up on anything missed while disconnected
            });
            connection.onclose(() => { liveConnected = false; });
            await connection.start();
            liveConnected = true;
        } catch (e) {
            console.error('Notification live channel failed, using polling:', e);
            connection = null;
            liveConnected = false;
        }
    };

    const start = async () => {
        if (started) return;
        started = true;
        if (!StorageManager.getAccessToken()) return; // login page etc. — nothing to do

        // Refresh the list whenever the bell opens, so it never shows stale rows.
        const toggle = document.getElementById('notificationBellToggle');
        if (toggle) {
            toggle.closest('.dropdown')?.addEventListener('show.bs.dropdown', () => {
                fetchList().catch((e) => console.error('Notification list failed:', e));
            });
        }

        document.getElementById('notificationDropdownList')?.addEventListener('click', (e) => {
            const row = e.target.closest('[data-id]');
            if (!row) return;
            e.preventDefault();
            const n = items.find((x) => x.id === row.getAttribute('data-id'));
            if (n) openNotification(n);
        });

        document.getElementById('notificationMarkAllRead')?.addEventListener('click', async (e) => {
            e.preventDefault();
            try {
                await AxiosManager.post('/Notification/MarkAllNotificationsRead', {});
                items.forEach((x) => { x.isRead = true; });
                unreadCount = 0;
                renderBadge();
                renderDropdown();
                notifySubscribers('refresh');
            } catch (err) {
                console.error('Mark all notifications read failed:', err);
            }
        });

        try {
            await fetchList();
        } catch (e) {
            console.error('Notification initial load failed:', e);
        }

        connectLive();
        pollTimer = setInterval(refreshQuietly, POLL_INTERVAL_MS);
    };

    const stop = () => {
        if (pollTimer) clearInterval(pollTimer);
        pollTimer = null;
        if (connection) connection.stop().catch(() => {});
        connection = null;
        started = false;
    };

    return {
        start,
        stop,
        refresh: refreshQuietly,
        refreshList: fetchList,
        markRead: openNotification,
        getItems: () => [...items],
        getUnreadCount: () => unreadCount,
        isLive: () => liveConnected,
        subscribe: (fn) => {
            subscribers.add(fn);
            return () => subscribers.delete(fn);
        }
    };
})();

document.addEventListener('DOMContentLoaded', () => {
    NotificationManager.start();
});
