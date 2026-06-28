using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PromotionConfiguration : BaseEntityConfiguration<Promotion>
{
    public override void Configure(EntityTypeBuilder<Promotion> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Code).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PricePolicyId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PromotionalPrice).IsRequired(false);
        builder.Property(x => x.DiscountPercent).IsRequired(false);
        builder.Property(x => x.StartDate).IsRequired(false);
        builder.Property(x => x.EndDate).IsRequired(false);
        builder.Property(x => x.Priority).IsRequired(true).HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired(true).HasDefaultValue(true);

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.PricePolicy).WithMany().HasForeignKey(x => x.PricePolicyId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Code);
        builder.HasIndex(e => e.ProductId);
    }
}
