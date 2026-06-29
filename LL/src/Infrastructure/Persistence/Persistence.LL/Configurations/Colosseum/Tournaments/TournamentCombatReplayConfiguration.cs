using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentCombatReplayConfiguration : IEntityTypeConfiguration<TournamentCombatReplay>
{
    public void Configure(EntityTypeBuilder<TournamentCombatReplay> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Outcome).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CombatResultJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId);
        builder.HasIndex(x => x.MatchId).IsUnique();
        builder.HasIndex(x => x.CombatSessionId).IsUnique();
        builder.HasIndex(x => x.BattleHistoryId).IsUnique();
        builder.HasIndex(x => new { x.TournamentId, x.MatchId });
    }
}
