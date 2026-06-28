using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class QuantityBreakConfiguration : BaseEntityConfiguration<QuantityBreak>
{
    public override void Configure(EntityTypeBuilder<QuantityBreak> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductPriceId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.MinQuantity).IsRequired(true).HasDefaultValue(1.0);
        builder.Property(x => x.MaxQuantity).IsRequired(false);
        builder.Property(x => x.Price).IsRequired(true).HasDefaultValue(0.0);

        builder.HasOne(x => x.ProductPrice).WithMany(x => x.QuantityBreakList).HasForeignKey(x => x.ProductPriceId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ProductPriceId);
    }
}
