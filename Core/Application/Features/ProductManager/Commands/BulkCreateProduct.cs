using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Commands;

public class BulkCreateProductResult
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkCreateProductErrorRow>? Errors { get; init; }
}

public class BulkCreateProductErrorRow
{
    public int RowNumber { get; init; }
    public string? Name { get; init; }
    public string? Message { get; init; }
}

public class BulkCreateProductRequest : IRequest<BulkCreateProductResult>
{
    public byte[] Data { get; init; } = null!;
    public string? CreatedById { get; init; }
}

public class BulkCreateProductValidator : AbstractValidator<BulkCreateProductRequest>
{
    public BulkCreateProductValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class BulkCreateProductHandler : IRequestHandler<BulkCreateProductRequest, BulkCreateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _context;

    public BulkCreateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext context
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _context = context;
    }

    public async Task<BulkCreateProductResult> Handle(BulkCreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var unitMeasures = await _context.UnitMeasure.AsNoTracking().ToListAsync(cancellationToken);
        var productGroups = await _context.ProductGroup.AsNoTracking().ToListAsync(cancellationToken);
        var brands = await _context.Brand.AsNoTracking().ToListAsync(cancellationToken);

        var errors = new List<BulkCreateProductErrorRow>();
        var successCount = 0;
        var totalRows = 0;

        using var stream = new MemoryStream(request.Data);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var range = sheet.RangeUsed();

        if (range == null)
        {
            return new BulkCreateProductResult
            {
                TotalRows = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Errors = new List<BulkCreateProductErrorRow>()
            };
        }

        var rows = range.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            totalRows++;
            var rowNumber = row.RowNumber();
            var name = row.Cell(1).GetString().Trim();
            var unitPriceText = row.Cell(2).GetString().Trim();
            var unitMeasureName = row.Cell(3).GetString().Trim();
            var productGroupName = row.Cell(4).GetString().Trim();
            var brandName = row.Cell(5).GetString().Trim();
            var barcode = row.Cell(6).GetString().Trim();
            var description = row.Cell(7).GetString().Trim();
            var isWarrantyText = row.Cell(8).GetString().Trim();

            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
                rowErrors.Add("Name is required.");

            if (!double.TryParse(unitPriceText, out var unitPrice))
                rowErrors.Add("UnitPrice must be a valid number.");

            if (string.IsNullOrWhiteSpace(unitMeasureName))
                rowErrors.Add("UnitMeasureName is required.");

            if (string.IsNullOrWhiteSpace(productGroupName))
                rowErrors.Add("ProductGroupName is required.");

            if (rowErrors.Count > 0)
            {
                errors.Add(new BulkCreateProductErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = string.Join(" ", rowErrors)
                });
                continue;
            }

            var unitMeasure = unitMeasures.FirstOrDefault(x => x.Name == unitMeasureName);
            if (unitMeasure == null)
            {
                errors.Add(new BulkCreateProductErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = $"UnitMeasure '{unitMeasureName}' not found."
                });
                continue;
            }

            var productGroup = productGroups.FirstOrDefault(x => x.Name == productGroupName);
            if (productGroup == null)
            {
                errors.Add(new BulkCreateProductErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = $"ProductGroup '{productGroupName}' not found."
                });
                continue;
            }

            Brand? brand = null;
            if (!string.IsNullOrWhiteSpace(brandName))
            {
                brand = brands.FirstOrDefault(x => x.Name == brandName);
                if (brand == null)
                {
                    errors.Add(new BulkCreateProductErrorRow
                    {
                        RowNumber = rowNumber,
                        Name = name,
                        Message = $"Brand '{brandName}' not found."
                    });
                    continue;
                }
            }

            var isWarranty = string.IsNullOrWhiteSpace(isWarrantyText) ? false :
                isWarrantyText.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            var entity = new Product();
            entity.CreatedById = request.CreatedById;
            entity.Number = _numberSequenceService.GenerateNumber(nameof(Product), "", "ART");
            entity.Name = name;
            entity.UnitPrice = unitPrice;
            entity.Physical = true;
            entity.Description = string.IsNullOrWhiteSpace(description) ? null : description;
            entity.UnitMeasureId = unitMeasure.Id;
            entity.ProductGroupId = productGroup.Id;
            entity.BrandId = brand?.Id;
            entity.Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
            entity.IsWarrantyApplicable = isWarranty;

            await _repository.CreateAsync(entity, cancellationToken);
            successCount++;
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new BulkCreateProductResult
        {
            TotalRows = totalRows,
            SuccessCount = successCount,
            FailureCount = errors.Count,
            Errors = errors
        };
    }
}
