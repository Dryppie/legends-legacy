using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Dungeons;

public sealed class DungeonCompletionRecordConfiguration : IEntityTypeConfiguration<DungeonCompletionRecord>
{
    public void Configure(EntityTypeBuilder<DungeonCompletionRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DungeonDefinitionId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => new { x.CharacterId, x.DungeonDefinitionId })
            .IsUnique();
    }
}
