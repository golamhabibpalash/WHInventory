using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BrandManager.Queries;

public record GetBrandListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Number { get; init; }
    public string? Description { get; init; }
    public string? ImageName { get; init; }
    public string? Status { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBrandListProfile : Profile
{
    public GetBrandListProfile()
    {
        CreateMap<Brand, GetBrandListDto>();
    }
}

public class GetBrandListResult
{
    public List<GetBrandListDto>? Data { get; init; }
}

public class GetBrandListRequest : IRequest<GetBrandListResult>
{
    public bool IsDeleted { get; init; } = false;
    public bool? IsActive { get; init; }
}


public class GetBrandListHandler : IRequestHandler<GetBrandListRequest, GetBrandListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBrandListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBrandListResult> Handle(GetBrandListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Brand
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        if (request.IsActive.HasValue && request.IsActive.Value)
        {
            query = query.Where(x => x.Status == "Active");
        }

        var entities = await query.Take(2000).ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBrandListDto>>(entities);

        return new GetBrandListResult
        {
            Data = dtos
        };
    }


}
