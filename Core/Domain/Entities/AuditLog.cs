using Domain.Common;

namespace Domain.Entities;

public class AuditLog : IHasTenant
{
    public string Id { get; set; } = null!;
    public string? TenantId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OperationType { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
}
