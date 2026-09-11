using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketPriorityConfiguration : BaseEntityConfiguration<TicketPriority>
{
    public override void Configure(EntityTypeBuilder<TicketPriority> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ColorHex).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.Level).HasDefaultValue(0).IsRequired(true);
        builder.Property(x => x.SlaResponseHours).IsRequired(false);
        builder.Property(x => x.SlaResolutionHours).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired(true);

        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
    }
}
