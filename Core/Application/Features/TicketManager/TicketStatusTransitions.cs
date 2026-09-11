using Domain.Enums;

namespace Application.Features.TicketManager;

/// <summary>
/// The ticket lifecycle's only source of truth for which status change is legal. Every status
/// change in this module — direct ChangeTicketStatus, and the Resolve/Close/Reopen shortcuts —
/// routes through CanTransition so an invalid jump (e.g. New straight to Closed) is rejected the
/// same way everywhere, instead of each call site re-deriving its own rules.
/// </summary>
public static class TicketStatusTransitions
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> Allowed = new()
    {
        [TicketStatus.New] = new[] { TicketStatus.Open, TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.Open] = new[] { TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.InProgress] = new[] { TicketStatus.PendingUser, TicketStatus.PendingInternal, TicketStatus.Resolved, TicketStatus.Cancelled },
        [TicketStatus.PendingUser] = new[] { TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.PendingInternal] = new[] { TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.Resolved] = new[] { TicketStatus.Reopened, TicketStatus.Closed },
        [TicketStatus.Reopened] = new[] { TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.Closed] = new[] { TicketStatus.Reopened },
        [TicketStatus.Cancelled] = Array.Empty<TicketStatus>(),
    };

    public static bool CanTransition(TicketStatus from, TicketStatus to)
    {
        if (from == to) return false;
        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    /// <summary>Throws with a message safe to surface to the UI as-is, matching this app's
    /// convention of plain Exception messages carrying the user-facing text (see e.g.
    /// CreateUnitMeasureHandler's "already exist" check).</summary>
    public static void EnsureCanTransition(TicketStatus from, TicketStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new Exception($"Cannot change ticket status from '{from}' to '{to}'.");
        }
    }
}
