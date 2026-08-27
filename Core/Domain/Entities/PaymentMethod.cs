using Domain.Common;

namespace Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Seeded methods every install needs. They may be deactivated but must not be deleted,
    /// mirroring how system warehouses are protected.
    /// </summary>
    public bool SystemMethod { get; set; }

    public bool IsActive { get; set; } = true;
}
