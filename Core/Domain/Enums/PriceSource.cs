using System.ComponentModel;

namespace Domain.Enums;

public enum PriceSource
{
    [Description("Promotional Price")]
    Promotional = 0,
    [Description("Customer Price Group")]
    CustomerPriceGroup = 1,
    [Description("Quantity Break")]
    QuantityBreak = 2,
    [Description("Product Price Policy")]
    ProductPricePolicy = 3,
    [Description("Default Product Price")]
    DefaultProductPrice = 4,
}
