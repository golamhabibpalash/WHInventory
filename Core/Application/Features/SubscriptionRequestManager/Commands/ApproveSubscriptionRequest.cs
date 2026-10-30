using Application.Common.Services.SubscriptionManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SubscriptionRequestManager.Commands;

public class ApproveSubscriptionRequestResult
{
    public SubscriptionRequestDto? Data { get; set; }
}

/// <summary>Platform-only: approve a tenant's plan request, applying the plan to its subscription.</summary>
public class ApproveSubscriptionRequestRequest : IRequest<ApproveSubscriptionRequestResult>, ISubscriptionExempt
{
    public string? Id { get; init; }
    public string? DecisionNote { get; init; }
    public string? UpdatedById { get; init; }
}

public class ApproveSubscriptionRequestValidator : AbstractValidator<ApproveSubscriptionRequestRequest>
{
    public ApproveSubscriptionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ApproveSubscriptionRequestHandler : IRequestHandler<ApproveSubscriptionRequestRequest, ApproveSubscriptionRequestResult>
{
    private readonly ISubscriptionRequestService _service;

    public ApproveSubscriptionRequestHandler(ISubscriptionRequestService service)
    {
        _service = service;
    }

    public async Task<ApproveSubscriptionRequestResult> Handle(ApproveSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        var data = await _service.ApproveAsync(request.Id!, request.UpdatedById, request.DecisionNote, cancellationToken);
        return new ApproveSubscriptionRequestResult { Data = data };
    }
}
