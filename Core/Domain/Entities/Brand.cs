using Domain.Common;

namespace Domain.Entities;

public class Brand : BaseEntity
{
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public string? ImageName { get; set; }
    public string? Status { get; set; }
}
