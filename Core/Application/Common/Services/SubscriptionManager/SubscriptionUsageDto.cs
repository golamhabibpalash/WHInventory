namespace Application.Common.Services.SubscriptionManager;

/// <summary>Current consumption of the quota-limited resources for a single tenant.</summary>
public class SubscriptionUsageDto
{
    public int Users { get; init; }
    public int Warehouses { get; init; }
    public int Products { get; init; }
}
