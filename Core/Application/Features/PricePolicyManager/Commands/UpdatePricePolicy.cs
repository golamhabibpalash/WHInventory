using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PricePolicyManager.Commands;

public class UpdatePricePolicyResult
{
    public PricePolicy? Data { get; set; }
}

public class UpdatePricePolicyRequest : IRequest<UpdatePricePolicyResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsActive { get; init; } = true;
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePricePolicyValidator : AbstractValidator<UpdatePricePolicyRequest>
{
    public UpdatePricePolicyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdatePricePolicyHandler : IRequestHandler<UpdatePricePolicyRequest, UpdatePricePolicyResult>
{
    private readonly ICommandRepository<PricePolicy> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePricePolicyHandler(ICommandRepository<PricePolicy> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePricePolicyResult> Handle(UpdatePricePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.Priority = request.Priority;
        entity.IsActive = request.IsActive;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePricePolicyResult { Data = entity };
    }
}
