namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Thrown by the subscription enforcement behaviour when a tenant whose subscription has lapsed
/// attempts a mutating operation. Surfaced to the client as HTTP 402 Payment Required.
/// </summary>
public class SubscriptionInactiveException : Exception
{
    public SubscriptionInactiveException(string message) : base(message)
    {
    }
}
