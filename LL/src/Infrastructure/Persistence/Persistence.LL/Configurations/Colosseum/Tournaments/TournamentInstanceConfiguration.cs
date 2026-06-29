using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentInstanceConfiguration : IEntityTypeConfiguration<TournamentInstance>
{
    public void Configure(EntityTypeBuilder<TournamentInstance> builder)
    {
        builder.ToTable("ArenaTournaments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.HasOne(x => x.Definition).WithMany().HasForeignKey(x => x.DefinitionId);
        builder.HasIndex(x => x.TournamentNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.RegistrationStartsAtUtc, x.RegistrationEndsAtUtc });
        builder.HasIndex(x => x.StartsAtUtc);
        builder.HasIndex(x => x.DefinitionId);
    }
}
