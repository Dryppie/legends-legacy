using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildRolePermissionConfiguration : IEntityTypeConfiguration<GuildRolePermission>
{
    public void Configure(EntityTypeBuilder<GuildRolePermission> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.Role });
        builder.HasOne(x => x.Guild)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
