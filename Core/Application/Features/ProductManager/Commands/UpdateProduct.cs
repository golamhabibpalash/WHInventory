using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class UpdateProductResult
{
    public Product? Data { get; set; }
}

public class UpdateProductRequest : IRequest<UpdateProductResult>
{
    public string? Id { get; init; }
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
    public string? UpdatedById { get; init; }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

public class UpdateProductHandler : IRequestHandler<UpdateProductRequest, UpdateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProductResult> Handle(UpdateProductRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.UnitPrice = request.UnitPrice.ToMoney();
        entity.MinSellingPrice = request.MinSellingPrice.ToMoney();
        entity.MaxSellingPrice = request.MaxSellingPrice.ToMoney();
        entity.Physical = request.Physical;
        entity.Description = request.Description;
        entity.UnitMeasureId = request.UnitMeasureId;
        entity.ProductGroupId = request.ProductGroupId;
        entity.BrandId = request.BrandId;
        entity.ImageName = string.IsNullOrWhiteSpace(request.ImageName) ? null : request.ImageName;
        entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entity.IsWarrantyApplicable = request.IsWarrantyApplicable;
        entity.WarrantyDays = (request.IsWarrantyApplicable == true) ? request.WarrantyDays : null;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateProductResult
        {
            Data = entity
        };
    }
}

