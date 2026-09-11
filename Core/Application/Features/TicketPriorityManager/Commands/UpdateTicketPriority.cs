using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketPriorityManager.Commands;

public class UpdateTicketPriorityResult
{
    public TicketPriority? Data { get; set; }
}

public class UpdateTicketPriorityRequest : IRequest<UpdateTicketPriorityResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? ColorHex { get; init; }
    public int Level { get; init; }
    public int? SlaResponseHours { get; init; }
    public int? SlaResolutionHours { get; init; }
    public bool IsActive { get; init; } = true;
    public string? UpdatedById { get; init; }
}

public class UpdateTicketPriorityValidator : AbstractValidator<UpdateTicketPriorityRequest>
{
    public UpdateTicketPriorityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateTicketPriorityHandler : IRequestHandler<UpdateTicketPriorityRequest, UpdateTicketPriorityResult>
{
    private readonly ICommandRepository<TicketPriority> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateTicketPriorityHandler(
        ICommandRepository<TicketPriority> repository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UpdateTicketPriorityResult> Handle(UpdateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket configuration role.");
        }

        var nameExists = await _queryContext.TicketPriority
            .AnyAsync(x => x.Name!.ToLower() == request.Name!.ToLower() && x.Id != request.Id && !x.IsDeleted, cancellationToken);
        if (nameExists)
            throw new Exception("Priority name already exists.");

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
            throw new Exception($"Entity not found: {request.Id}");

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name;
        entity.ColorHex = request.ColorHex;
        entity.Level = request.Level;
        entity.SlaResponseHours = request.SlaResponseHours;
        entity.SlaResolutionHours = request.SlaResolutionHours;
        entity.IsActive = request.IsActive;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateTicketPriorityResult { Data = entity };
    }
}
