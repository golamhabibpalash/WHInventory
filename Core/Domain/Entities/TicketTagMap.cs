using Domain.Common;

namespace Domain.Entities;

/// <summary>Join row: one ticket carrying one tag.</summary>
public class TicketTagMap : BaseEntity
{
    public string? TicketId { get; set; }
    public string? TicketTagId { get; set; }
}
