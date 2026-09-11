using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketCategoryConfiguration : BaseEntityConfiguration<TicketCategory>
{
    public override void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired(true);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired(true);

        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
    }
}
