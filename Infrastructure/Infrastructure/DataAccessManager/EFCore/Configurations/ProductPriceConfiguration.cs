using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductPriceConfiguration : BaseEntityConfiguration<ProductPrice>
{
    public override void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PricePolicyId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.CalculationMethod).IsRequired(true);
        builder.Property(x => x.FixedPrice).IsRequired(false);
        builder.Property(x => x.MarkupPercent).IsRequired(false);
        builder.Property(x => x.MarkupAmount).IsRequired(false);
        builder.Property(x => x.MarginPercent).IsRequired(false);
        builder.Property(x => x.FormulaMultiplier).IsRequired(false);
        builder.Property(x => x.MinimumSellingPrice).IsRequired(false);
        builder.Property(x => x.MaximumDiscountPercent).IsRequired(false);
        builder.Property(x => x.EffectiveFrom).IsRequired(false);
        builder.Property(x => x.EffectiveTo).IsRequired(false);
        builder.Property(x => x.Priority).IsRequired(true).HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired(true).HasDefaultValue(true);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.PricePolicy).WithMany().HasForeignKey(x => x.PricePolicyId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.QuantityBreakList).WithOne(x => x.ProductPrice).HasForeignKey(x => x.ProductPriceId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.PricePolicyId);
    }
}
