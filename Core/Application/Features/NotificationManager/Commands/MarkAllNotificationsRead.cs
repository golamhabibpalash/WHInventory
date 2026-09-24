using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NotificationManager.Commands;

public class MarkAllNotificationsReadResult
{
    public int MarkedCount { get; set; }
}

public class MarkAllNotificationsReadRequest : IRequest<MarkAllNotificationsReadResult>
{
    public string? UserId { get; init; }
    public string? UpdatedById { get; init; }
}

public class MarkAllNotificationsReadValidator : AbstractValidator<MarkAllNotificationsReadRequest>
{
    public MarkAllNotificationsReadValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadRequest, MarkAllNotificationsReadResult>
{
    private readonly ICommandRepository<Notification> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsReadHandler(
        ICommandRepository<Notification> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkAllNotificationsReadResult> Handle(MarkAllNotificationsReadRequest request, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQuery()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.UserId == request.UserId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.MarkRead();
            entity.UpdatedById = request.UpdatedById;
            _repository.Update(entity);
        }

        if (entities.Count > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new MarkAllNotificationsReadResult
        {
            MarkedCount = entities.Count
        };
    }
}
