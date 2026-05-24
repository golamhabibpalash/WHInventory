var ActivityTracker = (function () {
    var _throttleTimer = null;

    function track(activityType, description) {
        try {
            var userId = StorageManager.getUserId();
            var userEmail = StorageManager.getEmail();
            if (!userId) return;

            var payload = {
                userId: userId,
                userEmail: userEmail,
                activityType: activityType,
                description: description || activityType,
                pageUrl: window.location.pathname,
                userAgent: navigator.userAgent.substring(0, 500)
            };

            AxiosManager.post('/UserActivityLog/CreateUserActivityLog', payload)
                .catch(function () { /* silent — logging must never break the UI */ });
        } catch (e) { /* silent */ }
    }

    function trackPageView() {
        track('PageView', 'Visited: ' + window.location.pathname);
    }

    function trackThrottled(activityType, description) {
        if (_throttleTimer) clearTimeout(_throttleTimer);
        _throttleTimer = setTimeout(function () {
            track(activityType, description);
        }, 500);
    }

    return {
        track: track,
        trackPageView: trackPageView,
        trackThrottled: trackThrottled
    };
})();

document.addEventListener('DOMContentLoaded', function () {
    ActivityTracker.trackPageView();
});
