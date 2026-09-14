using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Application.Common.Tenancy;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public record GetTicketHistoryDto
{
    public string? Id { get; init; }
    public string? Action { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? PerformedById { get; init; }
    public string? PerformedByName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketHistoryResult
{
    public List<GetTicketHistoryDto>? Data { get; init; }
}

public class GetTicketHistoryRequest : IRequest<GetTicketHistoryResult>
{
    public string? TicketId { get; init; }
}

public class GetTicketHistoryValidator : AbstractValidator<GetTicketHistoryRequest>
{
    public GetTicketHistoryValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}

public class GetTicketHistoryHandler : IRequestHandler<GetTicketHistoryRequest, GetTicketHistoryResult>
{
    private readonly IQueryContext _context;
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;
    private readonly ITenantContext _tenantContext;

    public GetTicketHistoryHandler(
        IQueryContext context,
        ICommandRepository<Ticket> ticketRepository,
        ICurrentUserService currentUser,
        ISecurityService securityService,
        ITenantContext tenantContext
        )
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _securityService = securityService;
        _tenantContext = tenantContext;
    }

    public async Task<GetTicketHistoryResult> Handle(GetTicketHistoryRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken);
        var isCrossTenantView = false;

        if (ticket == null && _currentUser.IsInRole(TicketAccessGuard.PlatformRole))
        {
            ticket = await TicketTenantElevation.RunAsync(_tenantContext, elevate: true,
                () => _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken));
            isCrossTenantView = ticket != null;
        }

        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.TicketId}");
        }

        if (!isCrossTenantView)
        {
            var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
            TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);
        }

        return await TicketTenantElevation.RunAsync(_tenantContext, isCrossTenantView, async () =>
        {
            var entries = await _context.TicketHistory.AsNoTracking()
                .Where(x => x.TicketId == request.TicketId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new { x.Id, x.Action, x.OldValue, x.NewValue, x.PerformedById, x.CreatedAtUtc })
                .ToListAsync(cancellationToken);

            var users = await _securityService.GetUserListAsync(cancellationToken);
            var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());

            var dtos = entries.Select(x => new GetTicketHistoryDto
            {
                Id = x.Id,
                Action = x.Action,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                PerformedById = x.PerformedById,
                PerformedByName = x.PerformedById != null && userNames.TryGetValue(x.PerformedById, out var n) ? n : "System",
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList();

            return new GetTicketHistoryResult { Data = dtos };
        });
    }
}
