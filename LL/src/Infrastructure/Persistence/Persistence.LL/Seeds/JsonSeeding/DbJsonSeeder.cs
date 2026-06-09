using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.JsonSeeding.Dtos.Recipes;
using Persistence.LL.Seeds.JsonSeeding.JsonConverters;

namespace Persistence.LL.Seeds.JsonSeeding;
public static class DbJsonSeeder
{

    public static async Task RunAsync(IDbContext ctx)
    {
        //if (await ctx.Recipes.AnyAsync()) return;

        var opt = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(), new ItemBaseConverter() } };
        await SeedBaseItems(ctx, opt);
        await SeedRecipes(ctx, opt);
        await SeedDungeonLootTables(ctx);
        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task SeedBaseItems(IDbContext ctx, JsonSerializerOptions opt)
    {
        var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "items.json");
        var itemJson = await File.ReadAllTextAsync(itemPath);
        var items = JsonSerializer.Deserialize<List<ItemBase>>(itemJson, opt)!;

        foreach (var item in items)
        {
            var existing = await ctx.ItemBases.FirstOrDefaultAsync(x => x.Id == item.Id);

            if (item is EquipmentBase equipment)
            {
                foreach (var attr in equipment.AttributeModifiers)
                {
                    attr.ItemBaseId = item.Id;
                }
            }

            if (existing == null)
            {
                ctx.ItemBases.Add(item); // New item
            }
            else
            {
                ctx.GetEntry(existing).CurrentValues.SetValues(item); // Update existing, but not attributes
            }
        }
    }

    private static async Task SeedRecipes(IDbContext ctx, JsonSerializerOptions opt)
    {
        var recipePath = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var recipeJson = await File.ReadAllTextAsync(recipePath);
        var dtos = JsonSerializer.Deserialize<List<RecipeDto>>(recipeJson, opt)!;

        foreach (var dto in dtos)
        {
            var recipe = dto.ToEntity();
            var existing = await ctx.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existing == null)
            {
                ctx.Recipes.Add(recipe);
            }
            else
            {
                ctx.GetEntry(existing).CurrentValues.SetValues(recipe); // Update existing, but not materials
            }
        }
    }

    private static async Task SeedDungeonLootTables(IDbContext ctx)
    {
        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000001"),
                    itemId: "advancement_stone",
                    weight: 35)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000101"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000001"),
                    itemId: "stoneguard_ring",
                    weight: 10)
            ]);
    }

    private static async Task AddLootTableIfMissing(
        IDbContext ctx,
        Guid lootTableId,
        IReadOnlyCollection<LootTableEntry> entries)
    {
        var existing = await ctx.LootTables
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == lootTableId);

        if (existing is not null)
        {
            SyncLootTableEntries(ctx, existing, entries);
            return;
        }

        ctx.LootTables.Add(new LootTable
        {
            Id = lootTableId,
            Entries = [.. entries]
        });
    }

    private static void SyncLootTableEntries(
        IDbContext ctx,
        LootTable lootTable,
        IReadOnlyCollection<LootTableEntry> entries)
    {
        var desiredEntries = entries.ToDictionary(x => x.Id);

        foreach (var existingEntry in lootTable.Entries.ToList())
        {
            if (desiredEntries.Remove(existingEntry.Id, out var desiredEntry))
            {
                existingEntry.Weight = desiredEntry.Weight;

                if (existingEntry is LootTableItem existingItem &&
                    desiredEntry is LootTableItem desiredItem)
                {
                    existingItem.ItemId = desiredItem.ItemId;
                }

                continue;
            }

            lootTable.Entries.Remove(existingEntry);

            if (existingEntry is LootTableItem item)
            {
                ctx.LootTableItems.Remove(item);
            }
        }

        foreach (var entry in desiredEntries.Values)
        {
            lootTable.Entries.Add(entry);
        }
    }

    private static LootTableItem CreateLootTableItem(Guid id, string itemId, float weight)
    {
        return new LootTableItem
        {
            Id = id,
            ItemId = itemId,
            Weight = weight
        };
    }
}
