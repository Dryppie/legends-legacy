using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Attributes.Modifiers;
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
        var existingEquipmentById = await ctx.ItemBases
            .OfType<EquipmentBase>()
            .Include(x => x.AttributeModifiers)
            .ToDictionaryAsync(x => x.Id);
        var existingModifiersById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.AttributeModifiers)
            .ToDictionary(x => x.Id);
        var modifierOwnersById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.AttributeModifiers.Select(modifier => new { modifier.Id, Equipment = equipment }))
            .ToDictionary(x => x.Id, x => x.Equipment);
        var desiredModifierIds = items
            .OfType<EquipmentBase>()
            .SelectMany(x => x.AttributeModifiers.Select(modifier => modifier.Id))
            .ToHashSet();

        foreach (var item in items)
        {
            if (item is EquipmentBase equipment)
            {
                foreach (var attr in equipment.AttributeModifiers)
                {
                    attr.ItemBaseId = item.Id;
                }

                if (!existingEquipmentById.TryGetValue(item.Id, out var existingEquipment))
                {
                    SyncNewEquipmentAttributeModifiers(
                        equipment,
                        existingModifiersById,
                        modifierOwnersById);
                    ctx.ItemBases.Add(item);
                    continue;
                }

                ctx.GetEntry(existingEquipment).CurrentValues.SetValues(equipment);
                SyncEquipmentAttributeModifiers(
                    ctx,
                    existingEquipment,
                    equipment.AttributeModifiers,
                    existingModifiersById,
                    modifierOwnersById,
                    desiredModifierIds);
                continue;
            }

            var existing = await ctx.ItemBases.FirstOrDefaultAsync(x => x.Id == item.Id);

            if (existing == null) ctx.ItemBases.Add(item);
            else ctx.GetEntry(existing).CurrentValues.SetValues(item);
        }
    }

    private static void SyncEquipmentAttributeModifiers(
        IDbContext ctx,
        EquipmentBase equipment,
        IEnumerable<ItemAttributeModifier> desiredModifiers,
        Dictionary<Guid, ItemAttributeModifier> existingModifiersById,
        Dictionary<Guid, EquipmentBase> modifierOwnersById,
        IReadOnlySet<Guid> desiredModifierIds)
    {
        var desiredById = desiredModifiers.ToDictionary(x => x.Id);

        foreach (var existing in equipment.AttributeModifiers.ToList())
        {
            if (desiredById.Remove(existing.Id, out var desired))
            {
                existing.AttributeType = desired.AttributeType;
                existing.Amount = desired.Amount;
                existing.ModifierType = desired.ModifierType;
                existing.ItemBaseId = equipment.Id;
                continue;
            }

            if (desiredModifierIds.Contains(existing.Id))
                continue;

            equipment.AttributeModifiers.Remove(existing);
            ctx.GetEntry(existing).State = EntityState.Deleted;
            existingModifiersById.Remove(existing.Id);
            modifierOwnersById.Remove(existing.Id);
        }

        foreach (var modifier in desiredById.Values)
        {
            UpsertEquipmentAttributeModifier(
                equipment,
                modifier,
                existingModifiersById,
                modifierOwnersById);
        }
    }

    private static void SyncNewEquipmentAttributeModifiers(
        EquipmentBase equipment,
        Dictionary<Guid, ItemAttributeModifier> existingModifiersById,
        Dictionary<Guid, EquipmentBase> modifierOwnersById)
    {
        var desiredModifiers = equipment.AttributeModifiers.ToList();
        equipment.AttributeModifiers.Clear();

        foreach (var modifier in desiredModifiers)
        {
            UpsertEquipmentAttributeModifier(
                equipment,
                modifier,
                existingModifiersById,
                modifierOwnersById);
        }
    }

    private static void UpsertEquipmentAttributeModifier(
        EquipmentBase equipment,
        ItemAttributeModifier desired,
        Dictionary<Guid, ItemAttributeModifier> existingModifiersById,
        Dictionary<Guid, EquipmentBase> modifierOwnersById)
    {
        if (existingModifiersById.TryGetValue(desired.Id, out var existing))
        {
            existing.AttributeType = desired.AttributeType;
            existing.Amount = desired.Amount;
            existing.ModifierType = desired.ModifierType;
            existing.ItemBaseId = equipment.Id;

            if (modifierOwnersById.TryGetValue(existing.Id, out var previousOwner) &&
                !ReferenceEquals(previousOwner, equipment))
            {
                previousOwner.AttributeModifiers.Remove(existing);
            }

            if (!equipment.AttributeModifiers.Contains(existing))
                equipment.AttributeModifiers.Add(existing);

            modifierOwnersById[existing.Id] = equipment;
            return;
        }

        desired.ItemBaseId = equipment.Id;
        equipment.AttributeModifiers.Add(desired);
        existingModifiersById[desired.Id] = desired;
        modifierOwnersById[desired.Id] = equipment;
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
