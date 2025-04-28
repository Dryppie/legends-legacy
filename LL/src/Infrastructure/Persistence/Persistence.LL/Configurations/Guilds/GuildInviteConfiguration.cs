using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;
public class GuildInviteConfiguration : IEntityTypeConfiguration<GuildInvite>
{
    public void Configure(EntityTypeBuilder<GuildInvite> builder)
    {
        builder.HasOne(x => x.Guild)
               .WithMany(e => e.Invites)
               .HasForeignKey(x => x.GuildId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}