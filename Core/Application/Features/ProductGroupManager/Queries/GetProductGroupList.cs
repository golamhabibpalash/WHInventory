using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductGroupManager.Queries;

public record GetProductGroupListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProductGroupListProfile : Profile
{
    public GetProductGroupListProfile()
    {
        CreateMap<ProductGroup, GetProductGroupListDto>()
            .ForMember(d => d.ParentName, o => o.MapFrom(s => s.Parent != null ? s.Parent.Name : null));
    }
}

public class GetProductGroupListResult
{
    public List<GetProductGroupListDto>? Data { get; init; }
}

public class GetProductGroupListRequest : IRequest<GetProductGroupListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetProductGroupListHandler : IRequestHandler<GetProductGroupListRequest, GetProductGroupListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProductGroupListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProductGroupListResult> Handle(GetProductGroupListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ProductGroup
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Include(x => x.Parent)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProductGroupListDto>>(entities);

        return new GetProductGroupListResult
        {
            Data = dtos
        };
    }


}



