using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketTagConfiguration : BaseEntityConfiguration<TicketTag>
{
    public override void Configure(EntityTypeBuilder<TicketTag> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
    }
}
