using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantManager.Queries;

public class GetTenantUserListResult
{
    public List<GetUserListResultDto>? Data { get; init; }
}

/// <summary>Platform-only: the users belonging to a given tenant.</summary>
public class GetTenantUserListRequest : IRequest<GetTenantUserListResult>
{
    public string? TenantId { get; init; }
}

public class GetTenantUserListValidator : AbstractValidator<GetTenantUserListRequest>
{
    public GetTenantUserListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class GetTenantUserListHandler : IRequestHandler<GetTenantUserListRequest, GetTenantUserListResult>
{
    private readonly ISecurityService _securityService;

    public GetTenantUserListHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<GetTenantUserListResult> Handle(GetTenantUserListRequest request, CancellationToken cancellationToken)
    {
        var data = await _securityService.GetTenantUserListAsync(request.TenantId!, cancellationToken);
        return new GetTenantUserListResult { Data = data };
    }
}
