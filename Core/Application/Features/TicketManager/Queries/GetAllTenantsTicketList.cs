using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Application.Common.Tenancy;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public record GetAllTenantsTicketListDto
{
    public string? Id { get; init; }
    public string? TicketNumber { get; init; }
    public string? Subject { get; init; }
    public string? TenantId { get; init; }
    public string? TenantName { get; init; }
    public string? CategoryName { get; init; }
    public string? PriorityName { get; init; }
    public string? PriorityColorHex { get; init; }
    public TicketStatus Status { get; init; }
    public string? RequesterName { get; init; }
    public string? RequesterEmail { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? LastActivityAtUtc { get; init; }
    public DateTime? SlaResolutionDueAtUtc { get; init; }
    public bool IsOverdue { get; init; }
}

public class GetAllTenantsTicketListResult
{
    public List<GetAllTenantsTicketListDto>? Data { get; init; }
    public int TotalCount { get; init; }
}

/// <summary>
/// The platform-wide counterpart to GetTicketList — every tenant's tickets, for a caller holding
/// the "Tenants" role (see TicketAccessGuard.PlatformRole; the same role TenantController itself
/// is gated on). Read-only by design: there is no cross-tenant write path anywhere in this
/// module — a platform admin who also needs to act on a specific tenant's ticket does so as that
/// tenant's own agent, not through this endpoint.
/// </summary>
public class GetAllTenantsTicketListRequest : IRequest<GetAllTenantsTicketListResult>
{
    public string? TenantId { get; init; }
    public TicketStatus? Status { get; init; }
    public string? Search { get; init; }
    public bool OverdueOnly { get; init; }
    public bool ExcludeClosed { get; init; }
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 2000;
}

public class GetAllTenantsTicketListValidator : AbstractValidator<GetAllTenantsTicketListRequest>
{
    public GetAllTenantsTicketListValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 2000);
    }
}

public class GetAllTenantsTicketListHandler : IRequestHandler<GetAllTenantsTicketListRequest, GetAllTenantsTicketListResult>
{
    private readonly IQueryContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public GetAllTenantsTicketListHandler(
        IQueryContext context,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        ISecurityService securityService
        )
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<GetAllTenantsTicketListResult> Handle(GetAllTenantsTicketListRequest request, CancellationToken cancellationToken)
    {
        TicketAccessGuard.EnsureIsAgent(_currentUser.IsInRole(TicketAccessGuard.PlatformRole));

        return await TicketTenantElevation.RunAsync(_tenantContext, elevate: true, async () =>
        {
            var query = _context.Ticket.AsNoTracking().ApplyIsDeletedFilter(false).AsQueryable();

            if (!string.IsNullOrEmpty(request.TenantId))
            {
                query = query.Where(x => x.TenantId == request.TenantId);
            }
            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }
            if (request.ExcludeClosed)
            {
                query = query.Where(x => x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled);
            }
            if (request.OverdueOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(x => x.SlaResolutionDueAtUtc != null && x.SlaResolutionDueAtUtc < now
                    && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled);
            }
            if (!string.IsNullOrEmpty(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(x =>
                    (x.TicketNumber != null && x.TicketNumber.Contains(term)) ||
                    (x.Subject != null && x.Subject.Contains(term)) ||
                    (x.Description != null && x.Description.Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var page = await query
                .OrderByDescending(x => x.LastActivityAtUtc ?? x.CreatedAtUtc)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.TicketNumber,
                    x.Subject,
                    x.TenantId,
                    x.CategoryId,
                    x.PriorityId,
                    x.Status,
                    x.RequesterId,
                    x.AssignedToId,
                    x.CreatedAtUtc,
                    x.LastActivityAtUtc,
                    x.SlaResolutionDueAtUtc
                })
                .ToListAsync(cancellationToken);

            var tenantNames = await _context.Tenant.AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            var categoryNames = await _context.TicketCategory.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            var priorities = await _context.TicketPriority.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => new { x.Name, x.ColorHex }, cancellationToken);
            // Already root-scope aware on its own (see SecurityService.GetUserListAsync), so this
            // returns every tenant's users while elevated, matching the tickets above.
            var users = await _securityService.GetUserListAsync(cancellationToken);
            var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());
            var userEmails = users.ToDictionary(u => u.Id ?? string.Empty, u => u.Email);

            var now2 = DateTime.UtcNow;
            var dtos = page.Select(x => new GetAllTenantsTicketListDto
            {
                Id = x.Id,
                TicketNumber = x.TicketNumber,
                Subject = x.Subject,
                TenantId = x.TenantId,
                TenantName = x.TenantId != null && tenantNames.TryGetValue(x.TenantId, out var tn) ? tn : null,
                CategoryName = x.CategoryId != null && categoryNames.TryGetValue(x.CategoryId, out var cn) ? cn : null,
                PriorityName = x.PriorityId != null && priorities.TryGetValue(x.PriorityId, out var p) ? p.Name : null,
                PriorityColorHex = x.PriorityId != null && priorities.TryGetValue(x.PriorityId, out var p2) ? p2.ColorHex : null,
                Status = x.Status,
                RequesterName = x.RequesterId != null && userNames.TryGetValue(x.RequesterId, out var rn) ? rn : null,
                RequesterEmail = x.RequesterId != null && userEmails.TryGetValue(x.RequesterId, out var re) ? re : null,
                AssignedToName = x.AssignedToId != null && userNames.TryGetValue(x.AssignedToId, out var an) ? an : null,
                CreatedAtUtc = x.CreatedAtUtc,
                LastActivityAtUtc = x.LastActivityAtUtc,
                SlaResolutionDueAtUtc = x.SlaResolutionDueAtUtc,
                IsOverdue = x.SlaResolutionDueAtUtc.HasValue && x.SlaResolutionDueAtUtc.Value < now2
                    && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed && x.Status != TicketStatus.Cancelled
            }).ToList();

            return new GetAllTenantsTicketListResult { Data = dtos, TotalCount = totalCount };
        });
    }
}
