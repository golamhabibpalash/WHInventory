using Domain.Common;

namespace Domain.Entities;

public class PriceHistory : BaseEntity
{
    public string? ProductPriceId { get; set; }
    public ProductPrice? ProductPrice { get; set; }
    public double? PreviousPrice { get; set; }
    public double? NewPrice { get; set; }
    public string? ChangedById { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public string? ChangeReason { get; set; }
}
