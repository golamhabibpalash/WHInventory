using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Notification : BaseEntity
{
    /// <summary>Recipient user id (AspNetUsers.Id). Notifications are always per-user.</summary>
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    /// <summary>Optional deep link (e.g. "/Tickets/Details?id=...") opened on click.</summary>
    public string? LinkUrl { get; set; }

    /// <summary>Optional source context (entity class name + id), mirroring InventoryTransaction's ModuleName/ModuleId.</summary>
    public string? ModuleName { get; set; }
    public string? ModuleId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public void MarkRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }
}
