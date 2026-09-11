using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TicketConfiguration : BaseEntityConfiguration<Ticket>
{
    public override void Configure(EntityTypeBuilder<Ticket> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TicketNumber).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Subject).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(LengthConsts.XL).IsRequired(false);
        builder.Property(x => x.CategoryId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PriorityId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).IsRequired(true);
        builder.Property(x => x.Source).IsRequired(true);
        builder.Property(x => x.RequesterId).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.AssignedToId).HasMaxLength(UserIdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ReferenceModule).HasMaxLength(LengthConsts.S).IsRequired(false);
        builder.Property(x => x.ReferenceEntityType).HasMaxLength(LengthConsts.S).IsRequired(false);
        builder.Property(x => x.ReferenceEntityId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        // Ticket numbers are unique within a tenant, not globally — this is also the safety net
        // behind the retry-on-conflict in CreateTicketHandler (see its comment): NumberSequenceService's
        // lock only protects same-scope reentrancy, not two concurrent HTTP requests, so a duplicate
        // number can be *generated*; this index guarantees one can never be *persisted*.
        builder.HasIndex(e => new { e.TenantId, e.TicketNumber }).IsUnique();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequesterId);
        builder.HasIndex(e => e.AssignedToId);
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => e.PriorityId);
        builder.HasIndex(e => e.CreatedAtUtc);
        builder.HasIndex(e => e.LastActivityAtUtc);
        builder.HasIndex(e => new { e.ReferenceModule, e.ReferenceEntityType, e.ReferenceEntityId });
    }
}
