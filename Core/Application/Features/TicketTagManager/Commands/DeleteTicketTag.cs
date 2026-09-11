using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketTagManager.Commands;

public class DeleteTicketTagResult
{
    public TicketTag? Data { get; set; }
}

public class DeleteTicketTagRequest : IRequest<DeleteTicketTagResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteTicketTagValidator : AbstractValidator<DeleteTicketTagRequest>
{
    public DeleteTicketTagValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteTicketTagHandler : IRequestHandler<DeleteTicketTagRequest, DeleteTicketTagResult>
{
    private readonly ICommandRepository<TicketTag> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTicketTagHandler(
        ICommandRepository<TicketTag> repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<DeleteTicketTagResult> Handle(DeleteTicketTagRequest request, CancellationToken cancellationToken)
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

        return new DeleteTicketTagResult { Data = entity };
    }
}
