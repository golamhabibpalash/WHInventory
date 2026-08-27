using System.ComponentModel;

namespace Domain.Enums;

public enum PaymentDirection
{
    /// <summary>Money coming in — a customer paying a Sales Order.</summary>
    [Description("Received")]
    Received = 0,

    /// <summary>Money going out — paying a Vendor against a Purchase Order.</summary>
    [Description("Paid")]
    Paid = 1
}
