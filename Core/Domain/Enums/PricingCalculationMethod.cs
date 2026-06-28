using System.ComponentModel;

namespace Domain.Enums;

public enum PricingCalculationMethod
{
    [Description("Fixed Price")]
    FixedPrice = 0,
    [Description("Cost + Markup %")]
    CostPlusMarkupPercent = 1,
    [Description("Cost + Markup Amount")]
    CostPlusMarkupAmount = 2,
    [Description("Gross Margin %")]
    GrossMargin = 3,
    [Description("Formula Based")]
    FormulaBased = 4,
}
