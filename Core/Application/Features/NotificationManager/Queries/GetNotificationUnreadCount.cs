using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NotificationManager.Queries;

public class GetNotificationUnreadCountResult
{
    public int UnreadCount { get; init; }
}

public class GetNotificationUnreadCountRequest : IRequest<GetNotificationUnreadCountResult>
{
    public string? UserId { get; init; }
}

public class GetNotificationUnreadCountValidator : AbstractValidator<GetNotificationUnreadCountRequest>
{
    public GetNotificationUnreadCountValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetNotificationUnreadCountHandler : IRequestHandler<GetNotificationUnreadCountRequest, GetNotificationUnreadCountResult>
{
    private readonly IQueryContext _context;

    public GetNotificationUnreadCountHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetNotificationUnreadCountResult> Handle(GetNotificationUnreadCountRequest request, CancellationToken cancellationToken)
    {
        var count = await _context
            .Notification
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .CountAsync(x => x.UserId == request.UserId && !x.IsRead, cancellationToken);

        return new GetNotificationUnreadCountResult
        {
            UnreadCount = count
        };
    }
}
