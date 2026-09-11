using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketCategoryManager.Commands;

public class CreateTicketCategoryResult
{
    public TicketCategory? Data { get; set; }
}

public class CreateTicketCategoryRequest : IRequest<CreateTicketCategoryResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public string? CreatedById { get; init; }
}

public class CreateTicketCategoryValidator : AbstractValidator<CreateTicketCategoryRequest>
{
    public CreateTicketCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateTicketCategoryHandler : IRequestHandler<CreateTicketCategoryRequest, CreateTicketCategoryResult>
{
    private readonly ICommandRepository<TicketCategory> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTicketCategoryHandler(
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

    public async Task<CreateTicketCategoryResult> Handle(CreateTicketCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket configuration role.");
        }

        var nameExists = await _queryContext.TicketCategory
            .AnyAsync(x => x.Name!.ToLower() == request.Name!.ToLower() && !x.IsDeleted, cancellationToken);

        if (nameExists)
            throw new Exception("Category name already exists.");

        var entity = new TicketCategory
        {
            CreatedById = request.CreatedById,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTicketCategoryResult { Data = entity };
    }
}
