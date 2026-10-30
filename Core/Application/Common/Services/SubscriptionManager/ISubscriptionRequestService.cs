namespace Application.Common.Services.SubscriptionManager;

/// <summary>
/// Platform-side, cross-tenant handling of plan-change requests. Like the subscription admin
/// service it elevates to root scope internally so a platform administrator can see and decide any
/// tenant's request. Approving a request applies the requested plan to the tenant's subscription.
/// </summary>
public interface ISubscriptionRequestService
{
    Task<List<SubscriptionRequestDto>> GetAllAsync(bool pendingOnly, CancellationToken cancellationToken = default);

    Task<SubscriptionRequestDto> ApproveAsync(string requestId, string? decisionById, string? decisionNote, CancellationToken cancellationToken = default);

    Task<SubscriptionRequestDto> RejectAsync(string requestId, string? decisionById, string? decisionNote, CancellationToken cancellationToken = default);
}
