using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentManager.Services;

public class PaymentSummary
{
    public string? ModuleName { get; set; }
    public string? ModuleId { get; set; }
    public string? ModuleNumber { get; set; }

    /// <summary>Gross value of the document, tax included.</summary>
    public double DocumentTotal { get; set; }

    public double AmountPaid { get; set; }
    public double AmountOutstanding { get; set; }
    public bool IsFullySettled { get; set; }
    public int PaymentCount { get; set; }
}

/// <summary>
/// Answers "what is owed on this document" for any module that accepts payments.
/// Both the summary query and the create/update guards read through this, so the
/// figure the user sees and the figure enforced on save can never disagree.
/// </summary>
public class PaymentService
{
    /// <summary>Rounding slack, so a document settled to the cent is not left a fraction short.</summary>
    public const double Tolerance = 0.005;

    private readonly IQueryContext _context;

    public PaymentService(IQueryContext context)
    {
        _context = context;
    }

    public static PaymentDirection DirectionFor(string? moduleName) =>
        moduleName == nameof(PurchaseOrder) ? PaymentDirection.Paid : PaymentDirection.Received;

    public async Task<PaymentSummary> GetSummaryAsync(
        string? moduleName,
        string? moduleId,
        string? excludePaymentId = null,
        CancellationToken cancellationToken = default)
    {
        var (documentTotal, documentNumber) =
            await GetDocumentTotalAsync(moduleName, moduleId, cancellationToken);

        var paymentQuery = _context.Payment
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == moduleName && x.ModuleId == moduleId);

        if (!string.IsNullOrEmpty(excludePaymentId))
        {
            paymentQuery = paymentQuery.Where(x => x.Id != excludePaymentId);
        }

        var paid = await paymentQuery.SumAsync(x => (double?)x.Amount ?? 0.0, cancellationToken);
        var count = await paymentQuery.CountAsync(cancellationToken);

        var outstanding = documentTotal - paid;

        return new PaymentSummary
        {
            ModuleName = moduleName,
            ModuleId = moduleId,
            ModuleNumber = documentNumber,
            DocumentTotal = documentTotal.ToMoney(),
            AmountPaid = paid.ToMoney(),
            AmountOutstanding = outstanding.ToMoney(),
            IsFullySettled = outstanding <= Tolerance,
            PaymentCount = count
        };
    }

    private async Task<(double Total, string? Number)> GetDocumentTotalAsync(
        string? moduleName, string? moduleId, CancellationToken cancellationToken)
    {
        if (moduleName == nameof(SalesOrder))
        {
            var so = await _context.SalesOrder
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.Id == moduleId)
                .Select(x => new { x.AfterTaxAmount, x.Number })
                .FirstOrDefaultAsync(cancellationToken);

            return (so?.AfterTaxAmount ?? 0, so?.Number);
        }

        if (moduleName == nameof(PurchaseOrder))
        {
            var po = await _context.PurchaseOrder
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.Id == moduleId)
                .Select(x => new { x.AfterTaxAmount, x.Number })
                .FirstOrDefaultAsync(cancellationToken);

            return (po?.AfterTaxAmount ?? 0, po?.Number);
        }

        throw new Exception($"'{moduleName}' does not accept payments.");
    }
}
