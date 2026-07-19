using Domain.Models.MarketPlaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.MarketPlaces;

public sealed class MarketPlaceListingConfiguration : IEntityTypeConfiguration<MarketPlaceListing>
{
    public void Configure(EntityTypeBuilder<MarketPlaceListing> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => new { x.UnitPrice, x.CreatedAt });
        builder.HasIndex(x => x.ExpiresAt);
        builder.Property(x => x.SellerName).HasMaxLength(64);
    }
}
