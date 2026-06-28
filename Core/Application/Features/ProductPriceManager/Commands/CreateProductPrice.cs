using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductPriceManager.Commands;

public class CreateProductPriceResult
{
    public ProductPrice? Data { get; set; }
}

public class CreateProductPriceRequest : IRequest<CreateProductPriceResult>
{
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
    public string? CreatedById { get; init; }
}

public class CreateProductPriceValidator : AbstractValidator<CreateProductPriceRequest>
{
    public CreateProductPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class CreateProductPriceHandler : IRequestHandler<CreateProductPriceRequest, CreateProductPriceResult>
{
    private readonly ICommandRepository<ProductPrice> _repository;
    private readonly ICommandRepository<PriceHistory> _priceHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductPriceHandler(
        ICommandRepository<ProductPrice> repository,
        ICommandRepository<PriceHistory> priceHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _priceHistoryRepository = priceHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductPriceResult> Handle(CreateProductPriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ProductPrice();
        entity.CreatedById = request.CreatedById;
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

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var history = new PriceHistory();
        history.CreatedById = request.CreatedById;
        history.ProductPriceId = entity.Id;
        history.PreviousPrice = null;
        history.NewPrice = request.FixedPrice;
        history.ChangedById = request.CreatedById;
        history.ChangedDate = DateTime.UtcNow;
        history.ChangeReason = "Initial price set";

        await _priceHistoryRepository.CreateAsync(history, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateProductPriceResult { Data = entity };
    }
}
