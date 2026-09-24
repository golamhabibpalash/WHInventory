namespace Application.Features.NotificationManager;

public record NotificationPayload
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? Severity { get; init; }
    public string? LinkUrl { get; init; }
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
    public bool IsRead { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public interface INotificationBroadcaster
{
    Task BroadcastAsync(string userId, NotificationPayload payload, CancellationToken cancellationToken = default);
}
