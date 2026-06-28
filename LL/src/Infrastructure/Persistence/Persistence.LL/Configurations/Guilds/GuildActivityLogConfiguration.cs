using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildActivityLogConfiguration : IEntityTypeConfiguration<GuildActivityLog>
{
    public void Configure(EntityTypeBuilder<GuildActivityLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>();

        builder.Property(x => x.Message)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.GuildId, x.CreatedAt });

        builder.HasOne(x => x.Guild)
            .WithMany(x => x.ActivityLogs)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
