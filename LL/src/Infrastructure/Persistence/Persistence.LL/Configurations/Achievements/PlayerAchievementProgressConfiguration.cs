using Domain.Models.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Achievements;

public sealed class PlayerAchievementProgressConfiguration : IEntityTypeConfiguration<PlayerAchievementProgress>
{
    public void Configure(EntityTypeBuilder<PlayerAchievementProgress> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .HasOne(x => x.AchievementDefinition)
            .WithMany()
            .HasForeignKey(x => x.AchievementDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new
            {
                x.AccountId,
                x.CharacterId,
                x.AchievementDefinitionId,
                x.SeasonId
            })
            .IsUnique();

        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
    }
}
