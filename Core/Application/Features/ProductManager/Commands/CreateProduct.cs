using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class CreateProductResult
{
    public Product? Data { get; set; }
}

public class CreateProductRequest : IRequest<CreateProductResult>
{
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public double? UnitPrice { get; init; }
    public double? MinSellingPrice { get; init; }
    public double? MaxSellingPrice { get; init; }
    public bool? Physical { get; init; } = true;
    public string? UnitMeasureId { get; init; }
    public string? ProductGroupId { get; init; }
    public string? BrandId { get; init; }
    public string? ImageName { get; init; }
    public string? Barcode { get; init; }
    public bool? IsWarrantyApplicable { get; init; } = false;
    public int? WarrantyDays { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.MinSellingPrice)
            .GreaterThanOrEqualTo(0).When(x => x.MinSellingPrice.HasValue)
            .WithMessage("Minimum selling price cannot be negative.");
        RuleFor(x => x.MaxSellingPrice)
            .GreaterThanOrEqualTo(0).When(x => x.MaxSellingPrice.HasValue)
            .WithMessage("Maximum selling price cannot be negative.");
        RuleFor(x => x.MaxSellingPrice)
            .GreaterThanOrEqualTo(x => x.MinSellingPrice)
            .When(x => x.MinSellingPrice.HasValue && x.MaxSellingPrice.HasValue)
            .WithMessage("Maximum selling price must be greater than or equal to the minimum selling price.");
        RuleFor(x => x.Physical).NotEmpty();
        RuleFor(x => x.UnitMeasureId).NotEmpty();
        RuleFor(x => x.ProductGroupId).NotEmpty();
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductRequest, CreateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateProductResult> Handle(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Product();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(Product), "", "ART");
        entity.Name = request.Name;
        entity.UnitPrice = request.UnitPrice;
        entity.MinSellingPrice = request.MinSellingPrice;
        entity.MaxSellingPrice = request.MaxSellingPrice;
        entity.Physical = request.Physical;
        entity.Description = request.Description;
        entity.UnitMeasureId = request.UnitMeasureId;
        entity.ProductGroupId = request.ProductGroupId;
        entity.BrandId = request.BrandId;
        entity.ImageName = string.IsNullOrWhiteSpace(request.ImageName) ? null : request.ImageName;
        entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entity.IsWarrantyApplicable = request.IsWarrantyApplicable;
        entity.WarrantyDays = (request.IsWarrantyApplicable == true) ? request.WarrantyDays : null;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateProductResult
        {
            Data = entity
        };
    }
}
