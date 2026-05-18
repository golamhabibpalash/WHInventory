using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateAllUserRolesResult
{
    public List<string>? Data { get; set; }
}

public class UpdateAllUserRolesRequest : IRequest<UpdateAllUserRolesResult>
{
    public string? UserId { get; init; }
    public bool? AccessGranted { get; init; }
}

public class UpdateAllUserRolesValidator : AbstractValidator<UpdateAllUserRolesRequest>
{
    public UpdateAllUserRolesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class UpdateAllUserRolesHandler : IRequestHandler<UpdateAllUserRolesRequest, UpdateAllUserRolesResult>
{
    private readonly ISecurityService _securityService;

    public UpdateAllUserRolesHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<UpdateAllUserRolesResult> Handle(UpdateAllUserRolesRequest request, CancellationToken cancellationToken)
    {
        var result = await _securityService.UpdateAllUserRolesAsync(
            request.UserId ?? "",
            request.AccessGranted ?? true,
            cancellationToken
        );

        return new UpdateAllUserRolesResult { Data = result };
    }
}
