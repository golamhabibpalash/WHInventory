using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.SecurityManager;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Commands;

public class CreateTicketResult
{
    public Ticket? Data { get; set; }
}

public class CreateTicketRequest : IRequest<CreateTicketResult>
{
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public string? CategoryId { get; init; }
    public string? PriorityId { get; init; }

    /// <summary>Only an agent's chosen value is honored (see handler) — anyone else always
    /// files as themselves, matching section 21's "requester normally auto-identified" rule.</summary>
    public string? RequesterId { get; init; }

    public TicketSource Source { get; init; } = TicketSource.User;
    public string? ReferenceModule { get; init; }
    public string? ReferenceEntityType { get; init; }
    public string? ReferenceEntityId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateTicketValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class CreateTicketHandler : IRequestHandler<CreateTicketRequest, CreateTicketResult>
{
    private const int MaxNumberGenerationAttempts = 5;

    private readonly ICommandRepository<Ticket> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly TicketNotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecurityService _securityService;

    public CreateTicketHandler(
        ICommandRepository<Ticket> repository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        TicketHistoryRecorder historyRecorder,
        TicketNotificationService notificationService,
        ICurrentUserService currentUser,
        ISecurityService securityService
        )
    {
        _repository = repository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _historyRecorder = historyRecorder;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _securityService = securityService;
    }

    public async Task<CreateTicketResult> Handle(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _queryContext.TicketCategory
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);

        if (category == null || category.IsActive == false)
        {
            throw new Exception("Selected category is not valid.");
        }

        TicketPriority? priority = null;
        if (!string.IsNullOrEmpty(request.PriorityId))
        {
            priority = await _queryContext.TicketPriority
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.PriorityId && !x.IsDeleted, cancellationToken);
        }
        else
        {
            // Default to the lowest Level (most urgent-first ordering) active priority so a
            // ticket is never left without one.
            priority = await _queryContext.TicketPriority
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Level)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Only an agent may file a ticket "as" someone else — everyone else always files as
        // themselves, regardless of what the client sent (never trust a client-supplied identity).
        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        var requesterId = isAgent && !string.IsNullOrEmpty(request.RequesterId)
            ? request.RequesterId
            : (_currentUser.UserId ?? request.CreatedById);

        var entity = new Ticket
        {
            Subject = request.Subject,
            Description = request.Description,
            CategoryId = request.CategoryId,
            PriorityId = priority?.Id,
            Status = TicketStatus.New,
            Source = request.Source,
            RequesterId = requesterId,
            ReferenceModule = request.ReferenceModule,
            ReferenceEntityType = request.ReferenceEntityType,
            ReferenceEntityId = request.ReferenceEntityId,
            LastActivityAtUtc = DateTime.UtcNow,
            CreatedById = request.CreatedById
        };

        if (priority?.SlaResponseHours is int responseHours)
        {
            entity.SlaResponseDueAtUtc = DateTime.UtcNow.AddHours(responseHours);
        }
        if (priority?.SlaResolutionHours is int resolutionHours)
        {
            entity.SlaResolutionDueAtUtc = DateTime.UtcNow.AddHours(resolutionHours);
        }

        entity.TicketNumber = _numberSequenceService.GenerateNumber("Ticket", "TKT-", "", useDate: false, padding: 6);
        await _repository.CreateAsync(entity, cancellationToken);

        // NumberSequenceService's lock only protects reentrancy within the same DI scope, not two
        // concurrent HTTP requests racing the same counter row — so a duplicate TicketNumber can
        // be *generated* under load. The unique (TenantId, TicketNumber) index (see
        // TicketConfiguration) means it can never be *persisted*: a genuine collision surfaces as
        // a save failure here, retried with a freshly generated number a bounded number of times.
        // Only the number is regenerated on retry — the entity is already tracked as Added from
        // above and stays tracked (a failed SaveChanges does not untrack it), so re-adding it here
        // would duplicate the insert once a later attempt succeeds.
        for (var attempt = 1; attempt <= MaxNumberGenerationAttempts; attempt++)
        {
            try
            {
                await _unitOfWork.SaveAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException) when (attempt < MaxNumberGenerationAttempts)
            {
                entity.TicketNumber = _numberSequenceService.GenerateNumber("Ticket", "TKT-", "", useDate: false, padding: 6);
            }
        }

        // Written only once the number is final, and as its own save — writing it before the
        // retry loop risked recording a history row against a TicketNumber that a later retry
        // then changed.
        _historyRecorder.Record(entity.Id, "Created", null, entity.TicketNumber, request.CreatedById);
        await _unitOfWork.SaveAsync(cancellationToken);

        _ = _notificationService.NotifyTicketCreatedAsync(await GetRequesterEmailAsync(requesterId, cancellationToken), entity.TicketNumber ?? string.Empty, entity.Subject ?? string.Empty);

        return new CreateTicketResult { Data = entity };
    }

    private async Task<string?> GetRequesterEmailAsync(string? requesterId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(requesterId)) return null;

        // AspNetUsers is an Identity table, not part of IQueryContext's domain entity set —
        // ISecurityService is this app's existing abstraction over it (same one UserList uses).
        var users = await _securityService.GetUserListAsync(cancellationToken);
        return users.FirstOrDefault(u => u.Id == requesterId)?.Email;
    }
}
