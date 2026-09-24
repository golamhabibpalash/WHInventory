using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.NotificationManager.Commands;

public class MarkNotificationReadResult
{
    public Notification? Data { get; set; }
}

public class MarkNotificationReadRequest : IRequest<MarkNotificationReadResult>
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? UpdatedById { get; init; }
}

public class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadRequest>
{
    public MarkNotificationReadValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadRequest, MarkNotificationReadResult>
{
    private readonly ICommandRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationReadHandler(
        ICommandRepository<Notification> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkNotificationReadResult> Handle(MarkNotificationReadRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        // Never trust the client's id alone — a notification belongs to exactly one user.
        if (entity.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Notification does not belong to the current user.");
        }

        if (!entity.IsRead)
        {
            entity.MarkRead();
            entity.UpdatedById = request.UpdatedById;
            _repository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new MarkNotificationReadResult
        {
            Data = entity
        };
    }
}
