using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class AssignTicketResult
{
    public Ticket? Data { get; set; }
}

public class AssignTicketRequest : IRequest<AssignTicketResult>
{
    public string? Id { get; init; }

    /// <summary>Null/empty unassigns.</summary>
    public string? AssignedToId { get; init; }
    public string? UpdatedById { get; init; }
}

public class AssignTicketValidator : AbstractValidator<AssignTicketRequest>
{
    public AssignTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class AssignTicketHandler : IRequestHandler<AssignTicketRequest, AssignTicketResult>
{
    private readonly ICommandRepository<Ticket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly TicketNotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public AssignTicketHandler(
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

    public async Task<AssignTicketResult> Handle(AssignTicketRequest request, CancellationToken cancellationToken)
    {
        TicketAccessGuard.EnsureIsAgent(_currentUser.IsInRole(TicketAccessGuard.AgentRole));

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var users = await _securityService.GetUserListAsync(cancellationToken);
        var oldAssignee = users.FirstOrDefault(u => u.Id == entity.AssignedToId);
        var newAssignee = string.IsNullOrEmpty(request.AssignedToId)
            ? null
            : users.FirstOrDefault(u => u.Id == request.AssignedToId);

        if (!string.IsNullOrEmpty(request.AssignedToId) && newAssignee == null)
        {
            throw new Exception("Selected assignee is not a valid user for this tenant.");
        }

        var oldLabel = oldAssignee != null ? $"{oldAssignee.FirstName} {oldAssignee.LastName}".Trim() : "Unassigned";
        var newLabel = newAssignee != null ? $"{newAssignee.FirstName} {newAssignee.LastName}".Trim() : "Unassigned";

        entity.AssignedToId = request.AssignedToId;
        entity.UpdatedById = request.UpdatedById;
        entity.LastActivityAtUtc = DateTime.UtcNow;

        _historyRecorder.Record(entity.Id, "Assigned", oldLabel, newLabel, request.UpdatedById);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        if (newAssignee != null)
        {
            _ = _notificationService.NotifyTicketAssignedAsync(newAssignee.Email, entity.TicketNumber ?? string.Empty, entity.Subject ?? string.Empty);
        }

        return new AssignTicketResult { Data = entity };
    }
}
