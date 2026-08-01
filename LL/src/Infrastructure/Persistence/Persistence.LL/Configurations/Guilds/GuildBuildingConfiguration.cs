using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildBuildingConfiguration : IEntityTypeConfiguration<GuildBuilding>
{
    public void Configure(EntityTypeBuilder<GuildBuilding> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>();

        builder.HasIndex(x => new { x.GuildId, x.Type })
            .IsUnique();

        builder.HasOne(x => x.Guild)
            .WithMany(x => x.Buildings)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
