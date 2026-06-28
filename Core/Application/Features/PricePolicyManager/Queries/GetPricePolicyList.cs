using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PricePolicyManager.Queries;

public record GetPricePolicyListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPricePolicyListProfile : Profile
{
    public GetPricePolicyListProfile()
    {
        CreateMap<PricePolicy, GetPricePolicyListDto>();
    }
}

public class GetPricePolicyListResult
{
    public List<GetPricePolicyListDto>? Data { get; init; }
}

public class GetPricePolicyListRequest : IRequest<GetPricePolicyListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPricePolicyListHandler : IRequestHandler<GetPricePolicyListRequest, GetPricePolicyListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPricePolicyListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPricePolicyListResult> Handle(GetPricePolicyListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PricePolicy
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetPricePolicyListDto>>(entities);

        return new GetPricePolicyListResult { Data = dtos };
    }
}
