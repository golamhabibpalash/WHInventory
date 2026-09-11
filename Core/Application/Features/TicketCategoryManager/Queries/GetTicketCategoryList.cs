using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketCategoryManager.Queries;

public record GetTicketCategoryListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketCategoryListProfile : Profile
{
    public GetTicketCategoryListProfile()
    {
        CreateMap<TicketCategory, GetTicketCategoryListDto>();
    }
}

public class GetTicketCategoryListResult
{
    public List<GetTicketCategoryListDto>? Data { get; init; }
}

public class GetTicketCategoryListRequest : IRequest<GetTicketCategoryListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetTicketCategoryListHandler : IRequestHandler<GetTicketCategoryListRequest, GetTicketCategoryListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetTicketCategoryListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetTicketCategoryListResult> Handle(GetTicketCategoryListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.TicketCategory
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetTicketCategoryListDto>>(entities);

        return new GetTicketCategoryListResult { Data = dtos };
    }
}
