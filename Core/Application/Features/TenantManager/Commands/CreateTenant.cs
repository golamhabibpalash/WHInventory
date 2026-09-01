using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.TenantManager;
using Application.Common.Tenancy;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TenantManager.Commands;

public class CreateTenantResult
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public string? AdminEmail { get; init; }
}

public class CreateTenantRequest : IRequest<CreateTenantResult>
{
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
    public string? AdminFirstName { get; init; }
    public string? AdminLastName { get; init; }
    public string? CreatedById { get; init; }

    /// <summary>
    /// Set by public sign-up when the installation requires confirmed email addresses. An
    /// operator creating a tenant by hand leaves it false - they have vouched for the address.
    /// </summary>
    public bool RequireEmailConfirmation { get; init; }
}

public class CreateTenantValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Slug may contain only lowercase letters, digits and hyphens, and must start and end with a letter or digit.");

        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(6);
    }
}

public class CreateTenantHandler : IRequestHandler<CreateTenantRequest, CreateTenantResult>
{
    private readonly ICommandRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;
    private readonly ITenantProvisioningService _provisioningService;

    public CreateTenantHandler(
        ICommandRepository<Tenant> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext,
        ITenantProvisioningService provisioningService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
        _provisioningService = provisioningService;
    }

    public async Task<CreateTenantResult> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug!.Trim().ToLowerInvariant();

        if (TenantDefaults.ReservedSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException($"'{slug}' is reserved and cannot be used as a tenant address.");
        }

        var slugTaken = await _queryContext.Tenant
            .AsNoTracking()
            .AnyAsync(x => x.Slug == slug && x.IsDeleted == false, cancellationToken);

        if (slugTaken)
        {
            throw new ValidationException($"The address '{slug}' is already in use by another tenant.");
        }

        var email = request.AdminEmail!.Trim();

        // Checked before the tenant row exists: a tenant whose administrator could not be created
        // would be unreachable, with no way in to fix it.
        if (!await _provisioningService.IsEmailAvailableAsync(email))
        {
            throw new ValidationException($"'{email}' already belongs to a user. Every account belongs to exactly one tenant, so pick another address.");
        }

        var entity = new Tenant
        {
            Name = request.Name!.Trim(),
            Slug = slug,
            IsActive = true,
            CreatedById = request.CreatedById,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _provisioningService.ProvisionAsync(new ProvisionTenantDto
        {
            TenantId = entity.Id,
            CompanyName = entity.Name!,
            AdminEmail = email,
            AdminPassword = request.AdminPassword!,
            AdminFirstName = request.AdminFirstName,
            AdminLastName = request.AdminLastName,
            RequireEmailConfirmation = request.RequireEmailConfirmation
        }, cancellationToken);

        return new CreateTenantResult
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            AdminEmail = email
        };
    }
}
