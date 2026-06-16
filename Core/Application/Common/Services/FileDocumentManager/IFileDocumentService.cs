namespace Application.Common.Services.FileDocumentManager;

public interface IFileDocumentService
{
    Task<string> UploadAsync(
        string? originalFileName,
        string? docExtension,
        byte[]? fileData,
        long? size,
        string? description = "",
        string? createdById = "",
        string? moduleName = "",
        string? moduleId = "",
        CancellationToken cancellationToken = default);

    Task<byte[]> GetFileAsync(string fileName, CancellationToken cancellationToken = default);
}
