using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductPriceManager.Commands;

public class UpdateProductPriceResult
{
    public ProductPrice? Data { get; set; }
}

public class UpdateProductPriceRequest : IRequest<UpdateProductPriceResult>
{
    public string? Id { get; init; }
    public string? ProductId { get; init; }
    public string? PricePolicyId { get; init; }
    public PricingCalculationMethod CalculationMethod { get; init; } = PricingCalculationMethod.FixedPrice;
    public double? FixedPrice { get; init; }
    public double? MarkupPercent { get; init; }
    public double? MarkupAmount { get; init; }
    public double? MarginPercent { get; init; }
    public double? FormulaMultiplier { get; init; }
    public double? MinimumSellingPrice { get; init; }
    public double? MaximumDiscountPercent { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsActive { get; init; } = true;
    public string? UpdatedById { get; init; }
    public string? ChangeReason { get; init; }
}

public class UpdateProductPriceValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class UpdateProductPriceHandler : IRequestHandler<UpdateProductPriceRequest, UpdateProductPriceResult>
{
    private readonly ICommandRepository<ProductPrice> _repository;
    private readonly ICommandRepository<PriceHistory> _priceHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductPriceHandler(
        ICommandRepository<ProductPrice> repository,
        ICommandRepository<PriceHistory> priceHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _priceHistoryRepository = priceHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProductPriceResult> Handle(UpdateProductPriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var previousFixedPrice = entity.FixedPrice;

        entity.UpdatedById = request.UpdatedById;
        entity.ProductId = request.ProductId;
        entity.PricePolicyId = request.PricePolicyId;
        entity.CalculationMethod = request.CalculationMethod;
        entity.FixedPrice = request.FixedPrice;
        entity.MarkupPercent = request.MarkupPercent;
        entity.MarkupAmount = request.MarkupAmount;
        entity.MarginPercent = request.MarginPercent;
        entity.FormulaMultiplier = request.FormulaMultiplier;
        entity.MinimumSellingPrice = request.MinimumSellingPrice;
        entity.MaximumDiscountPercent = request.MaximumDiscountPercent;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Priority = request.Priority;
        entity.IsActive = request.IsActive;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        if (previousFixedPrice != request.FixedPrice)
        {
            var history = new PriceHistory();
            history.CreatedById = request.UpdatedById;
            history.ProductPriceId = entity.Id;
            history.PreviousPrice = previousFixedPrice;
            history.NewPrice = request.FixedPrice;
            history.ChangedById = request.UpdatedById;
            history.ChangedDate = DateTime.UtcNow;
            history.ChangeReason = request.ChangeReason ?? "Price updated";

            await _priceHistoryRepository.CreateAsync(history, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new UpdateProductPriceResult { Data = entity };
    }
}
