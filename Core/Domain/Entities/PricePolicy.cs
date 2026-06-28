using Domain.Common;

namespace Domain.Entities;

public class PricePolicy : BaseEntity
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
