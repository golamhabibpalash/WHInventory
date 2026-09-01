using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Tenancy;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TenantManager.Commands;

public class UpdateTenantResult
{
    public Tenant? Data { get; init; }
}

public class UpdateTenantRequest : IRequest<UpdateTenantResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Slug { get; init; }
    public bool IsActive { get; init; } = true;
    public string? UpdatedById { get; init; }
}

public class UpdateTenantValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Slug may contain only lowercase letters, digits and hyphens, and must start and end with a letter or digit.");
    }
}

public class UpdateTenantHandler : IRequestHandler<UpdateTenantRequest, UpdateTenantResult>
{
    private readonly ICommandRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public UpdateTenantHandler(
        ICommandRepository<Tenant> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<UpdateTenantResult> Handle(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var slug = request.Slug!.Trim().ToLowerInvariant();

        if (slug != entity.Slug &&
            TenantDefaults.ReservedSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException($"'{slug}' is reserved and cannot be used as a tenant address.");
        }

        var slugTaken = await _queryContext.Tenant
            .AsNoTracking()
            .AnyAsync(x => x.Slug == slug && x.Id != entity.Id && x.IsDeleted == false, cancellationToken);

        if (slugTaken)
        {
            throw new ValidationException($"The address '{slug}' is already in use by another tenant.");
        }

        // The default tenant owns every row that predates multi-tenancy; deactivating it would
        // lock out the installation itself.
        if (entity.Id == TenantDefaults.DefaultTenantId && !request.IsActive)
        {
            throw new ValidationException("The default tenant cannot be deactivated.");
        }

        entity.Name = request.Name!.Trim();
        entity.Slug = slug;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = request.UpdatedById;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateTenantResult { Data = entity };
    }
}
