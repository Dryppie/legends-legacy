using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildVaultItemConfiguration : IEntityTypeConfiguration<GuildVaultItem>
{
    public void Configure(EntityTypeBuilder<GuildVaultItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EquipmentInstanceId).IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.BorrowedByCharacterId });

        builder.HasOne(x => x.Guild)
            .WithMany(x => x.VaultItems)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.EquipmentInstance)
            .WithOne(x => x.GuildVaultItem)
            .HasForeignKey<GuildVaultItem>(x => x.EquipmentInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DonatedByCharacter)
            .WithMany()
            .HasForeignKey(x => x.DonatedByCharacterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BorrowedByCharacter)
            .WithMany()
            .HasForeignKey(x => x.BorrowedByCharacterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
