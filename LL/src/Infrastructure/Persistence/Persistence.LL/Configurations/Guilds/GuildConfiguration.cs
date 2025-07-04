using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;
public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        //builder.HasOne(g => g.Owner)
        //    .WithOne() // no reverse navigation from Character
        //    .HasForeignKey<Guild>(g => g.OwnerId)
        //    .OnDelete(DeleteBehavior.Restrict);
    }
}
