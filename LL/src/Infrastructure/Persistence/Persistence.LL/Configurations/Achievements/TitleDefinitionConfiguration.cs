using Domain.Models.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Achievements;

public sealed class TitleDefinitionConfiguration : IEntityTypeConfiguration<TitleDefinition>
{
    public void Configure(EntityTypeBuilder<TitleDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasMaxLength(160);
        builder.Property(x => x.Name).HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(600);
        builder.Property(x => x.SourceAchievementKey).HasMaxLength(160);
        builder.Property(x => x.IconKey).HasMaxLength(120);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
    }
}
