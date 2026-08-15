namespace Domain.Common;

public interface IHasTenant
{
    string? TenantId { get; set; }
}
