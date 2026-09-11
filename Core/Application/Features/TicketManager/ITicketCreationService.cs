using Domain.Entities;

namespace Application.Features.TicketManager;

/// <summary>
/// The one door other modules use to raise a ticket. They never touch Ticket, TicketComment, or
/// any other Ticketing entity directly — and Ticketing never references their entity types back,
/// only the generic (module name, entity type name, entity id) triple below — so a future
/// Inventory/Sales change can't break this module by renaming or restructuring its own tables.
///
/// Example call from another module's handler:
/// <code>
/// await _ticketCreationService.CreateSystemTicketAsync(
///     subject: "Stock Transfer Failed",
///     description: $"Stock transfer {transfer.Number} failed between {fromWarehouse} and {toWarehouse}.",
///     categoryName: "Inventory",
///     priorityName: "High",
///     referenceModule: "Inventory",
///     referenceEntityType: nameof(StockTransfer),
///     referenceEntityId: transfer.Id,
///     cancellationToken: cancellationToken);
/// </code>
/// </summary>
public interface ITicketCreationService
{
    /// <summary>
    /// Raises a ticket on behalf of the system rather than a person filling in a form. categoryName
    /// is matched case-insensitively against TicketCategory.Name (falling back to the first active
    /// category if not found, so a caller never fails just because an admin renamed a category);
    /// priorityName likewise against TicketPriority.Name (falling back to the default priority
    /// CreateTicketHandler already applies for a user ticket with none specified).
    /// </summary>
    Task<Ticket> CreateSystemTicketAsync(
        string subject,
        string description,
        string? categoryName = null,
        string? priorityName = null,
        string? referenceModule = null,
        string? referenceEntityType = null,
        string? referenceEntityId = null,
        string? requesterId = null,
        CancellationToken cancellationToken = default);
}
