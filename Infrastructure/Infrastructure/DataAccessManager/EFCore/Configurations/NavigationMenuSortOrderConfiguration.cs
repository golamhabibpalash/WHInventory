using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class NavigationMenuSortOrderConfiguration : BaseEntityConfiguration<NavigationMenuSortOrder>
{
    public override void Configure(EntityTypeBuilder<NavigationMenuSortOrder> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.UserId).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.SortOrderJson).IsRequired(false);
        builder.HasIndex(e => e.UserId);
    }
}
