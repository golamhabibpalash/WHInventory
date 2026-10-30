using Application.Common.Services.SubscriptionManager;
using MediatR;

namespace Application.Features.TenantSubscriptionManager.Queries;

public class GetMySubscriptionResult
{
    /// <summary>The caller's current subscription, or null if the tenant has none.</summary>
    public TenantSubscriptionDto? Subscription { get; init; }

    public SubscriptionUsageDto? Usage { get; init; }

    /// <summary>True when the workspace is currently read-only because the subscription lapsed.</summary>
    public bool IsReadOnly { get; init; }
}

public class GetMySubscriptionRequest : IRequest<GetMySubscriptionResult>
{
}

public class GetMySubscriptionHandler : IRequestHandler<GetMySubscriptionRequest, GetMySubscriptionResult>
{
    private readonly ISubscriptionGuard _guard;
    private readonly ISubscriptionQuotaService _quotaService;

    public GetMySubscriptionHandler(ISubscriptionGuard guard, ISubscriptionQuotaService quotaService)
    {
        _guard = guard;
        _quotaService = quotaService;
    }

    public async Task<GetMySubscriptionResult> Handle(GetMySubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _guard.GetCurrentAsync(cancellationToken);
        var usage = await _quotaService.GetUsageAsync(cancellationToken);

        return new GetMySubscriptionResult
        {
            Subscription = subscription,
            Usage = usage,
            IsReadOnly = subscription != null && !subscription.IsEntitled
        };
    }
}
