using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketHistoryConfiguration : BaseEntityConfiguration<TicketHistory>
{
    public override void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TicketId).HasMaxLength(IdConsts.MaxLength).IsRequired(true);
        builder.Property(x => x.Action).HasMaxLength(LengthConsts.S).IsRequired(false);
        builder.Property(x => x.OldValue).HasMaxLength(LengthConsts.M).IsRequired(false);
        builder.Property(x => x.NewValue).HasMaxLength(LengthConsts.M).IsRequired(false);
        builder.Property(x => x.PerformedById).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.TicketId);
    }
}
