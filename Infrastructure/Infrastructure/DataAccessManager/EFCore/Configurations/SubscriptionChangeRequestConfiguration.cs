using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SubscriptionChangeRequestConfiguration : BaseEntityConfiguration<SubscriptionChangeRequest>
{
    public override void Configure(EntityTypeBuilder<SubscriptionChangeRequest> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.RequestedPlanId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.RequestedPlanName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.RequestedPlanCode).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.CurrentPlanCode).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.RequestNote).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DecisionById).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DecisionNote).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequestedPlanId);
    }
}
