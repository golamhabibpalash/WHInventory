using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuantityBreakManager.Commands;

public class UpdateQuantityBreakResult
{
    public QuantityBreak? Data { get; set; }
}

public class UpdateQuantityBreakRequest : IRequest<UpdateQuantityBreakResult>
{
    public string? Id { get; init; }
    public string? ProductPriceId { get; init; }
    public double MinQuantity { get; init; } = 1;
    public double? MaxQuantity { get; init; }
    public double Price { get; init; } = 0;
    public string? UpdatedById { get; init; }
}

public class UpdateQuantityBreakValidator : AbstractValidator<UpdateQuantityBreakRequest>
{
    public UpdateQuantityBreakValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProductPriceId).NotEmpty();
        RuleFor(x => x.MinQuantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpdateQuantityBreakHandler : IRequestHandler<UpdateQuantityBreakRequest, UpdateQuantityBreakResult>
{
    private readonly ICommandRepository<QuantityBreak> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateQuantityBreakHandler(ICommandRepository<QuantityBreak> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateQuantityBreakResult> Handle(UpdateQuantityBreakRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.ProductPriceId = request.ProductPriceId;
        entity.MinQuantity = request.MinQuantity;
        entity.MaxQuantity = request.MaxQuantity;
        entity.Price = request.Price;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateQuantityBreakResult { Data = entity };
    }
}
