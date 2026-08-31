using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.PaymentManager.Services;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentManager.Commands;

public class CreatePaymentResult
{
    public Payment? Data { get; set; }
    public PaymentSummary? Summary { get; set; }
}

public class CreatePaymentRequest : IRequest<CreatePaymentResult>
{
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
    public DateTime? PaymentDate { get; init; }
    public double? Amount { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.ModuleName).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.Amount)
            .NotEmpty()
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
    }
}

public class CreatePaymentHandler : IRequestHandler<CreatePaymentRequest, CreatePaymentResult>
{
    private readonly ICommandRepository<Payment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentService _paymentService;
    private readonly NumberSequenceService _numberSequenceService;

    public CreatePaymentHandler(
        ICommandRepository<Payment> repository,
        IUnitOfWork unitOfWork,
        PaymentService paymentService,
        NumberSequenceService numberSequenceService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreatePaymentResult> Handle(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var summary = await _paymentService.GetSummaryAsync(
            request.ModuleName, request.ModuleId, null, cancellationToken);

        var amount = (request.Amount ?? 0).ToMoney();

        if (amount > summary.AmountOutstanding + PaymentService.Tolerance)
        {
            throw new Exception(
                $"Payment of {amount:N2} exceeds the outstanding balance on {summary.ModuleNumber} " +
                $"({summary.AmountOutstanding:N2} of {summary.DocumentTotal:N2}). " +
                $"Reduce the amount or record it against another document.");
        }

        var entity = new Payment
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(Payment), "", "PAY"),
            ModuleName = request.ModuleName,
            ModuleId = request.ModuleId,
            ModuleNumber = summary.ModuleNumber,
            PaymentDate = request.PaymentDate,
            Direction = PaymentService.DirectionFor(request.ModuleName),
            Amount = amount,
            PaymentMethodId = request.PaymentMethodId,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim(),
            Notes = request.Notes
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentResult
        {
            Data = entity,
            Summary = await _paymentService.GetSummaryAsync(
                request.ModuleName, request.ModuleId, null, cancellationToken)
        };
    }
}
