using System.ComponentModel;

namespace Domain.Enums;

/// <summary>How often a subscription renews and is charged.</summary>
public enum BillingInterval
{
    [Description("Monthly")]
    Monthly = 0,

    [Description("Yearly")]
    Yearly = 1
}
