using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AuditLogManager.Queries;

public record GetAuditLogListDto
{
    public string? Id { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? OperationType { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? UserId { get; init; }
    public string? IpAddress { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetAuditLogListProfile : Profile
{
    public GetAuditLogListProfile()
    {
        CreateMap<AuditLog, GetAuditLogListDto>();
    }
}

public class GetAuditLogListResult
{
    public List<GetAuditLogListDto>? Data { get; init; }
}

public class GetAuditLogListRequest : IRequest<GetAuditLogListResult>
{
    public string? EntityType { get; init; }
    public string? UserId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetAuditLogListHandler : IRequestHandler<GetAuditLogListRequest, GetAuditLogListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetAuditLogListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetAuditLogListResult> Handle(GetAuditLogListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(x => x.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(x => x.UserId == request.UserId);

        if (request.FromDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= request.ToDate.Value);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5000)
            .ToListAsync(cancellationToken);

        return new GetAuditLogListResult { Data = _mapper.Map<List<GetAuditLogListDto>>(entities) };
    }
}
