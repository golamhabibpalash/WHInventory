using Application.Common.CQS.Queries;
using Application.Common.Services.CurrentUserManager;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public class GetTicketDashboardResult
{
    public int Total { get; init; }
    public int New { get; init; }
    public int Open { get; init; }
    public int InProgress { get; init; }
    public int Pending { get; init; }
    public int Resolved { get; init; }
    public int Closed { get; init; }
    public int Overdue { get; init; }
    public int Critical { get; init; }
    public int Unassigned { get; init; }
    public int MyAssigned { get; init; }
}

/// <summary>Agent-only — a regular requester's own "My Tickets" page shows its counts inline
/// from the same GetTicketList result it already fetches, rather than a second dashboard.</summary>
public class GetTicketDashboardRequest : IRequest<GetTicketDashboardResult>
{
}

public class GetTicketDashboardValidator : AbstractValidator<GetTicketDashboardRequest>
{
}

public class GetTicketDashboardHandler : IRequestHandler<GetTicketDashboardRequest, GetTicketDashboardResult>
{
    private readonly IQueryContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketDashboardHandler(IQueryContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GetTicketDashboardResult> Handle(GetTicketDashboardRequest request, CancellationToken cancellationToken)
    {
        TicketAccessGuard.EnsureIsAgent(_currentUser.IsInRole(TicketAccessGuard.AgentRole));

        var now = DateTime.UtcNow;
        var query = _context.Ticket.AsNoTracking().Where(x => !x.IsDeleted);

        // A handful of small, independent COUNT queries — simple and easy to read, and cheap at
        // the ticket volumes this iteration targets; a single grouped aggregate query would save
        // round trips if this ever needs to scale further.
        var total = await query.CountAsync(cancellationToken);
        var newCount = await query.CountAsync(x => x.Status == TicketStatus.New, cancellationToken);
        var open = await query.CountAsync(x => x.Status == TicketStatus.Open, cancellationToken);
        var inProgress = await query.CountAsync(x => x.Status == TicketStatus.InProgress, cancellationToken);
        var pending = await query.CountAsync(x => x.Status == TicketStatus.PendingUser || x.Status == TicketStatus.PendingInternal, cancellationToken);
        var resolved = await query.CountAsync(x => x.Status == TicketStatus.Resolved, cancellationToken);
        var closed = await query.CountAsync(x => x.Status == TicketStatus.Closed, cancellationToken);
        var overdue = await query.CountAsync(x => x.SlaResolutionDueAtUtc != null && x.SlaResolutionDueAtUtc < now
            && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled, cancellationToken);
        var unassigned = await query.CountAsync(x => (x.AssignedToId == null || x.AssignedToId == "")
            && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled, cancellationToken);
        var myAssigned = await query.CountAsync(x => x.AssignedToId == _currentUser.UserId
            && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled, cancellationToken);

        var criticalPriorityIds = await _context.TicketPriority.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Level == 0)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var critical = criticalPriorityIds.Count == 0
            ? 0
            : await query.CountAsync(x => x.PriorityId != null && criticalPriorityIds.Contains(x.PriorityId)
                && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled, cancellationToken);

        return new GetTicketDashboardResult
        {
            Total = total,
            New = newCount,
            Open = open,
            InProgress = inProgress,
            Pending = pending,
            Resolved = resolved,
            Closed = closed,
            Overdue = overdue,
            Critical = critical,
            Unassigned = unassigned,
            MyAssigned = myAssigned
        };
    }
}
