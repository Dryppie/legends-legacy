using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class OperatorDraftConfiguration : IEntityTypeConfiguration<OperatorDraft>
{
    public void Configure(EntityTypeBuilder<OperatorDraft> b)
    {
        b.ToTable("OperatorDrafts"); b.HasKey(x => new { x.ActorSubject, x.Key });
        b.Property(x => x.ActorSubject).HasMaxLength(320);
        b.Property(x => x.Key).HasMaxLength(80);
        b.Property(x => x.Content).HasMaxLength(24000).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
    }
}
