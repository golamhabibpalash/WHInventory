namespace Application.Features.TicketManager;

/// <summary>
/// Ticketing's own attachment whitelist/size limit — deliberately separate from
/// FileDocumentHelper.AllowedBulkExtensions (which only covers office documents) rather than
/// widening that shared list, so this change can't affect any other module's upload behavior.
/// Tickets legitimately need screenshots and log files that Products/etc. never asked for.
/// </summary>
public static class TicketAttachmentHelper
{
    public const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv",
        ".png", ".jpg", ".jpeg", ".gif", ".log"
    };

    public static bool IsExtensionAllowed(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return AllowedExtensions.Contains(normalized);
    }
}
