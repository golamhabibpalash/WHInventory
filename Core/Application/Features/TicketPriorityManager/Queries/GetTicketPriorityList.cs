using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketPriorityManager.Queries;

public record GetTicketPriorityListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? ColorHex { get; init; }
    public int Level { get; init; }
    public int? SlaResponseHours { get; init; }
    public int? SlaResolutionHours { get; init; }
    public bool IsActive { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketPriorityListProfile : Profile
{
    public GetTicketPriorityListProfile()
    {
        CreateMap<TicketPriority, GetTicketPriorityListDto>();
    }
}

public class GetTicketPriorityListResult
{
    public List<GetTicketPriorityListDto>? Data { get; init; }
}

public class GetTicketPriorityListRequest : IRequest<GetTicketPriorityListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetTicketPriorityListHandler : IRequestHandler<GetTicketPriorityListRequest, GetTicketPriorityListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetTicketPriorityListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetTicketPriorityListResult> Handle(GetTicketPriorityListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.TicketPriority
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderBy(x => x.Level)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetTicketPriorityListDto>>(entities);

        return new GetTicketPriorityListResult { Data = dtos };
    }
}
