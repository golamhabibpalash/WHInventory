using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuickShortcutManager.Commands;

public class CreateQuickShortcutResult
{
    public QuickShortcut? Data { get; set; }
}

public class CreateQuickShortcutRequest : IRequest<CreateQuickShortcutResult>
{
    public string? Name { get; init; }
    public string? Icon { get; init; }
    public string? Url { get; init; }
    public int SortOrder { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateQuickShortcutValidator : AbstractValidator<CreateQuickShortcutRequest>
{
    public CreateQuickShortcutValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Icon).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().Must(url => url!.StartsWith('/')).WithMessage("Url must be an app-relative path starting with '/'.");
    }
}

public class CreateQuickShortcutHandler : IRequestHandler<CreateQuickShortcutRequest, CreateQuickShortcutResult>
{
    private readonly ICommandRepository<QuickShortcut> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuickShortcutHandler(
        ICommandRepository<QuickShortcut> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateQuickShortcutResult> Handle(CreateQuickShortcutRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new QuickShortcut();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Icon = request.Icon;
        entity.Url = request.Url;
        entity.SortOrder = request.SortOrder;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateQuickShortcutResult
        {
            Data = entity
        };
    }
}
