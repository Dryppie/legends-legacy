using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentMatchConfiguration : IEntityTypeConfiguration<TournamentMatch>
{
    public void Configure(EntityTypeBuilder<TournamentMatch> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasIndex(x => new { x.TournamentId, x.RoundNumber, x.MatchNumber }).IsUnique();
        builder.HasIndex(x => x.RoundId);
        builder.HasIndex(x => new { x.TournamentId, x.Status });
        builder.HasIndex(x => x.WinnerParticipantId);
    }
}
