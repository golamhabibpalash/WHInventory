using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.SubscriptionManager;
using Application.Common.Tenancy;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SubscriptionManager;

public class SubscriptionRequestService : ISubscriptionRequestService
{
    private readonly ITenantContext _tenantContext;
    private readonly IQueryContext _queryContext;
    private readonly ICommandRepository<SubscriptionChangeRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantSubscriptionAdminService _subscriptionAdminService;

    public SubscriptionRequestService(
        ITenantContext tenantContext,
        IQueryContext queryContext,
        ICommandRepository<SubscriptionChangeRequest> repository,
        IUnitOfWork unitOfWork,
        ITenantSubscriptionAdminService subscriptionAdminService)
    {
        _tenantContext = tenantContext;
        _queryContext = queryContext;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _subscriptionAdminService = subscriptionAdminService;
    }

    public async Task<List<SubscriptionRequestDto>> GetAllAsync(bool pendingOnly, CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var query = _queryContext.SubscriptionChangeRequest
                .AsNoTracking()
                .Where(x => x.IsDeleted == false);

            if (pendingOnly)
            {
                query = query.Where(x => x.Status == SubscriptionRequestStatus.Pending);
            }

            var requests = await query.ToListAsync(cancellationToken);

            var tenants = await _queryContext.Tenant.AsNoTracking()
                .Where(x => x.IsDeleted == false)
                .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

            return requests
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => Map(x, x.TenantId != null && tenants.TryGetValue(x.TenantId, out var t) ? t : null))
                .ToList();
        });
    }

    public async Task<SubscriptionRequestDto> ApproveAsync(string requestId, string? decisionById, string? decisionNote, CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var request = await FindTrackedAsync(requestId, cancellationToken);
            EnsurePending(request);

            if (string.IsNullOrEmpty(request!.RequestedPlanId))
            {
                throw new ValidationException("The request has no plan to apply.");
            }

            // Apply the plan to the tenant's subscription (admin service re-elevates as needed).
            await _subscriptionAdminService.AssignPlanAsync(
                request.TenantId!,
                request.RequestedPlanId!,
                SubscriptionStatus.Active,
                null,
                decisionById,
                cancellationToken);

            request.Status = SubscriptionRequestStatus.Approved;
            request.DecisionById = decisionById;
            request.DecisionAtUtc = DateTime.UtcNow;
            request.DecisionNote = decisionNote;
            request.UpdatedById = decisionById;

            _repository.Update(request);
            await _unitOfWork.SaveAsync(cancellationToken);

            return await MapWithTenantAsync(request, cancellationToken);
        });
    }

    public async Task<SubscriptionRequestDto> RejectAsync(string requestId, string? decisionById, string? decisionNote, CancellationToken cancellationToken = default)
    {
        return await RunAtRootAsync(async () =>
        {
            var request = await FindTrackedAsync(requestId, cancellationToken);
            EnsurePending(request);

            request!.Status = SubscriptionRequestStatus.Rejected;
            request.DecisionById = decisionById;
            request.DecisionAtUtc = DateTime.UtcNow;
            request.DecisionNote = decisionNote;
            request.UpdatedById = decisionById;

            _repository.Update(request);
            await _unitOfWork.SaveAsync(cancellationToken);

            return await MapWithTenantAsync(request, cancellationToken);
        });
    }

    private static void EnsurePending(SubscriptionChangeRequest? request)
    {
        if (request == null)
        {
            throw new ValidationException("Subscription request not found.");
        }
        if (request.Status != SubscriptionRequestStatus.Pending)
        {
            throw new ValidationException($"This request has already been {request.Status.ToString().ToLowerInvariant()}.");
        }
    }

    private async Task<SubscriptionChangeRequest?> FindTrackedAsync(string requestId, CancellationToken cancellationToken)
    {
        return await _repository.GetQuery()
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    private async Task<SubscriptionRequestDto> MapWithTenantAsync(SubscriptionChangeRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _queryContext.Tenant.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TenantId, cancellationToken);
        return Map(request, tenant);
    }

    private async Task<T> RunAtRootAsync<T>(Func<Task<T>> action)
    {
        var callerTenantId = _tenantContext.TenantId;
        var callerWasRoot = _tenantContext.IsRoot;

        try
        {
            _tenantContext.SetRootScope();
            return await action();
        }
        finally
        {
            if (callerWasRoot)
            {
                _tenantContext.SetRootScope();
            }
            else
            {
                _tenantContext.SetTenant(callerTenantId);
            }
        }
    }

    private static SubscriptionRequestDto Map(SubscriptionChangeRequest entity, Tenant? tenant)
    {
        return new SubscriptionRequestDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TenantName = tenant?.Name,
            TenantSlug = tenant?.Slug,
            RequestedPlanId = entity.RequestedPlanId,
            RequestedPlanName = entity.RequestedPlanName,
            RequestedPlanCode = entity.RequestedPlanCode,
            CurrentPlanCode = entity.CurrentPlanCode,
            Status = entity.Status,
            RequestNote = entity.RequestNote,
            DecisionById = entity.DecisionById,
            DecisionAtUtc = entity.DecisionAtUtc,
            DecisionNote = entity.DecisionNote,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }
}
