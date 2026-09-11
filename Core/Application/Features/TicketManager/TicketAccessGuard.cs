using Domain.Entities;

namespace Application.Features.TicketManager;

/// <summary>
/// Centralizes the module's three role names and its one resource-level rule: a ticket is
/// visible/actionable by its requester or by anyone holding the agent role — nobody else, tenant
/// membership aside (that part is already enforced for free by the EF global query filter, since
/// Ticket extends BaseEntity/IHasTenant).
/// </summary>
public static class TicketAccessGuard
{
    /// <summary>Raise/view own tickets. Granted to any user who should be able to use Ticketing at all.</summary>
    public const string RequesterRole = "Tickets";

    /// <summary>Manage all tickets: assign, change status/priority, internal notes, resolve/close/reopen.</summary>
    public const string AgentRole = "TicketAgent";

    /// <summary>Manage categories/priorities/tags.</summary>
    public const string ConfigRole = "TicketConfigurations";

    public static bool CanAccess(Ticket ticket, string? currentUserId, bool isAgent)
        => isAgent || (!string.IsNullOrEmpty(ticket.RequesterId) && ticket.RequesterId == currentUserId);

    public static void EnsureCanAccess(Ticket ticket, string? currentUserId, bool isAgent)
    {
        if (!CanAccess(ticket, currentUserId, isAgent))
        {
            throw new Exception("You do not have access to this ticket.");
        }
    }

    public static void EnsureIsAgent(bool isAgent)
    {
        if (!isAgent)
        {
            throw new Exception("This action requires the ticket agent role.");
        }
    }
}
