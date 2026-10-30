using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SubscriptionPlanManager.Queries;

public record GetSubscriptionPlanListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public double Price { get; init; }
    public string? Currency { get; init; }
    public BillingInterval BillingInterval { get; init; }
    public int TrialDays { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxWarehouses { get; init; }
    public int? MaxProducts { get; init; }
    public bool IsActive { get; init; }
    public bool IsPublic { get; init; }
    public int SortOrder { get; init; }
    public string? StripeProductId { get; init; }
    public string? StripePriceId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSubscriptionPlanListProfile : Profile
{
    public GetSubscriptionPlanListProfile()
    {
        CreateMap<SubscriptionPlan, GetSubscriptionPlanListDto>();
    }
}

public class GetSubscriptionPlanListResult
{
    public List<GetSubscriptionPlanListDto>? Data { get; init; }
}

public class GetSubscriptionPlanListRequest : IRequest<GetSubscriptionPlanListResult>
{
    public bool IsDeleted { get; init; } = false;

    /// <summary>When true only plans available for new subscriptions are returned.</summary>
    public bool ActiveOnly { get; init; } = false;

    /// <summary>When true only plans flagged for the public pricing surface are returned.</summary>
    public bool PublicOnly { get; init; } = false;
}

public class GetSubscriptionPlanListHandler : IRequestHandler<GetSubscriptionPlanListRequest, GetSubscriptionPlanListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSubscriptionPlanListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSubscriptionPlanListResult> Handle(GetSubscriptionPlanListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SubscriptionPlan
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (request.PublicOnly)
        {
            query = query.Where(x => x.IsPublic);
        }

        var entities = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .ToListAsync(cancellationToken);

        return new GetSubscriptionPlanListResult
        {
            Data = _mapper.Map<List<GetSubscriptionPlanListDto>>(entities)
        };
    }
}
