using Domain.Models.MarketPlaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.MarketPlaces;

public class MarketPlaceBuyOrderConfiguration : IEntityTypeConfiguration<MarketPlaceBuyOrder>
{
    public void Configure(EntityTypeBuilder<MarketPlaceBuyOrder> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ItemBaseId, x.UnitPrice, x.CreatedAt });
        builder.HasIndex(x => x.BuyerId);
        builder.Property(x => x.BuyerName).HasMaxLength(64);
        builder.Property(x => x.ItemBaseId).HasMaxLength(128);

        builder
            .HasOne(x => x.ItemBase)
            .WithMany()
            .HasForeignKey(x => x.ItemBaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
