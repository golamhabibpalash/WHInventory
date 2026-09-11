using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Commands;

public class UpdateTicketResult
{
    public Ticket? Data { get; set; }
}

public class UpdateTicketRequest : IRequest<UpdateTicketResult>
{
    public string? Id { get; init; }
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public string? CategoryId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateTicketValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateTicketHandler : IRequestHandler<UpdateTicketRequest, UpdateTicketResult>
{
    private readonly ICommandRepository<Ticket> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly ICurrentUserService _currentUser;

    public UpdateTicketHandler(
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

    public async Task<UpdateTicketResult> Handle(UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        TicketAccessGuard.EnsureCanAccess(entity, _currentUser.UserId, isAgent);

        // A requester may only edit their own ticket while it is still untouched by an agent
        // (New) — once work has started, only the agent may change what the ticket says.
        if (!isAgent && entity.Status != TicketStatus.New)
        {
            throw new Exception("This ticket can no longer be edited by the requester.");
        }

        var category = await _queryContext.TicketCategory
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);
        if (category == null || category.IsActive == false)
        {
            throw new Exception("Selected category is not valid.");
        }

        if (entity.CategoryId != request.CategoryId)
        {
            _historyRecorder.Record(entity.Id, "CategoryChanged", entity.CategoryId, request.CategoryId, request.UpdatedById);
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Subject = request.Subject;
        entity.Description = request.Description;
        entity.CategoryId = request.CategoryId;
        entity.LastActivityAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateTicketResult { Data = entity };
    }
}
