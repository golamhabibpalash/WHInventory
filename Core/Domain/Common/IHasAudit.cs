namespace Domain.Common;

public interface IHasAudit
{
    DateTime? CreatedAtUtc { get; set; }
    string? CreatedById { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedById { get; set; }
}
