using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.UnitPrice).IsRequired(false);
        builder.Property(x => x.Physical).IsRequired(false);
        builder.Property(x => x.UnitMeasureId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductGroupId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.BrandId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ImageName).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Barcode).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.IsWarrantyApplicable).IsRequired(false);

        builder.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Number);
        builder.HasIndex(e => e.UnitMeasureId);
        builder.HasIndex(e => e.ProductGroupId);
        builder.HasIndex(e => e.BrandId);
        builder.HasIndex(e => e.Barcode);
    }
}

