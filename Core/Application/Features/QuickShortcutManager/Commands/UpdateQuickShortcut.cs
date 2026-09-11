using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuickShortcutManager.Commands;

public class UpdateQuickShortcutResult
{
    public QuickShortcut? Data { get; set; }
}

public class UpdateQuickShortcutRequest : IRequest<UpdateQuickShortcutResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Icon { get; init; }
    public string? Url { get; init; }
    public int SortOrder { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateQuickShortcutValidator : AbstractValidator<UpdateQuickShortcutRequest>
{
    public UpdateQuickShortcutValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Icon).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().Must(url => url!.StartsWith('/')).WithMessage("Url must be an app-relative path starting with '/'.");
    }
}

public class UpdateQuickShortcutHandler : IRequestHandler<UpdateQuickShortcutRequest, UpdateQuickShortcutResult>
{
    private readonly ICommandRepository<QuickShortcut> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateQuickShortcutHandler(
        ICommandRepository<QuickShortcut> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateQuickShortcutResult> Handle(UpdateQuickShortcutRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
            throw new Exception($"Entity not found: {request.Id}");

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Icon = request.Icon;
        entity.Url = request.Url;
        entity.SortOrder = request.SortOrder;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateQuickShortcutResult
        {
            Data = entity
        };
    }
}
