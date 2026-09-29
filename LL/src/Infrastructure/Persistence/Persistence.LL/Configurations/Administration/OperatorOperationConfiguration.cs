using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;
public sealed class OperatorOperationConfiguration : IEntityTypeConfiguration<OperatorOperation>
{
    public void Configure(EntityTypeBuilder<OperatorOperation> b)
    {
        b.ToTable("OperatorOperations");
        b.HasKey(x => new { x.ActorSubject, x.Environment, x.OperationId });
        b.Property(x => x.ActorSubject).HasMaxLength(320);
        b.Property(x => x.Environment).HasMaxLength(80);
        b.Property(x => x.Kind).HasMaxLength(80);
        b.Property(x => x.Source).HasMaxLength(16);
        b.Property(x => x.TargetKind).HasMaxLength(24);
        b.Property(x => x.Outcome).HasMaxLength(24);
        b.HasIndex(x => new { x.ActorSubject, x.Environment, x.UpdatedAt, x.OperationId });
    }
}
