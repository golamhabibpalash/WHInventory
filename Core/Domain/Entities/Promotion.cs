using Domain.Common;

namespace Domain.Entities;

public class Promotion : BaseEntity
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? PricePolicyId { get; set; }
    public PricePolicy? PricePolicy { get; set; }
    public double? PromotionalPrice { get; set; }
    public double? DiscountPercent { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
