using Application.Common.Services.FileImageManager;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class UploadProductImageResult
{
    public string? ImageName { get; init; }
}

public class UploadProductImageRequest : IRequest<UploadProductImageResult>
{
    public string? OriginalFileName { get; init; }
    public string? Extension { get; init; }
    public byte[]? Data { get; init; }
    public long? Size { get; init; }
    public string? CreatedById { get; init; }
}

public static class ProductImageConsts
{
    public const long MaxFileSizeInBytes = 1 * 1024 * 1024; // 1 MB
    public static readonly string[] AllowedExtensions = { "png", "jpg", "jpeg" };
}

public class UploadProductImageValidator : AbstractValidator<UploadProductImageRequest>
{
    public UploadProductImageValidator()
    {
        RuleFor(x => x.OriginalFileName)
            .NotEmpty().WithMessage("An image file is required.");

        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("The image file is empty or could not be read.");

        RuleFor(x => x.Size)
            .NotNull().WithMessage("The image file is empty or could not be read.")
            .LessThanOrEqualTo(ProductImageConsts.MaxFileSizeInBytes)
            .WithMessage("Image must be 1 MB or smaller.");

        RuleFor(x => x.Extension)
            .NotEmpty().WithMessage("Only PNG and JPG images are allowed.")
            .Must(ext => ext != null && ProductImageConsts.AllowedExtensions.Contains(ext.ToLowerInvariant()))
            .WithMessage("Only PNG and JPG images are allowed.");
    }
}

public class UploadProductImageHandler : IRequestHandler<UploadProductImageRequest, UploadProductImageResult>
{
    private readonly IFileImageService _fileImageService;

    public UploadProductImageHandler(IFileImageService fileImageService)
    {
        _fileImageService = fileImageService;
    }

    public async Task<UploadProductImageResult> Handle(UploadProductImageRequest request, CancellationToken cancellationToken)
    {
        var imageName = await _fileImageService.UploadAsync(
            request.OriginalFileName,
            request.Extension,
            request.Data,
            request.Size,
            description: "Product image",
            createdById: request.CreatedById,
            cancellationToken: cancellationToken);

        return new UploadProductImageResult { ImageName = imageName };
    }
}
