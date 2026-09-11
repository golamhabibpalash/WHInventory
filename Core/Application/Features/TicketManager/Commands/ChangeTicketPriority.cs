using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Commands;

public class ChangeTicketPriorityResult
{
    public Ticket? Data { get; set; }
}

public class ChangeTicketPriorityRequest : IRequest<ChangeTicketPriorityResult>
{
    public string? Id { get; init; }
    public string? PriorityId { get; init; }
    public string? UpdatedById { get; init; }
}

public class ChangeTicketPriorityValidator : AbstractValidator<ChangeTicketPriorityRequest>
{
    public ChangeTicketPriorityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PriorityId).NotEmpty();
    }
}

public class ChangeTicketPriorityHandler : IRequestHandler<ChangeTicketPriorityRequest, ChangeTicketPriorityResult>
{
    private readonly ICommandRepository<Ticket> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly ICurrentUserService _currentUser;

    public ChangeTicketPriorityHandler(
        ICommandRepository<Ticket> repository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        TicketHistoryRecorder historyRecorder,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _historyRecorder = historyRecorder;
        _currentUser = currentUser;
    }

    public async Task<ChangeTicketPriorityResult> Handle(ChangeTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        TicketAccessGuard.EnsureIsAgent(_currentUser.IsInRole(TicketAccessGuard.AgentRole));

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var priority = await _queryContext.TicketPriority
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PriorityId && !x.IsDeleted, cancellationToken);
        if (priority == null)
        {
            throw new Exception("Selected priority is not valid.");
        }

        var oldPriorityId = entity.PriorityId;
        entity.PriorityId = priority.Id;
        entity.UpdatedById = request.UpdatedById;
        entity.LastActivityAtUtc = DateTime.UtcNow;

        // Recomputed from the ticket's own creation time, same as at creation — not "now" —
        // so re-prioritizing a ticket doesn't quietly grant it a fresh SLA clock.
        entity.SlaResponseDueAtUtc = priority.SlaResponseHours is int rh ? entity.CreatedAtUtc?.AddHours(rh) : null;
        entity.SlaResolutionDueAtUtc = priority.SlaResolutionHours is int sh ? entity.CreatedAtUtc?.AddHours(sh) : null;

        _historyRecorder.Record(entity.Id, "PriorityChanged", oldPriorityId, priority.Id, request.UpdatedById);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ChangeTicketPriorityResult { Data = entity };
    }
}
