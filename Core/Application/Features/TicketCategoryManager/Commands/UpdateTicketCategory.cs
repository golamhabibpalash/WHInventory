using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketCategoryManager.Commands;

public class UpdateTicketCategoryResult
{
    public TicketCategory? Data { get; set; }
}

public class UpdateTicketCategoryRequest : IRequest<UpdateTicketCategoryResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public string? UpdatedById { get; init; }
}

public class UpdateTicketCategoryValidator : AbstractValidator<UpdateTicketCategoryRequest>
{
    public UpdateTicketCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateTicketCategoryHandler : IRequestHandler<UpdateTicketCategoryRequest, UpdateTicketCategoryResult>
{
    private readonly ICommandRepository<TicketCategory> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateTicketCategoryHandler(
        ICommandRepository<TicketCategory> repository,
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

    public async Task<UpdateTicketCategoryResult> Handle(UpdateTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket configuration role.");
        }

        var nameExists = await _queryContext.TicketCategory
            .AnyAsync(x => x.Name!.ToLower() == request.Name!.ToLower() && x.Id != request.Id && !x.IsDeleted, cancellationToken);

        if (nameExists)
            throw new Exception("Category name already exists.");

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
            throw new Exception($"Entity not found: {request.Id}");

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateTicketCategoryResult { Data = entity };
    }
}
