using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BrandConfiguration : BaseEntityConfiguration<Brand>
{
    public override void Configure(EntityTypeBuilder<Brand> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ImageName).HasMaxLength(PathConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.Number);
    }
}
