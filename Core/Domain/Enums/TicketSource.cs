using System.ComponentModel;

namespace Domain.Enums;

/// <summary>
/// How a ticket came to exist. Kept extensible — Api/Integration/Email are named ahead of
/// need so a later automated-channel doesn't require a breaking enum renumber.
/// </summary>
public enum TicketSource
{
    [Description("User")]
    User = 0,
    [Description("System")]
    System = 1,
    [Description("Api")]
    Api = 2,
    [Description("Integration")]
    Integration = 3,
    [Description("Email")]
    Email = 4
}
