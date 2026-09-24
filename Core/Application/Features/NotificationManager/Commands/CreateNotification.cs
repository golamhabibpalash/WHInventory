using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.NotificationManager.Commands;

public class CreateNotificationResult
{
    public Notification? Data { get; set; }
}

public class CreateNotificationRequest : IRequest<CreateNotificationResult>
{
    public string? UserId { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Info;
    public string? LinkUrl { get; init; }
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateNotificationValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
    }
}

public class CreateNotificationHandler : IRequestHandler<CreateNotificationRequest, CreateNotificationResult>
{
    private readonly INotificationService _notificationService;

    public CreateNotificationHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<CreateNotificationResult> Handle(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _notificationService.NotifyAsync(
            request.UserId ?? string.Empty,
            request.Title ?? string.Empty,
            request.Message,
            request.Severity,
            request.LinkUrl,
            request.ModuleName,
            request.ModuleId,
            request.CreatedById,
            cancellationToken);

        return new CreateNotificationResult
        {
            Data = entity
        };
    }
}
