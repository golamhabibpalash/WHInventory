using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class AddTicketCommentResult
{
    public TicketComment? Data { get; set; }
}

public class AddTicketCommentRequest : IRequest<AddTicketCommentResult>
{
    public string? TicketId { get; init; }
    public string? Message { get; init; }

    /// <summary>Requested value is honored only if the caller is an agent — see handler.</summary>
    public bool IsInternal { get; init; }
    public string? CreatedById { get; init; }
}

public class AddTicketCommentValidator : AbstractValidator<AddTicketCommentRequest>
{
    public AddTicketCommentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class AddTicketCommentHandler : IRequestHandler<AddTicketCommentRequest, AddTicketCommentResult>
{
    private readonly ICommandRepository<TicketComment> _commentRepository;
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly TicketNotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public AddTicketCommentHandler(
        ICommandRepository<TicketComment> commentRepository,
        ICommandRepository<Ticket> ticketRepository,
        IUnitOfWork unitOfWork,
        TicketHistoryRecorder historyRecorder,
        TicketNotificationService notificationService,
        ICurrentUserService currentUser,
        ISecurityService securityService
        )
    {
        _commentRepository = commentRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _historyRecorder = historyRecorder;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<AddTicketCommentResult> Handle(AddTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken);
        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.TicketId}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);

        // Never trust a client-supplied IsInternal: only an agent may create one, regardless of
        // what the request body says.
        var isInternal = isAgent && request.IsInternal;

        var comment = new TicketComment
        {
            TicketId = request.TicketId,
            AuthorId = request.CreatedById,
            Message = request.Message,
            IsInternal = isInternal,
            CreatedById = request.CreatedById
        };

        await _commentRepository.CreateAsync(comment, cancellationToken);

        _historyRecorder.Record(ticket.Id, isInternal ? "InternalNoteAdded" : "CommentAdded", null, null, request.CreatedById);

        ticket.LastActivityAtUtc = DateTime.UtcNow;
        // A public reply from Pending User moves the ticket back into active work automatically —
        // the requester answered, so it's no longer waiting on them.
        if (!isAgent && !isInternal && ticket.Status == Domain.Enums.TicketStatus.PendingUser)
        {
            ticket.Status = Domain.Enums.TicketStatus.InProgress;
            _historyRecorder.Record(ticket.Id, "StatusChanged", Domain.Enums.TicketStatus.PendingUser.ToString(), Domain.Enums.TicketStatus.InProgress.ToString(), request.CreatedById);
        }
        _ticketRepository.Update(ticket);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!isInternal)
        {
            await NotifyAsync(ticket, isAgent, cancellationToken);
        }

        return new AddTicketCommentResult { Data = comment };
    }

    private async Task NotifyAsync(Ticket ticket, bool authorIsAgent, CancellationToken cancellationToken)
    {
        var users = await _securityService.GetUserListAsync(cancellationToken);

        // A requester's reply notifies the assigned agent; an agent's public reply notifies the requester.
        var targetId = authorIsAgent ? ticket.RequesterId : ticket.AssignedToId;
        var targetEmail = users.FirstOrDefault(u => u.Id == targetId)?.Email;

        _ = _notificationService.NotifyTicketRepliedAsync(targetEmail, ticket.TicketNumber ?? string.Empty, ticket.Subject ?? string.Empty);
    }
}
