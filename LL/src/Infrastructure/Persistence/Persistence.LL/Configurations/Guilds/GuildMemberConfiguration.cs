using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;
public class GuildMemberConfiguration : IEntityTypeConfiguration<GuildMember>
{
    public void Configure(EntityTypeBuilder<GuildMember> builder)
    {
        builder.HasKey(gm => new { gm.GuildId, gm.CharacterId });

        builder.HasOne(x => x.Guild)
               .WithMany(e => e.Members)
               .HasForeignKey(x => x.GuildId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}