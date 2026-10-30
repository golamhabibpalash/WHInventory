using Application.Common.Services.SubscriptionManager;
using MediatR;

namespace Application.Features.SubscriptionRequestManager.Queries;

public class GetSubscriptionRequestListResult
{
    public List<SubscriptionRequestDto>? Data { get; init; }
}

/// <summary>Platform-only: plan requests across every tenant (served by the request service, which
/// elevates to root scope).</summary>
public class GetSubscriptionRequestListRequest : IRequest<GetSubscriptionRequestListResult>
{
    public bool PendingOnly { get; init; } = false;
}

public class GetSubscriptionRequestListHandler : IRequestHandler<GetSubscriptionRequestListRequest, GetSubscriptionRequestListResult>
{
    private readonly ISubscriptionRequestService _service;

    public GetSubscriptionRequestListHandler(ISubscriptionRequestService service)
    {
        _service = service;
    }

    public async Task<GetSubscriptionRequestListResult> Handle(GetSubscriptionRequestListRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(request.PendingOnly, cancellationToken);
        return new GetSubscriptionRequestListResult { Data = data };
    }
}
