using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SubscriptionRequestManager.Queries;

public record GetMySubscriptionRequestListDto
{
    public string? Id { get; init; }
    public string? RequestedPlanId { get; init; }
    public string? RequestedPlanName { get; init; }
    public string? RequestedPlanCode { get; init; }
    public SubscriptionRequestStatus Status { get; init; }
    public string? RequestNote { get; init; }
    public string? DecisionNote { get; init; }
    public DateTime? DecisionAtUtc { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetMySubscriptionRequestListResult
{
    public List<GetMySubscriptionRequestListDto>? Data { get; init; }
}

/// <summary>The caller tenant's own plan requests, newest first (the ambient tenant filter scopes it).</summary>
public class GetMySubscriptionRequestListRequest : IRequest<GetMySubscriptionRequestListResult>
{
}

public class GetMySubscriptionRequestListHandler : IRequestHandler<GetMySubscriptionRequestListRequest, GetMySubscriptionRequestListResult>
{
    private readonly IQueryContext _context;

    public GetMySubscriptionRequestListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetMySubscriptionRequestListResult> Handle(GetMySubscriptionRequestListRequest request, CancellationToken cancellationToken)
    {
        var data = await _context.SubscriptionChangeRequest
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new GetMySubscriptionRequestListDto
            {
                Id = x.Id,
                RequestedPlanId = x.RequestedPlanId,
                RequestedPlanName = x.RequestedPlanName,
                RequestedPlanCode = x.RequestedPlanCode,
                Status = x.Status,
                RequestNote = x.RequestNote,
                DecisionNote = x.DecisionNote,
                DecisionAtUtc = x.DecisionAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new GetMySubscriptionRequestListResult { Data = data };
    }
}
