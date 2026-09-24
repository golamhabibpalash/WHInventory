using System.Net;
using Application.Common.Services.EmailManager;
using Application.Features.NotificationManager;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Features.TicketManager;

/// <summary>
/// Ticket-specific notification sender. Email (via the app's existing IEmailService) remains
/// the off-site surface; each event below additionally drops a persistent in-app notification
/// (INotificationService) that reaches the user live over SignalR and survives in the bell.
/// A send failure on either channel is logged and swallowed, never thrown: the ticket
/// operation that triggered it has already succeeded and must not be undone by a
/// downstream SMTP/SignalR problem (same rule TenantProvisioningService's
/// SendConfirmationEmailAsync already follows).
/// </summary>
public class TicketNotificationService
{
    private readonly IEmailService _emailService;
    private readonly INotificationService _inAppService;
    private readonly ILogger<TicketNotificationService> _logger;

    public TicketNotificationService(
        IEmailService emailService,
        INotificationService inAppService,
        ILogger<TicketNotificationService> logger)
    {
        _emailService = emailService;
        _inAppService = inAppService;
        _logger = logger;
    }

    public Task NotifyTicketCreatedAsync(string? toEmail, string ticketNumber, string subject, string? userId = null, string? ticketId = null, string? actorId = null)
    {
        _ = SendSafeAsync(toEmail, $"Ticket {ticketNumber} created",
            $"<p>Your ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been created. We'll be in touch soon.</p>");

        return NotifyInAppSafeAsync(userId, $"Ticket {ticketNumber} created",
            $"Your ticket \"{subject}\" has been created. We'll be in touch soon.",
            NotificationSeverity.Success, TicketLink(ticketId), ticketId, actorId);
    }

    public Task NotifyTicketAssignedAsync(string? toEmail, string ticketNumber, string subject, string? userId = null, string? ticketId = null, string? actorId = null)
    {
        _ = SendSafeAsync(toEmail, $"Ticket {ticketNumber} assigned to you",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been assigned to you.</p>");

        return NotifyInAppSafeAsync(userId, $"Ticket {ticketNumber} assigned to you",
            $"Ticket \"{subject}\" has been assigned to you.",
            NotificationSeverity.Warning, TicketLink(ticketId), ticketId, actorId);
    }

    public Task NotifyTicketRepliedAsync(string? toEmail, string ticketNumber, string subject, string? userId = null, string? ticketId = null, string? actorId = null)
    {
        _ = SendSafeAsync(toEmail, $"New reply on ticket {ticketNumber}",
            $"<p>There is a new reply on ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\".</p>");

        return NotifyInAppSafeAsync(userId, $"New reply on ticket {ticketNumber}",
            $"There is a new reply on ticket \"{subject}\".",
            NotificationSeverity.Info, TicketLink(ticketId), ticketId, actorId);
    }

    public Task NotifyTicketResolvedAsync(string? toEmail, string ticketNumber, string subject, string? userId = null, string? ticketId = null, string? actorId = null)
    {
        _ = SendSafeAsync(toEmail, $"Ticket {ticketNumber} resolved",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been marked as resolved. Reply if you'd like it reopened.</p>");

        return NotifyInAppSafeAsync(userId, $"Ticket {ticketNumber} resolved",
            $"Ticket \"{subject}\" has been marked as resolved.",
            NotificationSeverity.Success, TicketLink(ticketId), ticketId, actorId);
    }

    public Task NotifyTicketReopenedAsync(string? toEmail, string ticketNumber, string subject, string? userId = null, string? ticketId = null, string? actorId = null)
    {
        _ = SendSafeAsync(toEmail, $"Ticket {ticketNumber} reopened",
            $"<p>Ticket <strong>{Encode(ticketNumber)}</strong> — \"{Encode(subject)}\" — has been reopened.</p>");

        return NotifyInAppSafeAsync(userId, $"Ticket {ticketNumber} reopened",
            $"Ticket \"{subject}\" has been reopened.",
            NotificationSeverity.Warning, TicketLink(ticketId), ticketId, actorId);
    }

    private static string? TicketLink(string? ticketId) =>
        string.IsNullOrWhiteSpace(ticketId) ? null : $"/Tickets/TicketDetails?id={ticketId}";

    private async Task NotifyInAppSafeAsync(
        string? userId,
        string title,
        string message,
        NotificationSeverity severity,
        string? linkUrl,
        string? ticketId,
        string? actorId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        try
        {
            await _inAppService.NotifyAsync(userId, title, message, severity, linkUrl, "Ticket", ticketId, actorId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ticket in-app notification failed: {Title}", title);
        }
    }

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
