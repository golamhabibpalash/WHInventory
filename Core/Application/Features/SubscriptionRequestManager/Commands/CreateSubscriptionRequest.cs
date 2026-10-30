using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.SubscriptionManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SubscriptionRequestManager.Commands;

public class CreateSubscriptionRequestResult
{
    public SubscriptionChangeRequest? Data { get; set; }
}

/// <summary>
/// A tenant asks to move onto a different plan. Exempt from read-only enforcement so a lapsed
/// tenant can still request an upgrade — the very action that gets them reinstated.
/// </summary>
public class CreateSubscriptionRequestRequest : IRequest<CreateSubscriptionRequestResult>, ISubscriptionExempt
{
    public string? RequestedPlanId { get; init; }
    public string? RequestNote { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequestRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.RequestedPlanId).NotEmpty();
    }
}

public class CreateSubscriptionRequestHandler : IRequestHandler<CreateSubscriptionRequestRequest, CreateSubscriptionRequestResult>
{
    private readonly ICommandRepository<SubscriptionChangeRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public CreateSubscriptionRequestHandler(
        ICommandRepository<SubscriptionChangeRequest> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<CreateSubscriptionRequestResult> Handle(CreateSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        // One open request at a time — the ambient tenant filter scopes this to the caller.
        var hasPending = await _queryContext.SubscriptionChangeRequest
            .AsNoTracking()
            .AnyAsync(x => x.Status == SubscriptionRequestStatus.Pending && x.IsDeleted == false, cancellationToken);

        if (hasPending)
        {
            throw new ValidationException("You already have a pending plan request. Cancel it before making another.");
        }

        var plan = await _queryContext.SubscriptionPlan
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RequestedPlanId && x.IsDeleted == false, cancellationToken)
            ?? throw new ValidationException("The selected plan is not available.");

        if (!plan.IsActive)
        {
            throw new ValidationException("The selected plan is not open for new subscriptions.");
        }

        // Current plan code for the approver's context — the tenant filter returns only our own row.
        var currentPlanCode = await _queryContext.TenantSubscription
            .AsNoTracking()
            .Where(x => x.IsDeleted == false)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.PlanCode)
            .FirstOrDefaultAsync(cancellationToken);

        var entity = new SubscriptionChangeRequest
        {
            RequestedPlanId = plan.Id,
            RequestedPlanName = plan.Name,
            RequestedPlanCode = plan.Code,
            CurrentPlanCode = currentPlanCode,
            Status = SubscriptionRequestStatus.Pending,
            RequestNote = request.RequestNote,
            CreatedById = request.CreatedById
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateSubscriptionRequestResult { Data = entity };
    }
}
