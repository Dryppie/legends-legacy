using Domain.Models.Colosseum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum;

public sealed class ChampionMarketPurchaseConfiguration : IEntityTypeConfiguration<ChampionMarketPurchase>
{
    public void Configure(EntityTypeBuilder<ChampionMarketPurchase> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CharacterId, x.ItemId, x.PurchasedAt });
    }
}
