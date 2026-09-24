using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

/// <summary>
/// Live per-user channel. Each connection joins its owner's private group
/// (<see cref="GroupFor"/>); server code never broadcasts to all clients.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public static string GroupFor(string userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId));
        }

        await base.OnConnectedAsync();
    }
}
