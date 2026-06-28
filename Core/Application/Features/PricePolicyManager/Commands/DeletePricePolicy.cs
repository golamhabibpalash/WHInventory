using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PricePolicyManager.Commands;

public class DeletePricePolicyResult
{
    public PricePolicy? Data { get; set; }
}

public class DeletePricePolicyRequest : IRequest<DeletePricePolicyResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePricePolicyValidator : AbstractValidator<DeletePricePolicyRequest>
{
    public DeletePricePolicyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePricePolicyHandler : IRequestHandler<DeletePricePolicyRequest, DeletePricePolicyResult>
{
    private readonly ICommandRepository<PricePolicy> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePricePolicyHandler(ICommandRepository<PricePolicy> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePricePolicyResult> Handle(DeletePricePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePricePolicyResult { Data = entity };
    }
}
