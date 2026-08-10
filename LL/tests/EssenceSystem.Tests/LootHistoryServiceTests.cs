using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Items.Dtos;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Inventories;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class LootHistoryServiceTests
{
    [Fact]
    public async Task Recent_history_returns_only_the_newest_50_entries_with_timestamps()
    {
        await using var db = CreateDbContext();
        var characterId = Guid.NewGuid();
        var otherCharacterId = Guid.NewGuid();
        var clock = new MutableTimeProvider(
            new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));
        var service = new LootHistoryService(
            db,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            clock);

        for (var quantity = 1; quantity <= 55; quantity++)
        {
            await service.RecordAsync(
                characterId,
                [CreateItem(quantity)],
                "combat-reward",
                "Lumo Ruins",
                CancellationToken.None);
            clock.Advance(TimeSpan.FromMinutes(1));
        }

        await service.RecordAsync(
            otherCharacterId,
            [CreateItem(999)],
            "combat-reward",
            "Shenic Forest",
            CancellationToken.None);

        var entries = await service.GetRecentAsync(characterId, CancellationToken.None);

        Assert.Equal(50, entries.Count);
        Assert.Equal(55, entries[0].Item.Quantity);
        Assert.Equal(6, entries[^1].Item.Quantity);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 9, 12, 54, 0, TimeSpan.Zero),
            entries[0].ReceivedAt);
        Assert.All(entries, entry => Assert.Equal("combat-reward", entry.Source));
        Assert.All(entries, entry => Assert.Equal("Lumo Ruins", entry.Location));
    }

    [Fact]
    public async Task Clear_removes_all_entries_for_the_character_not_only_the_visible_50()
    {
        await using var db = CreateDbContext();
        var characterId = Guid.NewGuid();
        var otherCharacterId = Guid.NewGuid();
        var service = new LootHistoryService(
            db,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            TimeProvider.System);

        var ownEntries = Enumerable.Range(1, 55)
            .Select(quantity => CreateItem(quantity))
            .ToList();
        await service.RecordAsync(
            characterId,
            ownEntries,
            "combat-reward",
            "Lumo Ruins",
            CancellationToken.None);
        await service.RecordAsync(
            otherCharacterId,
            [CreateItem(999)],
            "combat-reward",
            null,
            CancellationToken.None);

        var deleted = await service.ClearAsync(characterId, CancellationToken.None);

        Assert.Equal(55, deleted);
        Assert.Empty(await service.GetRecentAsync(characterId, CancellationToken.None));
        Assert.Single(await service.GetRecentAsync(otherCharacterId, CancellationToken.None));
    }

    [Fact]
    public async Task Equipment_snapshots_allow_polymorphic_metadata_after_regular_properties()
    {
        await using var db = CreateDbContext();
        var characterId = Guid.NewGuid();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var service = new LootHistoryService(db, jsonOptions, TimeProvider.System);
        var equipmentBase = new EquipmentBaseDto
        {
            Id = "test-sword",
            Name = "Test Sword",
            ItemType = ItemType.Equipment,
            Stackable = false,
            EquipmentType = EquipmentType.OneHanded
        };
        var item = new InventoryItemDto
        {
            ItemInstanceId = Guid.NewGuid(),
            Quantity = 1,
            ItemInstance = new EquipmentInstanceDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Test Sword",
                ItemBase = equipmentBase
            }
        };

        await service.RecordAsync(
            characterId,
            [item],
            "combat-reward",
            "Lumo Ruins",
            CancellationToken.None);

        var entry = Assert.Single(
            await service.GetRecentAsync(characterId, CancellationToken.None));
        var equipment = Assert.IsType<EquipmentInstanceDto>(entry.Item.ItemInstance);
        Assert.IsType<EquipmentBaseDto>(equipment.ItemBase);
        Assert.Equal("Test Sword", equipment.DisplayName);
    }

    private static InventoryItemDto CreateItem(int quantity) => new()
    {
        ItemInstanceId = Guid.NewGuid(),
        Quantity = quantity,
        ItemInstance = new ItemInstanceDto
        {
            Id = Guid.NewGuid(),
            ItemBase = new ItemBaseDto
            {
                Id = "test-ore",
                Name = "Test Ore",
                ItemType = ItemType.Resource,
                Stackable = true
            }
        }
    };

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan amount) => now = now.Add(amount);
    }
}
