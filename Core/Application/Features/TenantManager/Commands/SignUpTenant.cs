using Application.Common.Services.TenantManager;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantManager.Commands;

public class SignUpTenantResult
{
    public string? Slug { get; init; }
    public string? Name { get; init; }
    public string? AdminEmail { get; init; }

    /// <summary>Whether the new administrator must confirm their address before signing in.</summary>
    public bool EmailConfirmationRequired { get; init; }
}

public class SignUpTenantRequest : IRequest<SignUpTenantResult>
{
    public string? OrganizationName { get; init; }
    public string? Slug { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? ConfirmPassword { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public class SignUpTenantValidator : AbstractValidator<SignUpTenantRequest>
{
    public SignUpTenantValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Address may contain only lowercase letters, digits and hyphens, and must start and end with a letter or digit.");

        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Password and ConfirmPassword do not match.");

        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

public class SignUpTenantHandler : IRequestHandler<SignUpTenantRequest, SignUpTenantResult>
{
    private readonly ISender _sender;
    private readonly ITenantProvisioningService _provisioningService;

    public SignUpTenantHandler(ISender sender, ITenantProvisioningService provisioningService)
    {
        _sender = sender;
        _provisioningService = provisioningService;
    }

    public async Task<SignUpTenantResult> Handle(SignUpTenantRequest request, CancellationToken cancellationToken)
    {
        var policy = _provisioningService.GetSignUpPolicy();

        // Anyone on the internet can reach this, so it stays shut unless an operator opens it.
        if (!policy.PublicSignUpEnabled)
        {
            throw new Exception("Public sign-up is not enabled on this installation. Please contact the administrator for an account.");
        }

        var requireConfirmation = policy.RequireEmailConfirmation;

        // Deliberately routed through CreateTenant rather than repeating its work: the slug rules,
        // the reserved list, the email availability check and the provisioning steps then cannot
        // drift between the operator-created path and this one.
        var created = await _sender.Send(new CreateTenantRequest
        {
            Name = request.OrganizationName,
            Slug = request.Slug,
            AdminEmail = request.Email,
            AdminPassword = request.Password,
            AdminFirstName = request.FirstName,
            AdminLastName = request.LastName,
            RequireEmailConfirmation = requireConfirmation
        }, cancellationToken);

        return new SignUpTenantResult
        {
            Slug = created.Slug,
            Name = created.Name,
            AdminEmail = created.AdminEmail,
            EmailConfirmationRequired = requireConfirmation
        };
    }
}
