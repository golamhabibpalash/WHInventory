using Domain.Common;

namespace Domain.Entities;

public class Company : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
    public string? LogoName { get; set; }
    public bool AllowNegativeStock { get; set; } = false;

    /// <summary>
    /// When true, sales prices outside a product's Min/Max selling price band are permitted.
    /// Defaults to false so the band is enforced.
    /// </summary>
    public bool AllowPriceOutsideBand { get; set; } = false;
}
