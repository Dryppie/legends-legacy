using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentRoundConfiguration : IEntityTypeConfiguration<TournamentRound>
{
    public void Configure(EntityTypeBuilder<TournamentRound> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasMany(x => x.Matches).WithOne(x => x.Round).HasForeignKey(x => x.RoundId);
        builder.HasIndex(x => new { x.TournamentId, x.RoundNumber }).IsUnique();
        builder.HasIndex(x => new { x.TournamentId, x.Status });
        builder.HasIndex(x => x.StartsAtUtc);
    }
}
