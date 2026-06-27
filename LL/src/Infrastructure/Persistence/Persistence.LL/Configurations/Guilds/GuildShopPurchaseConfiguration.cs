using Domain.Models.Guilds.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildShopPurchaseConfiguration : IEntityTypeConfiguration<GuildShopPurchase>
{
    public void Configure(EntityTypeBuilder<GuildShopPurchase> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.CharacterId, x.ShopItemKey, x.PeriodKey }).IsUnique();
    }
}
