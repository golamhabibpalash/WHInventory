using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PricePolicyConfiguration : BaseEntityConfiguration<PricePolicy>
{
    public override void Configure(EntityTypeBuilder<PricePolicy> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Code).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Priority).IsRequired(true).HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired(true).HasDefaultValue(true);
        builder.Property(x => x.EffectiveFrom).IsRequired(false);
        builder.Property(x => x.EffectiveTo).IsRequired(false);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Code);
    }
}
