using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;
public class GuildResourceConfiguration : IEntityTypeConfiguration<GuildResource>
{
    public void Configure(EntityTypeBuilder<GuildResource> builder)
    {
        builder.HasKey(e => new { e.GuildId, e.Resource });
    }
}
