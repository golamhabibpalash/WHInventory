using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NotificationManager.Queries;

public record GetNotificationListDto
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? Severity { get; init; }
    public string? LinkUrl { get; init; }
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetNotificationListProfile : Profile
{
    public GetNotificationListProfile()
    {
        CreateMap<Notification, GetNotificationListDto>()
            .ForMember(d => d.Severity, o => o.MapFrom(s => s.Severity.ToString()));
    }
}

public class GetNotificationListResult
{
    public List<GetNotificationListDto>? Data { get; init; }
    public int UnreadCount { get; init; }
}

public class GetNotificationListRequest : IRequest<GetNotificationListResult>
{
    public string? UserId { get; init; }
    public bool OnlyUnread { get; init; }
    public int Take { get; init; } = 50;
}

public class GetNotificationListValidator : AbstractValidator<GetNotificationListRequest>
{
    public GetNotificationListValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

public class GetNotificationListHandler : IRequestHandler<GetNotificationListRequest, GetNotificationListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetNotificationListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetNotificationListResult> Handle(GetNotificationListRequest request, CancellationToken cancellationToken)
    {
        var baseQuery = _context
            .Notification
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.UserId == request.UserId);

        var unreadCount = await baseQuery.CountAsync(x => !x.IsRead, cancellationToken);

        var filtered = request.OnlyUnread ? baseQuery.Where(x => !x.IsRead) : baseQuery;

        // Unread first, then newest — the bell dropdown reads naturally without sorting client-side.
        var entities = await filtered
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(request.Take <= 0 ? 50 : Math.Min(request.Take, 200))
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetNotificationListDto>>(entities);

        return new GetNotificationListResult
        {
            Data = dtos,
            UnreadCount = unreadCount
        };
    }
}
