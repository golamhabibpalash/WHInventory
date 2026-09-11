using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketPriorityManager.Commands;

public class DeleteTicketPriorityResult
{
    public TicketPriority? Data { get; set; }
}

public class DeleteTicketPriorityRequest : IRequest<DeleteTicketPriorityResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteTicketPriorityValidator : AbstractValidator<DeleteTicketPriorityRequest>
{
    public DeleteTicketPriorityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteTicketPriorityHandler : IRequestHandler<DeleteTicketPriorityRequest, DeleteTicketPriorityResult>
{
    private readonly ICommandRepository<TicketPriority> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTicketPriorityHandler(
        ICommandRepository<TicketPriority> repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<DeleteTicketPriorityResult> Handle(DeleteTicketPriorityRequest request, CancellationToken cancellationToken)
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

        return new DeleteTicketPriorityResult { Data = entity };
    }
}
