using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuickShortcutManager.Commands;

public class DeleteQuickShortcutResult
{
    public QuickShortcut? Data { get; set; }
}

public class DeleteQuickShortcutRequest : IRequest<DeleteQuickShortcutResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteQuickShortcutValidator : AbstractValidator<DeleteQuickShortcutRequest>
{
    public DeleteQuickShortcutValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteQuickShortcutHandler : IRequestHandler<DeleteQuickShortcutRequest, DeleteQuickShortcutResult>
{
    private readonly ICommandRepository<QuickShortcut> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQuickShortcutHandler(
        ICommandRepository<QuickShortcut> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteQuickShortcutResult> Handle(DeleteQuickShortcutRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteQuickShortcutResult
        {
            Data = entity
        };
    }
}
