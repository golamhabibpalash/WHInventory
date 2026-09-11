using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketTagMapConfiguration : BaseEntityConfiguration<TicketTagMap>
{
    public override void Configure(EntityTypeBuilder<TicketTagMap> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TicketId).HasMaxLength(IdConsts.MaxLength).IsRequired(true);
        builder.Property(x => x.TicketTagId).HasMaxLength(IdConsts.MaxLength).IsRequired(true);

        builder.HasIndex(e => new { e.TicketId, e.TicketTagId }).IsUnique();
        builder.HasIndex(e => e.TicketTagId);
    }
}
