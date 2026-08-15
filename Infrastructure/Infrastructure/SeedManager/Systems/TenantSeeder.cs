using Application.Common.Repositories;
using Application.Common.Tenancy;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

public class TenantSeeder
{
    private readonly ICommandRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TenantSeeder(
        ICommandRepository<Tenant> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Ensures the default tenant exists and returns its id. Existing single-tenant installations
    /// have their data adopted into this tenant by the schema backfill at startup.
    /// </summary>
    public async Task<string> GenerateDataAsync()
    {
        var existing = await _repository.GetAsync(TenantDefaults.DefaultTenantId);
        if (existing != null)
        {
            return existing.Id;
        }

        var entity = new Tenant
        {
            Id = TenantDefaults.DefaultTenantId,
            Name = TenantDefaults.DefaultTenantName,
            Slug = TenantDefaults.DefaultTenantSlug,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repository.CreateAsync(entity);
        await _unitOfWork.SaveAsync();

        return entity.Id;
    }
}
