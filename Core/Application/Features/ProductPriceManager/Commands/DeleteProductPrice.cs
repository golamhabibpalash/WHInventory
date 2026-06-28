using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductPriceManager.Commands;

public class DeleteProductPriceResult
{
    public ProductPrice? Data { get; set; }
}

public class DeleteProductPriceRequest : IRequest<DeleteProductPriceResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteProductPriceValidator : AbstractValidator<DeleteProductPriceRequest>
{
    public DeleteProductPriceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteProductPriceHandler : IRequestHandler<DeleteProductPriceRequest, DeleteProductPriceResult>
{
    private readonly ICommandRepository<ProductPrice> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductPriceHandler(ICommandRepository<ProductPrice> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProductPriceResult> Handle(DeleteProductPriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProductPriceResult { Data = entity };
    }
}
