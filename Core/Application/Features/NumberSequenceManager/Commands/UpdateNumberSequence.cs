using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.NumberSequenceManager.Commands;

public class UpdateNumberSequenceResult
{
    public NumberSequence? Data { get; set; }
}

public class UpdateNumberSequenceRequest : IRequest<UpdateNumberSequenceResult>
{
    public string? Id { get; init; }
    public int? LastUsedCount { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateNumberSequenceValidator : AbstractValidator<UpdateNumberSequenceRequest>
{
    public UpdateNumberSequenceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LastUsedCount).NotNull().GreaterThanOrEqualTo(0);
    }
}

// EntityName/Prefix/Suffix are intentionally not editable here: NumberSequenceService.GenerateNumber
// looks a row up by that exact triple (it's also the entity's unique index), and every call site
// hardcodes its own triple (e.g. GenerateNumber(nameof(SalesOrder), "", "SO")). Changing them on an
// existing row would orphan it, so the next GenerateNumber call for that document type would insert
// a fresh row starting at 1 and reissue numbers that already exist. LastUsedCount is the only field
// that's safe to hand-edit — it just moves where the next generated number starts from.
public class UpdateNumberSequenceHandler : IRequestHandler<UpdateNumberSequenceRequest, UpdateNumberSequenceResult>
{
    private readonly ICommandRepository<NumberSequence> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNumberSequenceHandler(
        ICommandRepository<NumberSequence> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateNumberSequenceResult> Handle(UpdateNumberSequenceRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.LastUsedCount = request.LastUsedCount;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateNumberSequenceResult
        {
            Data = entity
        };
    }
}
