using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetProductByBarcodeDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? Barcode { get; init; }
    public double? UnitPrice { get; init; }
    public bool? Physical { get; init; }
    public string? UnitMeasureId { get; init; }
    public string? UnitMeasureName { get; init; }
    public string? ProductGroupId { get; init; }
    public string? ProductGroupName { get; init; }
    public string? ImageName { get; init; }
}

public class GetProductByBarcodeResult
{
    public GetProductByBarcodeDto? Data { get; init; }
}

public class GetProductByBarcodeRequest : IRequest<GetProductByBarcodeResult>
{
    public string? Barcode { get; init; }
}

public class GetProductByBarcodeValidator : AbstractValidator<GetProductByBarcodeRequest>
{
    public GetProductByBarcodeValidator()
    {
        RuleFor(x => x.Barcode).NotEmpty();
    }
}

public class GetProductByBarcodeHandler : IRequestHandler<GetProductByBarcodeRequest, GetProductByBarcodeResult>
{
    private readonly IQueryContext _context;

    public GetProductByBarcodeHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetProductByBarcodeResult> Handle(GetProductByBarcodeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Include(x => x.UnitMeasure)
            .Include(x => x.ProductGroup)
            .Where(x => x.Barcode == request.Barcode)
            .Select(x => new GetProductByBarcodeDto
            {
                Id = x.Id,
                Number = x.Number,
                Name = x.Name,
                Barcode = x.Barcode,
                UnitPrice = x.UnitPrice,
                Physical = x.Physical,
                UnitMeasureId = x.UnitMeasureId,
                UnitMeasureName = x.UnitMeasure != null ? x.UnitMeasure.Name : null,
                ProductGroupId = x.ProductGroupId,
                ProductGroupName = x.ProductGroup != null ? x.ProductGroup.Name : null,
                ImageName = x.ImageName
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new GetProductByBarcodeResult { Data = entity };
    }
}
