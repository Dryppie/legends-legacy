using Domain.Models.Colosseum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum;

public sealed class CharacterArenaProfileConfiguration : IEntityTypeConfiguration<CharacterArenaProfile>
{
    public void Configure(EntityTypeBuilder<CharacterArenaProfile> builder)
    {
        builder.HasKey(x => x.CharacterId);

        builder
            .HasOne(x => x.Character)
            .WithOne(x => x.ArenaProfile)
            .HasForeignKey<CharacterArenaProfile>(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => x.Rating);
    }
}
