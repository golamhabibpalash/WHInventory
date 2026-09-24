using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Features.NotificationManager;

public interface INotificationService
{
    Task<Notification?> NotifyAsync(
        string userId,
        string title,
        string? message = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? linkUrl = null,
        string? moduleName = null,
        string? moduleId = null,
        string? actorId = null,
        CancellationToken cancellationToken = default);

    Task NotifyManyAsync(
        IEnumerable<string> userIds,
        string title,
        string? message = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? linkUrl = null,
        string? moduleName = null,
        string? moduleId = null,
        string? actorId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists a per-user in-app notification and pushes it to the recipient's live
/// channel. Broadcast failures are logged and swallowed — the stored notification
/// is still picked up by the client's polling fallback, so a SignalR outage must
/// never fail the business operation that triggered the notification.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ICommandRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationBroadcaster _broadcaster;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ICommandRepository<Notification> repository,
        IUnitOfWork unitOfWork,
        INotificationBroadcaster broadcaster,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task<Notification?> NotifyAsync(
        string userId,
        string title,
        string? message = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? linkUrl = null,
        string? moduleName = null,
        string? moduleId = null,
        string? actorId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var entity = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Severity = severity,
            LinkUrl = linkUrl,
            ModuleName = moduleName,
            ModuleId = moduleId,
            CreatedById = actorId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await BroadcastSafeAsync(entity, cancellationToken);

        return entity;
    }

    public async Task NotifyManyAsync(
        IEnumerable<string> userIds,
        string title,
        string? message = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? linkUrl = null,
        string? moduleName = null,
        string? moduleId = null,
        string? actorId = null,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        foreach (var id in distinctIds)
        {
            await NotifyAsync(id, title, message, severity, linkUrl, moduleName, moduleId, actorId, cancellationToken);
        }
    }

    private async Task BroadcastSafeAsync(Notification entity, CancellationToken cancellationToken)
    {
        try
        {
            await _broadcaster.BroadcastAsync(entity.UserId, new NotificationPayload
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Title = entity.Title,
                Message = entity.Message,
                Severity = entity.Severity.ToString(),
                LinkUrl = entity.LinkUrl,
                ModuleName = entity.ModuleName,
                ModuleId = entity.ModuleId,
                IsRead = entity.IsRead,
                CreatedAtUtc = entity.CreatedAtUtc
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live notification broadcast failed for user {UserId}", entity.UserId);
        }
    }
}
