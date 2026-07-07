using Domain.Models.Tutorials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Tutorials;

public sealed class CharacterTutorialProgressConfiguration : IEntityTypeConfiguration<CharacterTutorialProgress>
{
    public void Configure(EntityTypeBuilder<CharacterTutorialProgress> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.TutorialId });

        builder.Property(x => x.TutorialId).HasMaxLength(100);
        builder.Property(x => x.CurrentStep).HasMaxLength(100);

        builder.HasIndex(x => x.CurrentStep);
    }
}
