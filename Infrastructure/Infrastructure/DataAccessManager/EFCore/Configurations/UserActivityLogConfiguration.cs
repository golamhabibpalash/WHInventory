using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
{
    public void Configure(EntityTypeBuilder<UserActivityLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(50).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired(false);
        builder.Property(e => e.UserEmail).HasMaxLength(255).IsRequired(false);
        builder.Property(e => e.ActivityType).HasMaxLength(100).IsRequired(false);
        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired(false);
        builder.Property(e => e.PageUrl).HasMaxLength(2000).IsRequired(false);
        builder.Property(e => e.IpAddress).HasMaxLength(50).IsRequired(false);
        builder.Property(e => e.UserAgent).HasMaxLength(500).IsRequired(false);
        builder.Property(e => e.CreatedAtUtc).IsRequired(false);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ActivityType);
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}
