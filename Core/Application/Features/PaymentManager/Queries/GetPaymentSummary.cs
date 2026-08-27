using Application.Features.PaymentManager.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentManager.Queries;

public class GetPaymentSummaryResult
{
    public PaymentSummary? Data { get; init; }
}

public class GetPaymentSummaryRequest : IRequest<GetPaymentSummaryResult>
{
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
}

public class GetPaymentSummaryValidator : AbstractValidator<GetPaymentSummaryRequest>
{
    public GetPaymentSummaryValidator()
    {
        RuleFor(x => x.ModuleName).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
    }
}

public class GetPaymentSummaryHandler : IRequestHandler<GetPaymentSummaryRequest, GetPaymentSummaryResult>
{
    private readonly PaymentService _paymentService;

    public GetPaymentSummaryHandler(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<GetPaymentSummaryResult> Handle(GetPaymentSummaryRequest request, CancellationToken cancellationToken)
    {
        return new GetPaymentSummaryResult
        {
            Data = await _paymentService.GetSummaryAsync(
                request.ModuleName, request.ModuleId, null, cancellationToken)
        };
    }
}
