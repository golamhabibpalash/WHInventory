using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class QuickShortcutConfiguration : BaseEntityConfiguration<QuickShortcut>
{
    public override void Configure(EntityTypeBuilder<QuickShortcut> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Icon).HasMaxLength(LengthConsts.S).IsRequired(false);
        builder.Property(x => x.Url).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired(true);
    }
}
