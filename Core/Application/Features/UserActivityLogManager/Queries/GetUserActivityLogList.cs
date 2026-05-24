using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.UserActivityLogManager.Queries;

public record GetUserActivityLogListDto
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? ActivityType { get; init; }
    public string? Description { get; init; }
    public string? PageUrl { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetUserActivityLogListProfile : Profile
{
    public GetUserActivityLogListProfile()
    {
        CreateMap<UserActivityLog, GetUserActivityLogListDto>();
    }
}

public class GetUserActivityLogListResult
{
    public List<GetUserActivityLogListDto>? Data { get; init; }
}

public class GetUserActivityLogListRequest : IRequest<GetUserActivityLogListResult>
{
    public string? UserId { get; init; }
    public string? ActivityType { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class GetUserActivityLogListHandler : IRequestHandler<GetUserActivityLogListRequest, GetUserActivityLogListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetUserActivityLogListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetUserActivityLogListResult> Handle(GetUserActivityLogListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.UserActivityLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(x => x.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.ActivityType))
            query = query.Where(x => x.ActivityType == request.ActivityType);

        if (request.FromDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= request.ToDate.Value);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5000)
            .ToListAsync(cancellationToken);

        return new GetUserActivityLogListResult { Data = _mapper.Map<List<GetUserActivityLogListDto>>(entities) };
    }
}
