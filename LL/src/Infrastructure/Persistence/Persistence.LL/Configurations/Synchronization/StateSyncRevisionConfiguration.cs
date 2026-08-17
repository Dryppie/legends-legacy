using Domain.Models.Synchronization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Synchronization;

public sealed class StateSyncRevisionConfiguration : IEntityTypeConfiguration<StateSyncRevision>
{
    public void Configure(EntityTypeBuilder<StateSyncRevision> builder)
    {
        builder.ToTable("StateSyncRevisions");
        builder.HasKey(x => x.ScopeKey);
        builder.Property(x => x.ScopeKey).HasMaxLength(180);
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
