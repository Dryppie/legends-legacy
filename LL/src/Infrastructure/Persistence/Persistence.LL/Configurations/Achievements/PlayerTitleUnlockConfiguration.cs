using Domain.Models.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Achievements;

public sealed class PlayerTitleUnlockConfiguration : IEntityTypeConfiguration<PlayerTitleUnlock>
{
    public void Configure(EntityTypeBuilder<PlayerTitleUnlock> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .HasOne(x => x.TitleDefinition)
            .WithMany()
            .HasForeignKey(x => x.TitleDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new
            {
                x.AccountId,
                x.CharacterId,
                x.TitleDefinitionId,
                x.SeasonId
            })
            .IsUnique();

        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
    }
}
