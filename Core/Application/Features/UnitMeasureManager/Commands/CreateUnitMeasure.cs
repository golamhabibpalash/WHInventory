using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.UnitMeasureManager.Commands;

public class CreateUnitMeasureResult
{
    public UnitMeasure? Data { get; set; }
}

public class CreateUnitMeasureRequest : IRequest<CreateUnitMeasureResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateUnitMeasureValidator : AbstractValidator<CreateUnitMeasureRequest>
{
    public CreateUnitMeasureValidator(IQueryContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MustAsync(async (name, cancellationToken) =>
            {
                return !await context.UnitMeasure.AnyAsync(x => x.Name == name && !x.IsDeleted, cancellationToken);
            }).WithMessage("Unit name already exist.");
    }
}

public class CreateUnitMeasureHandler : IRequestHandler<CreateUnitMeasureRequest, CreateUnitMeasureResult>
{
    private readonly ICommandRepository<UnitMeasure> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUnitMeasureHandler(
        ICommandRepository<UnitMeasure> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUnitMeasureResult> Handle(CreateUnitMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new UnitMeasure();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateUnitMeasureResult
        {
            Data = entity
        };
    }
}
