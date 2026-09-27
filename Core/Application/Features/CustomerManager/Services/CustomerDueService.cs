using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.PaymentManager.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Services;

/// <summary>
/// One customer's billed / paid roll-up over confirmed sales orders.
/// Due is clamped at zero: an overpayment leaves the customer in credit, not in negative due.
/// </summary>
public record CustomerDueAggregate
{
    public string? CustomerId { get; init; }
    public double BilledAmount { get; init; }
    public double PaidAmount { get; init; }
    public int OrderCount { get; init; }
    public DateTime? LastOrderDate { get; init; }

    public double DueAmount => Math.Max(BilledAmount - PaidAmount, 0.0);
    public bool HasDue => DueAmount > PaymentService.Tolerance;
}

/// <summary>
/// Single place that answers "what does this customer still owe": the value billed on confirmed
/// sales orders less payments received against those same orders. The per-customer due shown in
/// the POS modal, the due list page and the dashboard Total Due tile all read through this, so
/// the figures can never disagree.
/// </summary>
public class CustomerDueService
{
    private readonly IQueryContext _context;

    public CustomerDueService(IQueryContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Per-customer receivable roll-up. When <paramref name="customerId"/> is set only that
    /// customer is aggregated; <paramref name="excludeSalesOrderId"/> leaves one order (and its
    /// payments) out so an order being edited is not counted in its own "previous" due.
    /// Customers with no billing and no payment activity are left out.
    /// </summary>
    public async Task<List<CustomerDueAggregate>> GetAggregatesAsync(
        string? customerId = null,
        string? excludeSalesOrderId = null,
        CancellationToken cancellationToken = default)
    {
        var ordersQuery = _context.SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.OrderStatus == SalesOrderStatus.Confirmed);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            ordersQuery = ordersQuery.Where(x => x.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(excludeSalesOrderId))
        {
            ordersQuery = ordersQuery.Where(x => x.Id != excludeSalesOrderId);
        }

        var orders = await ordersQuery
            .Select(x => new { x.Id, x.CustomerId, Billed = x.AfterTaxAmount ?? 0.0, x.OrderDate })
            .ToListAsync(cancellationToken);

        var customerByOrderId = orders
            .Where(x => x.Id != null)
            .ToDictionary(x => x.Id!, x => x.CustomerId);

        var payments = await _context.Payment
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(p =>
                p.Direction == PaymentDirection.Received &&
                p.ModuleName == nameof(SalesOrder) &&
                p.ModuleId != null)
            .Select(p => new { p.ModuleId, Amount = p.Amount ?? 0.0 })
            .ToListAsync(cancellationToken);

        // A payment only counts when its order is part of this roll-up (live, confirmed and not
        // excluded): advances or payments on deleted/draft orders stay out, exactly like the
        // per-document outstanding balance ignores anything outside its own document.
        var billedByCustomer = orders
            .Where(x => x.CustomerId != null)
            .GroupBy(x => x.CustomerId!)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Billed = g.Sum(x => x.Billed),
                    OrderCount = g.Count(),
                    LastOrderDate = g.Max(x => x.OrderDate)
                });

        var paidByCustomer = payments
            .Where(p => customerByOrderId.TryGetValue(p.ModuleId!, out var owner) &&
                        owner != null &&
                        (customerId == null || owner == customerId))
            .GroupBy(p => customerByOrderId[p.ModuleId!]!)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        return billedByCustomer.Keys
            .Union(paidByCustomer.Keys)
            .Select(id => new CustomerDueAggregate
            {
                CustomerId = id,
                BilledAmount = billedByCustomer.GetValueOrDefault(id)?.Billed ?? 0.0,
                PaidAmount = paidByCustomer.GetValueOrDefault(id, 0.0),
                OrderCount = billedByCustomer.GetValueOrDefault(id)?.OrderCount ?? 0,
                LastOrderDate = billedByCustomer.GetValueOrDefault(id)?.LastOrderDate
            })
            .Where(x => x.BilledAmount > 0 || x.PaidAmount > 0)
            .OrderByDescending(x => x.DueAmount)
            .ThenBy(x => x.CustomerId)
            .ToList();
    }

    /// <summary>
    /// Outstanding receivable for one customer, money-rounded. Zero when the customer has no
    /// billing or payment activity.
    /// </summary>
    public async Task<double> GetDueAsync(
        string? customerId,
        string? excludeSalesOrderId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return 0.0;
        }

        var aggregates = await GetAggregatesAsync(customerId, excludeSalesOrderId, cancellationToken);
        return aggregates.FirstOrDefault()?.DueAmount.ToMoney() ?? 0.0;
    }
}
