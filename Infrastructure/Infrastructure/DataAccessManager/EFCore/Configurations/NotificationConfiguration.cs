using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.UserId).HasMaxLength(UserIdConsts.MaxLength).IsRequired(true);
        builder.Property(x => x.Title).HasMaxLength(NameConsts.MaxLength).IsRequired(true);
        builder.Property(x => x.Message).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.LinkUrl).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ModuleName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ModuleId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsRead).HasDefaultValue(false).IsRequired(true);

        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}
