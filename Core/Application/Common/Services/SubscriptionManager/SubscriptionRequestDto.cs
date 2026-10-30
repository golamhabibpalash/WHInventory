using Domain.Enums;

namespace Application.Common.Services.SubscriptionManager;

/// <summary>Flat view of a tenant's plan-change request, shared by the request service and read models.</summary>
public class SubscriptionRequestDto
{
    public string? Id { get; init; }
    public string? TenantId { get; init; }
    public string? TenantName { get; init; }
    public string? TenantSlug { get; init; }
    public string? RequestedPlanId { get; init; }
    public string? RequestedPlanName { get; init; }
    public string? RequestedPlanCode { get; init; }
    public string? CurrentPlanCode { get; init; }
    public SubscriptionRequestStatus Status { get; init; }
    public string? RequestNote { get; init; }
    public string? DecisionById { get; init; }
    public DateTime? DecisionAtUtc { get; init; }
    public string? DecisionNote { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}
