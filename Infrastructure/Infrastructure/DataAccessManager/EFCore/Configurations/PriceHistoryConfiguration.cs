using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PriceHistoryConfiguration : BaseEntityConfiguration<PriceHistory>
{
    public override void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductPriceId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PreviousPrice).IsRequired(false);
        builder.Property(x => x.NewPrice).IsRequired(false);
        builder.Property(x => x.ChangedById).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ChangedDate).IsRequired(true);
        builder.Property(x => x.ChangeReason).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.ProductPrice).WithMany().HasForeignKey(x => x.ProductPriceId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.ProductPriceId);
        builder.HasIndex(e => e.ChangedDate);
    }
}
