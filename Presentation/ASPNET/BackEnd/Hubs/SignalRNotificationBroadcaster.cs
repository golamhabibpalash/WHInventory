using Application.Features.NotificationManager;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

/// <summary>
/// Application-layer <see cref="INotificationBroadcaster"/> backed by SignalR.
/// Throws on delivery failure so the caller's safe wrapper can log it — the stored
/// row is still picked up by client polling, so nothing is lost.
/// </summary>
public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastAsync(string userId, NotificationPayload payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(NotificationHub.GroupFor(userId)).SendAsync("ReceiveNotification", payload, cancellationToken);
    }
}
