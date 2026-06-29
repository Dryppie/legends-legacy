using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryRankTier).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasOne(x => x.Snapshot).WithMany().HasForeignKey(x => x.SnapshotId);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.TournamentId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.TournamentId, x.AccountId }).IsUnique();
        builder.HasIndex(x => new { x.TournamentId, x.TeamId });
        builder.HasIndex(x => new { x.TournamentId, x.Seed });
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => x.Status);
    }
}
