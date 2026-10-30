using Application.Common.Services.SubscriptionManager;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantSubscriptionManager.Commands;

public class AssignSubscriptionPlanResult
{
    public TenantSubscriptionDto? Data { get; set; }
}

/// <summary>
/// Platform-only: assign (or re-assign) a plan to a tenant. Marked <see cref="ISubscriptionExempt"/>
/// so it is never blocked by read-only enforcement — it is one of the actions that fixes a lapse.
/// </summary>
public class AssignSubscriptionPlanRequest : IRequest<AssignSubscriptionPlanResult>, ISubscriptionExempt
{
    public string? TenantId { get; init; }
    public string? PlanId { get; init; }
    public SubscriptionStatus Status { get; init; } = SubscriptionStatus.Active;

    /// <summary>Optional explicit period end. When null the service derives it from the plan
    /// (trial length, billing interval, or open-ended for a free plan).</summary>
    public DateTime? CurrentPeriodEndUtc { get; init; }

    public string? UpdatedById { get; init; }
}

public class AssignSubscriptionPlanValidator : AbstractValidator<AssignSubscriptionPlanRequest>
{
    public AssignSubscriptionPlanValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
    }
}

public class AssignSubscriptionPlanHandler : IRequestHandler<AssignSubscriptionPlanRequest, AssignSubscriptionPlanResult>
{
    private readonly ITenantSubscriptionAdminService _adminService;

    public AssignSubscriptionPlanHandler(ITenantSubscriptionAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<AssignSubscriptionPlanResult> Handle(AssignSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var data = await _adminService.AssignPlanAsync(
            request.TenantId!,
            request.PlanId!,
            request.Status,
            request.CurrentPeriodEndUtc,
            request.UpdatedById,
            cancellationToken);

        return new AssignSubscriptionPlanResult { Data = data };
    }
}
