using Application.Common.Services.SubscriptionManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SubscriptionRequestManager.Commands;

public class RejectSubscriptionRequestResult
{
    public SubscriptionRequestDto? Data { get; set; }
}

/// <summary>Platform-only: reject a tenant's plan request, leaving its current subscription unchanged.</summary>
public class RejectSubscriptionRequestRequest : IRequest<RejectSubscriptionRequestResult>, ISubscriptionExempt
{
    public string? Id { get; init; }
    public string? DecisionNote { get; init; }
    public string? UpdatedById { get; init; }
}

public class RejectSubscriptionRequestValidator : AbstractValidator<RejectSubscriptionRequestRequest>
{
    public RejectSubscriptionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class RejectSubscriptionRequestHandler : IRequestHandler<RejectSubscriptionRequestRequest, RejectSubscriptionRequestResult>
{
    private readonly ISubscriptionRequestService _service;

    public RejectSubscriptionRequestHandler(ISubscriptionRequestService service)
    {
        _service = service;
    }

    public async Task<RejectSubscriptionRequestResult> Handle(RejectSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.RejectAsync(request.Id!, request.UpdatedById, request.DecisionNote, cancellationToken);
        return new RejectSubscriptionRequestResult { Data = data };
    }
}
