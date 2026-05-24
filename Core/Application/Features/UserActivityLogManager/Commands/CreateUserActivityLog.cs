using Application.Common.CQS.Commands;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.UserActivityLogManager.Commands;

public class CreateUserActivityLogResult
{
    public UserActivityLog? Data { get; init; }
}

public class CreateUserActivityLogRequest : IRequest<CreateUserActivityLogResult>
{
    public string? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? ActivityType { get; init; }
    public string? Description { get; init; }
    public string? PageUrl { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public class CreateUserActivityLogValidator : AbstractValidator<CreateUserActivityLogRequest>
{
    public CreateUserActivityLogValidator()
    {
        RuleFor(x => x.ActivityType).NotEmpty();
    }
}

public class CreateUserActivityLogHandler : IRequestHandler<CreateUserActivityLogRequest, CreateUserActivityLogResult>
{
    private readonly ICommandContext _context;

    public CreateUserActivityLogHandler(ICommandContext context)
    {
        _context = context;
    }

    public async Task<CreateUserActivityLogResult> Handle(CreateUserActivityLogRequest request, CancellationToken cancellationToken)
    {
        var entity = new UserActivityLog
        {
            Id = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            UserEmail = request.UserEmail,
            ActivityType = request.ActivityType,
            Description = request.Description,
            PageUrl = request.PageUrl,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.UserActivityLog.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateUserActivityLogResult { Data = entity };
    }
}
