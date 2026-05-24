using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(255).IsRequired(false);
        builder.Property(e => e.EntityId).HasMaxLength(50).IsRequired(false);
        builder.Property(e => e.OperationType).HasMaxLength(50).IsRequired(false);
        builder.Property(e => e.OldValues).IsRequired(false);
        builder.Property(e => e.NewValues).IsRequired(false);
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired(false);
        builder.Property(e => e.IpAddress).HasMaxLength(50).IsRequired(false);
        builder.Property(e => e.CreatedAtUtc).IsRequired(false);

        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}
