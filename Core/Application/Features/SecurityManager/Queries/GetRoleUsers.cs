using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetRoleUsersResult
{
    public List<RoleUserDto>? Data { get; init; }
}

public class GetRoleUsersRequest : IRequest<GetRoleUsersResult>
{
    public string? RoleName { get; init; }
}

public class GetRoleUsersValidator : AbstractValidator<GetRoleUsersRequest>
{
    public GetRoleUsersValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

public class GetRoleUsersHandler : IRequestHandler<GetRoleUsersRequest, GetRoleUsersResult>
{
    private readonly ISecurityService _securityService;

    public GetRoleUsersHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<GetRoleUsersResult> Handle(GetRoleUsersRequest request, CancellationToken cancellationToken)
    {
        var result = await _securityService.GetRoleUsersAsync(
            request.RoleName ?? "",
            cancellationToken
        );

        return new GetRoleUsersResult
        {
            Data = result
        };
    }
}
