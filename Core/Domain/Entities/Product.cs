using Domain.Common;

namespace Domain.Entities;

public class Product : BaseEntity
{
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public double? UnitPrice { get; set; }

    /// <summary>
    /// Lowest price this product may be sold at. Null means no floor.
    /// </summary>
    public double? MinSellingPrice { get; set; }

    /// <summary>
    /// Highest price this product may be sold at. Null means no ceiling.
    /// </summary>
    public double? MaxSellingPrice { get; set; }
    public bool? Physical { get; set; } = true;
    public string? UnitMeasureId { get; set; }
    public UnitMeasure? UnitMeasure { get; set; }
    public string? ProductGroupId { get; set; }
    public ProductGroup? ProductGroup { get; set; }
    public string? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public string? ImageName { get; set; }
    public string? Barcode { get; set; }
    public bool? IsWarrantyApplicable { get; set; } = false;
    public int? WarrantyDays { get; set; }
}
