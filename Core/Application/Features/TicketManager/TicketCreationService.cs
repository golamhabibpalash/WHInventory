using Application.Common.CQS.Queries;
using Application.Features.TicketManager.Commands;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager;

public class TicketCreationService : ITicketCreationService
{
    private readonly ISender _sender;
    private readonly IQueryContext _queryContext;

    public TicketCreationService(ISender sender, IQueryContext queryContext)
    {
        _sender = sender;
        _queryContext = queryContext;
    }

    public async Task<Ticket> CreateSystemTicketAsync(
        string subject,
        string description,
        string? categoryName = null,
        string? priorityName = null,
        string? referenceModule = null,
        string? referenceEntityType = null,
        string? referenceEntityId = null,
        string? requesterId = null,
        CancellationToken cancellationToken = default)
    {
        var category = !string.IsNullOrEmpty(categoryName)
            ? await _queryContext.TicketCategory.AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.IsActive && x.Name != null && x.Name.ToLower() == categoryName.ToLower(), cancellationToken)
            : null;
        category ??= await _queryContext.TicketCategory.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new Exception("No active ticket category is configured — cannot raise a system ticket.");
        }

        var priority = !string.IsNullOrEmpty(priorityName)
            ? await _queryContext.TicketPriority.AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.IsActive && x.Name != null && x.Name.ToLower() == priorityName.ToLower(), cancellationToken)
            : null;

        var result = await _sender.Send(new CreateTicketRequest
        {
            Subject = subject,
            Description = description,
            CategoryId = category.Id,
            PriorityId = priority?.Id,
            RequesterId = requesterId,
            Source = TicketSource.System,
            ReferenceModule = referenceModule,
            ReferenceEntityType = referenceEntityType,
            ReferenceEntityId = referenceEntityId,
            CreatedById = requesterId
        }, cancellationToken);

        return result.Data!;
    }
}
