using Domain.Models.MarketPlaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.MarketPlaces;

public sealed class MarketPlaceOrderConfiguration : IEntityTypeConfiguration<MarketPlaceOrder>
{
    public void Configure(EntityTypeBuilder<MarketPlaceOrder> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemBaseId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.ItemBaseId, x.PurchasedAt });
        builder.HasIndex(x => new { x.BuyerId, x.PurchasedAt });
        builder.HasIndex(x => new { x.SellerId, x.PurchasedAt });
        builder.HasIndex(x => new { x.BuyerAccountId, x.PurchasedAt });
        builder.HasIndex(x => new { x.SellerAccountId, x.PurchasedAt });

        builder.HasOne(x => x.ItemBase)
            .WithMany()
            .HasForeignKey(x => x.ItemBaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
