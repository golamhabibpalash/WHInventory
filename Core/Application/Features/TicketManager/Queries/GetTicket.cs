using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public record GetTicketCommentDto
{
    public string? Id { get; init; }
    public string? AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public string? Message { get; init; }
    public bool IsInternal { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketResult
{
    public string? Id { get; init; }
    public string? TicketNumber { get; init; }
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? PriorityId { get; init; }
    public string? PriorityName { get; init; }
    public string? PriorityColorHex { get; init; }
    public TicketStatus Status { get; init; }
    public TicketSource Source { get; init; }
    public string? RequesterId { get; init; }
    public string? RequesterName { get; init; }
    public string? RequesterEmail { get; init; }
    public string? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public DateTime? SlaResponseDueAtUtc { get; init; }
    public DateTime? SlaResolutionDueAtUtc { get; init; }
    public string? ReferenceModule { get; init; }
    public string? ReferenceEntityType { get; init; }
    public string? ReferenceEntityId { get; init; }
    public bool CurrentUserIsAgent { get; init; }
    public bool CurrentUserCanEdit { get; init; }
    public List<GetTicketCommentDto>? Comments { get; init; }
    public List<string>? TagIds { get; init; }
}

public class GetTicketRequest : IRequest<GetTicketResult>
{
    public string? Id { get; init; }
}

public class GetTicketValidator : AbstractValidator<GetTicketRequest>
{
    public GetTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetTicketHandler : IRequestHandler<GetTicketRequest, GetTicketResult>
{
    private readonly IQueryContext _context;
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public GetTicketHandler(
        IQueryContext context,
        ICommandRepository<Ticket> ticketRepository,
        ICurrentUserService currentUser,
        ISecurityService securityService
        )
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<GetTicketResult> Handle(GetTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);

        var category = ticket.CategoryId != null
            ? await _context.TicketCategory.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticket.CategoryId, cancellationToken)
            : null;
        var priority = ticket.PriorityId != null
            ? await _context.TicketPriority.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticket.PriorityId, cancellationToken)
            : null;

        var users = await _securityService.GetUserListAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());
        var requester = users.FirstOrDefault(u => u.Id == ticket.RequesterId);

        var commentsQuery = _context.TicketComment.AsNoTracking()
            .Where(x => x.TicketId == ticket.Id && !x.IsDeleted);
        if (!isAgent)
        {
            // A requester never receives internal notes, at the query level — not just hidden by
            // the UI — so there is no client-side toggle or DOM inspection that could expose one.
            commentsQuery = commentsQuery.Where(x => !x.IsInternal);
        }

        var comments = await commentsQuery
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.AuthorId, x.Message, x.IsInternal, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var tagIds = await _context.TicketTagMap.AsNoTracking()
            .Where(x => x.TicketId == ticket.Id && !x.IsDeleted)
            .Select(x => x.TicketTagId!)
            .ToListAsync(cancellationToken);

        return new GetTicketResult
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = category?.Name,
            PriorityId = ticket.PriorityId,
            PriorityName = priority?.Name,
            PriorityColorHex = priority?.ColorHex,
            Status = ticket.Status,
            Source = ticket.Source,
            RequesterId = ticket.RequesterId,
            RequesterName = requester != null ? $"{requester.FirstName} {requester.LastName}".Trim() : null,
            RequesterEmail = requester?.Email,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedToId != null && userNames.TryGetValue(ticket.AssignedToId, out var an) ? an : null,
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
            ResolvedAtUtc = ticket.ResolvedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            SlaResponseDueAtUtc = ticket.SlaResponseDueAtUtc,
            SlaResolutionDueAtUtc = ticket.SlaResolutionDueAtUtc,
            ReferenceModule = ticket.ReferenceModule,
            ReferenceEntityType = ticket.ReferenceEntityType,
            ReferenceEntityId = ticket.ReferenceEntityId,
            CurrentUserIsAgent = isAgent,
            CurrentUserCanEdit = isAgent || (ticket.RequesterId == _currentUser.UserId && ticket.Status == TicketStatus.New),
            Comments = comments.Select(c => new GetTicketCommentDto
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                AuthorName = c.AuthorId != null && userNames.TryGetValue(c.AuthorId, out var authorName) ? authorName : "Unknown",
                Message = c.Message,
                IsInternal = c.IsInternal,
                CreatedAtUtc = c.CreatedAtUtc
            }).ToList(),
            TagIds = tagIds
        };
    }
}
