using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class ProductPrice : BaseEntity
{
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? PricePolicyId { get; set; }
    public PricePolicy? PricePolicy { get; set; }
    public PricingCalculationMethod CalculationMethod { get; set; } = PricingCalculationMethod.FixedPrice;
    public double? FixedPrice { get; set; }
    public double? MarkupPercent { get; set; }
    public double? MarkupAmount { get; set; }
    public double? MarginPercent { get; set; }
    public double? FormulaMultiplier { get; set; }
    public double? MinimumSellingPrice { get; set; }
    public double? MaximumDiscountPercent { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public ICollection<QuantityBreak> QuantityBreakList { get; set; } = new List<QuantityBreak>();
}
