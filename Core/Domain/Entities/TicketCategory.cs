using Domain.Common;

namespace Domain.Entities;

public class TicketCategory : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
