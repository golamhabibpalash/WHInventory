using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.CustomerManager.Services;
using Application.Features.NumberSequenceManager;
using Application.Features.PaymentManager.Services;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Commands;

public class CollectCustomerDueResult
{
    public int PaymentsCreated { get; set; }
    public double AmountCollected { get; set; }
    public double RemainingDue { get; set; }
}

public class CollectCustomerDueRequest : IRequest<CollectCustomerDueResult>
{
    public string? CustomerId { get; init; }
    public double? Amount { get; init; }
    public string? PaymentMethodId { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? ReferenceNumber { get; init; }
    /// <summary>Leaves one order out of the collection (e.g. the order open for edit in the POS,
    /// whose balance is shown separately), so "Pay Due" there settles only the previous due.</summary>
    public string? ExcludeSalesOrderId { get; init; }
    public string? CreatedById { get; init; }
}

public class CollectCustomerDueValidator : AbstractValidator<CollectCustomerDueRequest>
{
    public CollectCustomerDueValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Amount)
            .NotNull()
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

/// <summary>
/// Collects a payment against a customer's outstanding receivable and spreads it across their
/// confirmed sales orders oldest-first (FIFO), creating one Received payment per order it touches,
/// capped at each order's remaining balance. The amount may not exceed the customer's current due.
/// </summary>
public class CollectCustomerDueHandler : IRequestHandler<CollectCustomerDueRequest, CollectCustomerDueResult>
{
    private readonly IQueryContext _queryContext;
    private readonly ICommandRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly CustomerDueService _customerDueService;

    public CollectCustomerDueHandler(
        IQueryContext queryContext,
        ICommandRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        CustomerDueService customerDueService)
    {
        _queryContext = queryContext;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _customerDueService = customerDueService;
    }

    public async Task<CollectCustomerDueResult> Handle(CollectCustomerDueRequest request, CancellationToken cancellationToken = default)
    {
        var amount = (request.Amount ?? 0).ToMoney();

        var due = await _customerDueService.GetDueAsync(request.CustomerId, request.ExcludeSalesOrderId, cancellationToken);
        if (due <= PaymentService.Tolerance)
        {
            throw new ValidationException("This customer has no outstanding due to collect.");
        }
        if (amount > due + PaymentService.Tolerance)
        {
            throw new ValidationException($"Amount {amount:N2} exceeds the customer's due of {due:N2}.");
        }

        // Confirmed orders for this customer, oldest first, with what has already been paid on each.
        var orders = await _queryContext.SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.CustomerId == request.CustomerId && x.OrderStatus == SalesOrderStatus.Confirmed)
            .Where(x => string.IsNullOrEmpty(request.ExcludeSalesOrderId) || x.Id != request.ExcludeSalesOrderId)
            .OrderBy(x => x.OrderDate)
            .ThenBy(x => x.Number)
            .Select(x => new { x.Id, x.Number, Billed = x.AfterTaxAmount ?? 0.0 })
            .ToListAsync(cancellationToken);

        var orderIds = orders.Where(o => !string.IsNullOrEmpty(o.Id)).Select(o => o.Id!).ToList();

        var paidByOrder = new Dictionary<string, double>();
        if (orderIds.Count > 0)
        {
            var payments = await _queryContext.Payment
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(p => p.Direction == PaymentDirection.Received
                         && p.ModuleName == nameof(SalesOrder)
                         && p.ModuleId != null && orderIds.Contains(p.ModuleId))
                .Select(p => new { p.ModuleId, Amount = p.Amount ?? 0.0 })
                .ToListAsync(cancellationToken);
            paidByOrder = payments
                .GroupBy(p => p.ModuleId!)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
        }

        var remaining = amount;
        var paymentsCreated = 0;

        foreach (var order in orders)
        {
            if (remaining <= PaymentService.Tolerance) break;
            if (string.IsNullOrEmpty(order.Id)) continue;

            var paid = paidByOrder.TryGetValue(order.Id, out var p) ? p : 0.0;
            var outstanding = (order.Billed - paid).ToMoney();
            if (outstanding <= PaymentService.Tolerance) continue;

            var allocation = Math.Min(remaining, outstanding).ToMoney();
            if (allocation <= 0) continue;

            var entity = new Payment
            {
                CreatedById = request.CreatedById,
                Number = _numberSequenceService.GenerateNumber(nameof(Payment), "", "PAY"),
                ModuleName = nameof(SalesOrder),
                ModuleId = order.Id,
                ModuleNumber = order.Number,
                PaymentDate = request.PaymentDate,
                Direction = PaymentDirection.Received,
                Amount = allocation,
                PaymentMethodId = request.PaymentMethodId,
                ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim(),
                Notes = "Due collection"
            };

            await _paymentRepository.CreateAsync(entity, cancellationToken);
            paymentsCreated++;
            remaining = (remaining - allocation).ToMoney();
        }

        if (paymentsCreated > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        var remainingDue = await _customerDueService.GetDueAsync(request.CustomerId, request.ExcludeSalesOrderId, cancellationToken);

        return new CollectCustomerDueResult
        {
            PaymentsCreated = paymentsCreated,
            AmountCollected = (amount - remaining).ToMoney(),
            RemainingDue = remainingDue
        };
    }
}
