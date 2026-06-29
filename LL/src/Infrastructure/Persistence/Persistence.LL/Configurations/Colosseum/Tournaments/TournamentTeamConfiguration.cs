using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentTeamConfiguration : IEntityTypeConfiguration<TournamentTeam>
{
    public void Configure(EntityTypeBuilder<TournamentTeam> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasIndex(x => new { x.TournamentId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.TournamentId, x.Seed });
        builder.HasIndex(x => new { x.TournamentId, x.Status });
        builder.HasIndex(x => x.OwnerParticipantId);
    }
}
