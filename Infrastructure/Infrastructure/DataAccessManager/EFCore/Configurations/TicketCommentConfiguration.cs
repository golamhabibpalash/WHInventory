using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketCommentConfiguration : BaseEntityConfiguration<TicketComment>
{
    public override void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TicketId).HasMaxLength(IdConsts.MaxLength).IsRequired(true);
        builder.Property(x => x.AuthorId).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Message).HasMaxLength(LengthConsts.XL).IsRequired(false);
        builder.Property(x => x.IsInternal).HasDefaultValue(false).IsRequired(true);

        builder.HasIndex(e => e.TicketId);
    }
}
