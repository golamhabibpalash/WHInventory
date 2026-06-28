using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WarrantyManager.Queries;

public record GetWarrantyCheckDto
{
    public string? DeliveryOrderId { get; init; }
    public string? DeliveryOrderNumber { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? SalesOrderId { get; init; }
    public string? SalesOrderNumber { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public double? Quantity { get; init; }
    public int? WarrantyDays { get; init; }
    public DateTime? WarrantyExpireDate { get; init; }
    public int? DaysRemaining { get; init; }
    public bool IsWarrantyValid { get; init; }
    public string? WarrantyStatus { get; init; }
}

public class GetWarrantyCheckResult
{
    public List<GetWarrantyCheckDto>? Data { get; init; }
}

public class GetWarrantyCheckRequest : IRequest<GetWarrantyCheckResult>
{
    public string? CustomerId { get; init; }
    public string? ProductId { get; init; }
    public DateTime? ClaimDate { get; init; }
}

public class GetWarrantyCheckHandler : IRequestHandler<GetWarrantyCheckRequest, GetWarrantyCheckResult>
{
    private readonly IQueryContext _context;

    public GetWarrantyCheckHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetWarrantyCheckResult> Handle(GetWarrantyCheckRequest request, CancellationToken cancellationToken)
    {
        var claimDate = (request.ClaimDate ?? DateTime.UtcNow).Date;

        var query =
            from d in _context.DeliveryOrder.AsNoTracking().ApplyIsDeletedFilter(false)
            join so in _context.SalesOrder.AsNoTracking().ApplyIsDeletedFilter(false) on d.SalesOrderId equals so.Id
            join c in _context.Customer.AsNoTracking().ApplyIsDeletedFilter(false) on so.CustomerId equals c.Id
            join soi in _context.SalesOrderItem.AsNoTracking().ApplyIsDeletedFilter(false) on so.Id equals soi.SalesOrderId
            join p in _context.Product.AsNoTracking().ApplyIsDeletedFilter(false) on soi.ProductId equals p.Id
            where d.Status == DeliveryOrderStatus.Confirmed
            where p.IsWarrantyApplicable == true && p.WarrantyDays != null && p.WarrantyDays > 0
            where request.CustomerId == null || so.CustomerId == request.CustomerId
            where request.ProductId == null || soi.ProductId == request.ProductId
            select new
            {
                DeliveryOrderId = d.Id,
                d.Number,
                d.DeliveryDate,
                CustomerId = so.CustomerId,
                CustomerName = c.Name,
                SalesOrderId = d.SalesOrderId,
                SalesOrderNumber = so.Number,
                ProductId = soi.ProductId,
                ProductName = p.Name,
                ProductNumber = p.Number,
                soi.Quantity,
                p.WarrantyDays,
            };

        var rows = await query.Take(2000).ToListAsync(cancellationToken);

        var dtos = rows.Select(row =>
        {
            var expireDate = row.DeliveryDate.HasValue && row.WarrantyDays.HasValue
                ? row.DeliveryDate.Value.AddDays(row.WarrantyDays.Value).Date
                : (DateTime?)null;

            var daysRemaining = expireDate.HasValue
                ? (int)(expireDate.Value - claimDate).TotalDays
                : (int?)null;

            var isValid = expireDate.HasValue && claimDate <= expireDate.Value;

            var status = expireDate == null ? "Not Applicable"
                       : isValid ? "Valid"
                       : "Expired";

            return new GetWarrantyCheckDto
            {
                DeliveryOrderId = row.DeliveryOrderId,
                DeliveryOrderNumber = row.Number,
                DeliveryDate = row.DeliveryDate,
                CustomerId = row.CustomerId,
                CustomerName = row.CustomerName,
                SalesOrderId = row.SalesOrderId,
                SalesOrderNumber = row.SalesOrderNumber,
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                ProductNumber = row.ProductNumber,
                Quantity = row.Quantity,
                WarrantyDays = row.WarrantyDays,
                WarrantyExpireDate = expireDate,
                DaysRemaining = daysRemaining,
                IsWarrantyValid = isValid,
                WarrantyStatus = status,
            };
        }).ToList();

        return new GetWarrantyCheckResult { Data = dtos };
    }
}
