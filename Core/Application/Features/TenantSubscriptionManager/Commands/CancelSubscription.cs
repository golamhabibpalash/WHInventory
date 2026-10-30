using Application.Common.Services.SubscriptionManager;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantSubscriptionManager.Commands;

public class CancelSubscriptionResult
{
    public TenantSubscriptionDto? Data { get; set; }
}

/// <summary>Platform-only: cancel a tenant's subscription. The tenant drops to read-only at once.</summary>
public class CancelSubscriptionRequest : IRequest<CancelSubscriptionResult>, ISubscriptionExempt
{
    public string? TenantId { get; init; }
    public string? UpdatedById { get; init; }
}

public class CancelSubscriptionValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class CancelSubscriptionHandler : IRequestHandler<CancelSubscriptionRequest, CancelSubscriptionResult>
{
    private readonly ITenantSubscriptionAdminService _adminService;

    public CancelSubscriptionHandler(ITenantSubscriptionAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<CancelSubscriptionResult> Handle(CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var data = await _adminService.CancelAsync(request.TenantId!, request.UpdatedById, cancellationToken);
        return new CancelSubscriptionResult { Data = data };
    }
}
