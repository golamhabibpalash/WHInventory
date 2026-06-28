using Domain.Common;

namespace Domain.Entities;

public class QuantityBreak : BaseEntity
{
    public string? ProductPriceId { get; set; }
    public ProductPrice? ProductPrice { get; set; }
    public double MinQuantity { get; set; } = 1;
    public double? MaxQuantity { get; set; }
    public double Price { get; set; } = 0;
}
