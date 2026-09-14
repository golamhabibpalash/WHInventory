using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.SecurityManager;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesOrderManager.Queries;

public record GetSalesOrderListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? OrderDate { get; init; }
    public SalesOrderStatus? OrderStatus { get; init; }
    public string? OrderStatusName { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public string? CreatedById { get; init; }
    public string? CreatedByName { get; init; }
}

public class GetSalesOrderListResult
{
    public List<GetSalesOrderListDto>? Data { get; init; }
}

public class GetSalesOrderListRequest : IRequest<GetSalesOrderListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetSalesOrderListHandler : IRequestHandler<GetSalesOrderListRequest, GetSalesOrderListResult>
{
    private readonly IQueryContext _context;
    private readonly ISecurityService _securityService;

    public GetSalesOrderListHandler(IQueryContext context, ISecurityService securityService)
    {
        _context = context;
        _securityService = securityService;
    }

    public async Task<GetSalesOrderListResult> Handle(GetSalesOrderListRequest request, CancellationToken cancellationToken)
    {
        var page = await _context
            .SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.OrderDate,
                x.OrderStatus,
                x.Description,
                x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                x.TaxId,
                TaxName = x.Tax != null ? x.Tax.Name : string.Empty,
                x.BeforeTaxAmount,
                x.TaxAmount,
                x.AfterTaxAmount,
                x.CreatedAtUtc,
                x.CreatedById
            })
            .Take(2000)
            .ToListAsync(cancellationToken);

        var users = await _securityService.GetUserListAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());

        var dtos = page.Select(x => new GetSalesOrderListDto
        {
            Id = x.Id,
            Number = x.Number,
            OrderDate = x.OrderDate,
            OrderStatus = x.OrderStatus,
            OrderStatusName = x.OrderStatus.HasValue ? x.OrderStatus.Value.ToFriendlyName() : string.Empty,
            Description = x.Description,
            CustomerId = x.CustomerId,
            CustomerName = x.CustomerName,
            TaxId = x.TaxId,
            TaxName = x.TaxName,
            BeforeTaxAmount = x.BeforeTaxAmount,
            TaxAmount = x.TaxAmount,
            AfterTaxAmount = x.AfterTaxAmount,
            CreatedAtUtc = x.CreatedAtUtc,
            CreatedById = x.CreatedById,
            CreatedByName = x.CreatedById != null && userNames.TryGetValue(x.CreatedById, out var cn) ? cn : null
        }).ToList();

        return new GetSalesOrderListResult
        {
            Data = dtos
        };
    }


}



