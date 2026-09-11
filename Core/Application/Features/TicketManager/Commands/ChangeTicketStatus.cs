using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class ChangeTicketStatusResult
{
    public Ticket? Data { get; set; }
}

public class ChangeTicketStatusRequest : IRequest<ChangeTicketStatusResult>
{
    public string? Id { get; init; }
    public TicketStatus Status { get; init; }
    public string? UpdatedById { get; init; }
}

public class ChangeTicketStatusValidator : AbstractValidator<ChangeTicketStatusRequest>
{
    public ChangeTicketStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ChangeTicketStatusHandler : IRequestHandler<ChangeTicketStatusRequest, ChangeTicketStatusResult>
{
    private readonly ICommandRepository<Ticket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly TicketNotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public ChangeTicketStatusHandler(
        ICommandRepository<Ticket> repository,
        IUnitOfWork unitOfWork,
        TicketHistoryRecorder historyRecorder,
        TicketNotificationService notificationService,
        ICurrentUserService currentUser,
        ISecurityService securityService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _historyRecorder = historyRecorder;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<ChangeTicketStatusResult> Handle(ChangeTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);

        // A requester may only reopen (confirming they're not satisfied) or close (confirming
        // resolution) their own ticket — every other transition is an agent-only action.
        if (!isAgent)
        {
            TicketAccessGuard.EnsureCanAccess(entity, _currentUser.UserId, isAgent: false);
            if (request.Status != TicketStatus.Reopened && request.Status != TicketStatus.Closed)
            {
                throw new Exception("This status change requires the ticket agent role.");
            }
        }

        var oldStatus = entity.Status;
        TicketStatusTransitions.EnsureCanTransition(oldStatus, request.Status);

        entity.Status = request.Status;
        entity.UpdatedById = request.UpdatedById;
        entity.LastActivityAtUtc = DateTime.UtcNow;

        if (request.Status == TicketStatus.Resolved)
        {
            entity.ResolvedAtUtc = DateTime.UtcNow;
        }
        if (request.Status == TicketStatus.Closed)
        {
            entity.ClosedAtUtc = DateTime.UtcNow;
        }
        if (request.Status == TicketStatus.Reopened)
        {
            entity.ResolvedAtUtc = null;
            entity.ClosedAtUtc = null;
        }

        _historyRecorder.Record(entity.Id, "StatusChanged", oldStatus.ToString(), request.Status.ToString(), request.UpdatedById);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        await NotifyAsync(entity, request.Status, cancellationToken);

        return new ChangeTicketStatusResult { Data = entity };
    }

    private async Task NotifyAsync(Ticket entity, TicketStatus newStatus, CancellationToken cancellationToken)
    {
        if (newStatus != TicketStatus.Resolved && newStatus != TicketStatus.Reopened) return;

        var users = await _securityService.GetUserListAsync(cancellationToken);

        if (newStatus == TicketStatus.Resolved)
        {
            // The requester is told their ticket is resolved.
            var requesterEmail = users.FirstOrDefault(u => u.Id == entity.RequesterId)?.Email;
            _ = _notificationService.NotifyTicketResolvedAsync(requesterEmail, entity.TicketNumber ?? string.Empty, entity.Subject ?? string.Empty);
        }
        else
        {
            // The assigned agent is told the requester wasn't satisfied and reopened it.
            var agentEmail = users.FirstOrDefault(u => u.Id == entity.AssignedToId)?.Email;
            _ = _notificationService.NotifyTicketReopenedAsync(agentEmail, entity.TicketNumber ?? string.Empty, entity.Subject ?? string.Empty);
        }
    }
}
