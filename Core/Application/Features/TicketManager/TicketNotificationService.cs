using System.Net;
using Application.Common.Services.EmailManager;
using Microsoft.Extensions.Logging;

namespace Application.Features.TicketManager;

/// <summary>
/// Ticket-specific notification sender, built entirely on the app's existing IEmailService — no
/// in-app notification bell/table exists anywhere in this codebase to reuse or duplicate, so
/// email is the whole notification surface for this iteration. A send failure is logged and
/// swallowed, never thrown: the ticket operation that triggered it has already succeeded and
/// must not be undone by a downstream SMTP problem (same rule TenantProvisioningService's
/// SendConfirmationEmailAsync already follows).
/// </summary>
public class TicketNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TicketNotificationService> _logger;

    public TicketNotificationService(IEmailService emailService, ILogger<TicketNotificationService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public Task NotifyTicketCreatedAsync(string? toEmail, string ticketNumber, string subject) =>
        SendSafeAsync(toEmail, $"Ticket {ticketNumber} created",
            $"<p>Your ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been created. We'll be in touch soon.</p>");

    public Task NotifyTicketAssignedAsync(string? toEmail, string ticketNumber, string subject) =>
        SendSafeAsync(toEmail, $"Ticket {ticketNumber} assigned to you",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been assigned to you.</p>");

    public Task NotifyTicketRepliedAsync(string? toEmail, string ticketNumber, string subject) =>
        SendSafeAsync(toEmail, $"New reply on ticket {ticketNumber}",
            $"<p>There is a new reply on ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\".</p>");

    public Task NotifyTicketResolvedAsync(string? toEmail, string ticketNumber, string subject) =>
        SendSafeAsync(toEmail, $"Ticket {ticketNumber} resolved",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been marked as resolved. Reply if you'd like it reopened.</p>");

    public Task NotifyTicketReopenedAsync(string? toEmail, string ticketNumber, string subject) =>
        SendSafeAsync(toEmail, $"Ticket {ticketNumber} reopened",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been reopened.</p>");

    private async Task SendSafeAsync(string? toEmail, string subject, string html)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            await _emailService.SendEmailAsync(toEmail, subject, html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ticket notification email failed: {Subject}", subject);
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
