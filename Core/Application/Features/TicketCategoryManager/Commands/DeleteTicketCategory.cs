using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketCategoryManager.Commands;

public class DeleteTicketCategoryResult
{
    public TicketCategory? Data { get; set; }
}

public class DeleteTicketCategoryRequest : IRequest<DeleteTicketCategoryResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteTicketCategoryValidator : AbstractValidator<DeleteTicketCategoryRequest>
{
    public DeleteTicketCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteTicketCategoryHandler : IRequestHandler<DeleteTicketCategoryRequest, DeleteTicketCategoryResult>
{
    private readonly ICommandRepository<TicketCategory> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTicketCategoryHandler(
        ICommandRepository<TicketCategory> repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<DeleteTicketCategoryResult> Handle(DeleteTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket configuration role.");
        }

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteTicketCategoryResult { Data = entity };
    }
}
