using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Seeds.JsonSeeding.JsonConverters;

namespace EssenceSystem.Tests;

public sealed class ItemBaseDiscriminatorTests
{
    [Fact]
    public async Task Consumable_item_base_round_trips_with_its_discriminator()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(), new ItemBaseConverter() }
        };
        var item = JsonSerializer.Deserialize<ItemBase>(
            """
            {
              "id": "item.test_consumable",
              "name": "Test Consumable",
              "description": "A consumable used to verify EF materialization.",
              "stackable": true,
              "isBound": true,
              "itemType": "Consumable",
              "rarity": "Rare"
            }
            """,
            serializerOptions);

        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var writeContext = new LLDbContext(options))
        {
            writeContext.ItemBases.Add(item!);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new LLDbContext(options);
        var persisted = await readContext.ItemBases.SingleAsync();

        Assert.IsType<ConsumableItemBase>(persisted);
        Assert.Equal(ItemType.Consumable, persisted.ItemType);
    }
}
