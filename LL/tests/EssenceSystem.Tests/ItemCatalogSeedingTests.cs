using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Seeds.JsonSeeding.JsonConverters;

namespace EssenceSystem.Tests;

public sealed class ItemCatalogSeedingTests
{
    [Fact]
    public async Task Real_catalog_signet_deserializes_and_round_trips_as_misc()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(), new ItemBaseConverter() }
        };
        var catalog = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "items", "items.json"));
        var items = JsonSerializer.Deserialize<List<ItemBase>>(catalog, options)!;
        var signet = Assert.IsType<MiscItemBase>(Assert.Single(items, item => item.Id == "signet"));
        Assert.Equal(ItemType.Misc, signet.ItemType);
        Assert.True(signet.Stackable);
        Assert.False(signet.IsBound);
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ItemBases.Add(signet);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var stored = Assert.IsType<MiscItemBase>(await db.ItemBases.SingleAsync());
        Assert.Equal(ItemType.Misc, stored.ItemType);
        Assert.Equal(ItemType.Misc, db.Model.FindEntityType(typeof(MiscItemBase))!.GetDiscriminatorValue());
    }

    [Fact]
    public void Unknown_catalog_discriminator_still_fails()
    {
        var options = new JsonSerializerOptions { Converters = { new ItemBaseConverter() } };
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ItemBase>("""{"id":"bad","itemType":"Typo"}""", options));
    }
}
