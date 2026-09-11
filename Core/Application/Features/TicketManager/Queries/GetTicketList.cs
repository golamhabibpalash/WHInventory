using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public record GetTicketListDto
{
    public string? Id { get; init; }
    public string? TicketNumber { get; init; }
    public string? Subject { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? PriorityId { get; init; }
    public string? PriorityName { get; init; }
    public string? PriorityColorHex { get; init; }
    public TicketStatus Status { get; init; }
    public TicketSource Source { get; init; }
    public string? RequesterId { get; init; }
    public string? RequesterName { get; init; }
    public string? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? LastActivityAtUtc { get; init; }
    public DateTime? SlaResolutionDueAtUtc { get; init; }
    public bool IsOverdue { get; init; }
}

public class GetTicketListResult
{
    public List<GetTicketListDto>? Data { get; init; }
    public int TotalCount { get; init; }
}

/// <summary>
/// Serves both "My Tickets" and the agent's "All Tickets": a non-agent caller always gets
/// RequesterId forced to their own id server-side (see handler), regardless of what's sent here
/// — one query, one authorization rule, instead of two endpoints that could drift apart.
/// Coarse filters (status/priority/category/assignment/date range/tag/search) run in SQL before
/// the Take, matching this app's existing "server-side coarse filter, client-side sort/quick
/// search within the fetched page" grid convention (see e.g. AuditLogList's date-range filter).
/// </summary>
public class GetTicketListRequest : IRequest<GetTicketListResult>
{
    public TicketStatus? Status { get; init; }
    public string? PriorityId { get; init; }
    public string? CategoryId { get; init; }
    public string? AssignedToId { get; init; }
    public string? RequesterId { get; init; }
    public string? TagId { get; init; }
    public string? Search { get; init; }
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
    public bool UnassignedOnly { get; init; }
    public bool OverdueOnly { get; init; }
    public bool ExcludeClosed { get; init; }
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 2000;
}

public class GetTicketListValidator : AbstractValidator<GetTicketListRequest>
{
    public GetTicketListValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 2000);
    }
}

public class GetTicketListHandler : IRequestHandler<GetTicketListRequest, GetTicketListResult>
{
    private readonly IQueryContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public GetTicketListHandler(IQueryContext context, ICurrentUserService currentUser, ISecurityService securityService)
    {
        _context = context;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<GetTicketListResult> Handle(GetTicketListRequest request, CancellationToken cancellationToken)
    {
        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);

        var query = _context.Ticket.AsNoTracking().ApplyIsDeletedFilter(false).AsQueryable();

        if (!isAgent)
        {
            // Never trust the client's RequesterId — a non-agent only ever sees their own tickets.
            query = query.Where(x => x.RequesterId == _currentUser.UserId);
        }
        else if (!string.IsNullOrEmpty(request.RequesterId))
        {
            query = query.Where(x => x.RequesterId == request.RequesterId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }
        if (request.ExcludeClosed)
        {
            query = query.Where(x => x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled);
        }
        if (!string.IsNullOrEmpty(request.PriorityId))
        {
            query = query.Where(x => x.PriorityId == request.PriorityId);
        }
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            query = query.Where(x => x.CategoryId == request.CategoryId);
        }
        if (!string.IsNullOrEmpty(request.AssignedToId))
        {
            query = query.Where(x => x.AssignedToId == request.AssignedToId);
        }
        if (request.UnassignedOnly)
        {
            query = query.Where(x => x.AssignedToId == null || x.AssignedToId == string.Empty);
        }
        if (request.OverdueOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.SlaResolutionDueAtUtc != null && x.SlaResolutionDueAtUtc < now
                && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled);
        }
        if (request.FromDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= request.FromDateUtc.Value);
        }
        if (request.ToDateUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= request.ToDateUtc.Value);
        }
        if (!string.IsNullOrEmpty(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                (x.TicketNumber != null && x.TicketNumber.Contains(term)) ||
                (x.Subject != null && x.Subject.Contains(term)) ||
                (x.Description != null && x.Description.Contains(term)));
        }
        if (!string.IsNullOrEmpty(request.TagId))
        {
            query = query.Where(x => _context.TicketTagMap.Any(m => m.TicketId == x.Id && m.TicketTagId == request.TagId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderByDescending(x => x.LastActivityAtUtc ?? x.CreatedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,
                x.TicketNumber,
                x.Subject,
                x.CategoryId,
                x.PriorityId,
                x.Status,
                x.Source,
                x.RequesterId,
                x.AssignedToId,
                x.CreatedAtUtc,
                x.LastActivityAtUtc,
                x.SlaResolutionDueAtUtc
            })
            .ToListAsync(cancellationToken);

        var categoryNames = await _context.TicketCategory.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var priorities = await _context.TicketPriority.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => new { x.Name, x.ColorHex }, cancellationToken);
        var users = await _securityService.GetUserListAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());

        var now2 = DateTime.UtcNow;
        var dtos = page.Select(x => new GetTicketListDto
        {
            Id = x.Id,
            TicketNumber = x.TicketNumber,
            Subject = x.Subject,
            CategoryId = x.CategoryId,
            CategoryName = x.CategoryId != null && categoryNames.TryGetValue(x.CategoryId, out var cn) ? cn : null,
            PriorityId = x.PriorityId,
            PriorityName = x.PriorityId != null && priorities.TryGetValue(x.PriorityId, out var p) ? p.Name : null,
            PriorityColorHex = x.PriorityId != null && priorities.TryGetValue(x.PriorityId, out var p2) ? p2.ColorHex : null,
            Status = x.Status,
            Source = x.Source,
            RequesterId = x.RequesterId,
            RequesterName = x.RequesterId != null && userNames.TryGetValue(x.RequesterId, out var rn) ? rn : null,
            AssignedToId = x.AssignedToId,
            AssignedToName = x.AssignedToId != null && userNames.TryGetValue(x.AssignedToId, out var an) ? an : null,
            CreatedAtUtc = x.CreatedAtUtc,
            LastActivityAtUtc = x.LastActivityAtUtc,
            SlaResolutionDueAtUtc = x.SlaResolutionDueAtUtc,
            IsOverdue = x.SlaResolutionDueAtUtc.HasValue && x.SlaResolutionDueAtUtc.Value < now2
                && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled
        }).ToList();

        return new GetTicketListResult { Data = dtos, TotalCount = totalCount };
    }
}
