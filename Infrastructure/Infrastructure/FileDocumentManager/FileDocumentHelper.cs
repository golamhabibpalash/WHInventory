namespace Infrastructure.FileDocumentManager;

public static class FileDocumentHelper
{
    private static readonly Dictionary<string, string> MimeTypes = new()
    {
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
    };

    public static readonly HashSet<string> AllowedBulkExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    public static string GetMimeType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            throw new Exception($"Extension cannot be null or empty: {nameof(extension)}");

        extension = extension.ToLowerInvariant();

        return MimeTypes.ContainsKey(extension)
            ? MimeTypes[extension]
            : "application/octet-stream";
    }
}

