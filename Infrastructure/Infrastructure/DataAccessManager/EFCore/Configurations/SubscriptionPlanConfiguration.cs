using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SubscriptionPlanConfiguration : BaseEntityConfiguration<SubscriptionPlan>
{
    public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Code).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Price).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.BillingInterval).IsRequired();
        builder.Property(x => x.TrialDays).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsPublic).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.StripeProductId).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.StripePriceId).HasMaxLength(NameConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Code);
    }
}
