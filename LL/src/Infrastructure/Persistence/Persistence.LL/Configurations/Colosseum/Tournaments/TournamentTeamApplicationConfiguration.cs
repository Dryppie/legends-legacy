using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentTeamApplicationConfiguration : IEntityTypeConfiguration<TournamentTeamApplication>
{
    public void Configure(EntityTypeBuilder<TournamentTeamApplication> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
        builder.HasIndex(x => new { x.TeamId, x.ApplicantParticipantId, x.Status });
        builder.HasIndex(x => new { x.TournamentId, x.ApplicantParticipantId });
    }
}
