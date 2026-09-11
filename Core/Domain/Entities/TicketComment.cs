using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// One entry in a ticket's conversation. IsInternal marks a note visible only to users holding
/// the TicketAgent role — never returned to the requester by any query in this module.
/// </summary>
public class TicketComment : BaseEntity
{
    public string? TicketId { get; set; }
    public string? AuthorId { get; set; }
    public string? Message { get; set; }
    public bool IsInternal { get; set; }
}
