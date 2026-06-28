using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PricePolicyManager.Commands;

public class CreatePricePolicyResult
{
    public PricePolicy? Data { get; set; }
}

public class CreatePricePolicyRequest : IRequest<CreatePricePolicyResult>
{
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsActive { get; init; } = true;
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePricePolicyValidator : AbstractValidator<CreatePricePolicyRequest>
{
    public CreatePricePolicyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreatePricePolicyHandler : IRequestHandler<CreatePricePolicyRequest, CreatePricePolicyResult>
{
    private readonly ICommandRepository<PricePolicy> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePricePolicyHandler(ICommandRepository<PricePolicy> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePricePolicyResult> Handle(CreatePricePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PricePolicy();
        entity.CreatedById = request.CreatedById;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.Priority = request.Priority;
        entity.IsActive = request.IsActive;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePricePolicyResult { Data = entity };
    }
}
