using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TenantSubscriptionConfiguration : BaseEntityConfiguration<TenantSubscription>
{
    public override void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.PlanId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PlanName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PlanCode).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.BillingInterval).IsRequired();
        builder.Property(x => x.Provider).IsRequired();
        builder.Property(x => x.StripeCustomerId).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.StripeSubscriptionId).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.PlanId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CurrentPeriodEndUtc);
    }
}
