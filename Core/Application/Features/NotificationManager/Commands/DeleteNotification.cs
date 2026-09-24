using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.NotificationManager.Commands;

public class DeleteNotificationResult
{
    public Notification? Data { get; set; }
}

public class DeleteNotificationRequest : IRequest<DeleteNotificationResult>
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteNotificationValidator : AbstractValidator<DeleteNotificationRequest>
{
    public DeleteNotificationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationRequest, DeleteNotificationResult>
{
    private readonly ICommandRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationHandler(
        ICommandRepository<Notification> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteNotificationResult> Handle(DeleteNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        if (entity.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Notification does not belong to the current user.");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteNotificationResult
        {
            Data = entity
        };
    }
}
