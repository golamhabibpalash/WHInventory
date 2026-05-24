using Domain.Common;

namespace Domain.Entities;

public class ProductGroup : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public ProductGroup? Parent { get; set; }
    public ICollection<ProductGroup>? Children { get; set; }
}
