using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

/// <summary>
/// Tenant is the tenant registry itself, so it is deliberately not derived from
/// BaseEntityConfiguration: it carries no TenantId column and is never tenant-filtered.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Ignore(e => e.TenantId);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(e => e.Slug).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired(false);
        builder.Property(e => e.CreatedById).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(e => e.UpdatedAtUtc).IsRequired(false);
        builder.Property(e => e.UpdatedById).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.IsDeleted);
    }
}
