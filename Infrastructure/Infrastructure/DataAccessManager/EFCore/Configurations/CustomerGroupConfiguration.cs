using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class CustomerGroupConfiguration : BaseEntityConfiguration<CustomerGroup>
{
    public override void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PricePolicyId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.PricePolicy).WithMany().HasForeignKey(x => x.PricePolicyId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Name);
    }
}

