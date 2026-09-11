using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketTagManager.Queries;

public record GetTicketTagListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketTagListProfile : Profile
{
    public GetTicketTagListProfile()
    {
        CreateMap<TicketTag, GetTicketTagListDto>();
    }
}

public class GetTicketTagListResult
{
    public List<GetTicketTagListDto>? Data { get; init; }
}

public class GetTicketTagListRequest : IRequest<GetTicketTagListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetTicketTagListHandler : IRequestHandler<GetTicketTagListRequest, GetTicketTagListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetTicketTagListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetTicketTagListResult> Handle(GetTicketTagListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.TicketTag
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderBy(x => x.Name)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetTicketTagListDto>>(entities);

        return new GetTicketTagListResult { Data = dtos };
    }
}
