using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ModuleName).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ModuleId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ModuleNumber).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PaymentDate).IsRequired(false);
        builder.Property(x => x.Direction).IsRequired();
        builder.Property(x => x.Amount).IsRequired(false);
        builder.Property(x => x.PaymentMethodId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Number);
        builder.HasIndex(e => e.ModuleId);
        builder.HasIndex(e => new { e.ModuleName, e.ModuleId });
        builder.HasIndex(e => e.PaymentDate);
    }
}
