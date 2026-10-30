using Application.Common.Services.SubscriptionManager;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantSubscriptionManager.Commands;

public class ChangeSubscriptionStatusResult
{
    public TenantSubscriptionDto? Data { get; set; }
}

/// <summary>Platform-only: move a tenant's subscription to a new status (e.g. mark Past Due,
/// Suspend, re-Activate), optionally extending the paid period.</summary>
public class ChangeSubscriptionStatusRequest : IRequest<ChangeSubscriptionStatusResult>, ISubscriptionExempt
{
    public string? TenantId { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime? CurrentPeriodEndUtc { get; init; }
    public string? UpdatedById { get; init; }
}

public class ChangeSubscriptionStatusValidator : AbstractValidator<ChangeSubscriptionStatusRequest>
{
    public ChangeSubscriptionStatusValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class ChangeSubscriptionStatusHandler : IRequestHandler<ChangeSubscriptionStatusRequest, ChangeSubscriptionStatusResult>
{
    private readonly ITenantSubscriptionAdminService _adminService;

    public ChangeSubscriptionStatusHandler(ITenantSubscriptionAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<ChangeSubscriptionStatusResult> Handle(ChangeSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var data = await _adminService.ChangeStatusAsync(
            request.TenantId!,
            request.Status,
            request.CurrentPeriodEndUtc,
            request.UpdatedById,
            cancellationToken);

        return new ChangeSubscriptionStatusResult { Data = data };
    }
}
