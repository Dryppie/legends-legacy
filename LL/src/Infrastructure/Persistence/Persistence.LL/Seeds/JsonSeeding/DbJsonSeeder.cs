using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
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
            .Include(x => x.ToolBonuses)
            .ToDictionaryAsync(x => x.Id);
        var existingModifiersById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.AttributeModifiers)
            .ToDictionary(x => x.Id);
        var existingToolBonusesById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.ToolBonuses)
            .ToDictionary(x => x.Id);
        var modifierOwnersById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.AttributeModifiers.Select(modifier => new { modifier.Id, Equipment = equipment }))
            .ToDictionary(x => x.Id, x => x.Equipment);
        var toolBonusOwnersById = existingEquipmentById.Values
            .SelectMany(equipment => equipment.ToolBonuses.Select(bonus => new { bonus.Id, Equipment = equipment }))
            .ToDictionary(x => x.Id, x => x.Equipment);
        var desiredModifierIds = items
            .OfType<EquipmentBase>()
            .SelectMany(x => x.AttributeModifiers.Select(modifier => modifier.Id))
            .ToHashSet();
        var desiredToolBonusIds = items
            .OfType<EquipmentBase>()
            .SelectMany(x => x.ToolBonuses.Select(bonus => bonus.Id))
            .ToHashSet();

        foreach (var item in items)
        {
            if (item is EquipmentBase equipment)
            {
                foreach (var attr in equipment.AttributeModifiers)
                {
                    attr.ItemBaseId = item.Id;
                }

                foreach (var bonus in equipment.ToolBonuses)
                {
                    bonus.EquipmentBaseId = item.Id;
                }

                if (!existingEquipmentById.TryGetValue(item.Id, out var existingEquipment))
                {
                    SyncNewEquipmentAttributeModifiers(
                        equipment,
                        existingModifiersById,
                        modifierOwnersById);
                    SyncNewToolBonuses(
                        equipment,
                        existingToolBonusesById,
                        toolBonusOwnersById);
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
                SyncToolBonuses(
                    ctx,
                    existingEquipment,
                    equipment.ToolBonuses,
                    existingToolBonusesById,
                    toolBonusOwnersById,
                    desiredToolBonusIds);
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

    private static void SyncToolBonuses(
        IDbContext ctx,
        EquipmentBase equipment,
        IEnumerable<ToolBonusModifier> desiredBonuses,
        Dictionary<Guid, ToolBonusModifier> existingBonusesById,
        Dictionary<Guid, EquipmentBase> bonusOwnersById,
        IReadOnlySet<Guid> desiredBonusIds)
    {
        var desiredById = desiredBonuses.ToDictionary(x => x.Id);

        foreach (var existing in equipment.ToolBonuses.ToList())
        {
            if (desiredById.Remove(existing.Id, out var desired))
            {
                existing.BonusType = desired.BonusType;
                existing.Name = desired.Name;
                existing.Amount = desired.Amount;
                existing.ScopeId = desired.ScopeId;
                existing.EquipmentBaseId = equipment.Id;
                continue;
            }

            if (desiredBonusIds.Contains(existing.Id))
                continue;

            equipment.ToolBonuses.Remove(existing);
            ctx.GetEntry(existing).State = EntityState.Deleted;
            existingBonusesById.Remove(existing.Id);
            bonusOwnersById.Remove(existing.Id);
        }

        foreach (var bonus in desiredById.Values)
        {
            UpsertToolBonus(
                equipment,
                bonus,
                existingBonusesById,
                bonusOwnersById);
        }
    }

    private static void SyncNewToolBonuses(
        EquipmentBase equipment,
        Dictionary<Guid, ToolBonusModifier> existingBonusesById,
        Dictionary<Guid, EquipmentBase> bonusOwnersById)
    {
        var desiredBonuses = equipment.ToolBonuses.ToList();
        equipment.ToolBonuses.Clear();

        foreach (var bonus in desiredBonuses)
        {
            UpsertToolBonus(
                equipment,
                bonus,
                existingBonusesById,
                bonusOwnersById);
        }
    }

    private static void UpsertToolBonus(
        EquipmentBase equipment,
        ToolBonusModifier desired,
        Dictionary<Guid, ToolBonusModifier> existingBonusesById,
        Dictionary<Guid, EquipmentBase> bonusOwnersById)
    {
        if (existingBonusesById.TryGetValue(desired.Id, out var existing))
        {
            existing.BonusType = desired.BonusType;
            existing.Name = desired.Name;
            existing.Amount = desired.Amount;
            existing.ScopeId = desired.ScopeId;
            existing.EquipmentBaseId = equipment.Id;

            if (bonusOwnersById.TryGetValue(existing.Id, out var previousOwner) &&
                !ReferenceEquals(previousOwner, equipment))
            {
                previousOwner.ToolBonuses.Remove(existing);
            }

            if (!equipment.ToolBonuses.Contains(existing))
                equipment.ToolBonuses.Add(existing);

            bonusOwnersById[existing.Id] = equipment;
            return;
        }

        desired.EquipmentBaseId = equipment.Id;
        equipment.ToolBonuses.Add(desired);
        existingBonusesById[desired.Id] = desired;
        bonusOwnersById[desired.Id] = equipment;
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
                    weight: 32),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000011"),
                    itemId: "pickaxe",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000012"),
                    itemId: "woodcutting_hatchet",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000013"),
                    itemId: "fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000014"),
                    itemId: "skinning_knife",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000015"),
                    itemId: "rare_pickaxe",
                    weight: 1),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0001-000000000016"),
                    itemId: "rare_hatchet",
                    weight: 1)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000002"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000001"),
                    itemId: "advancement_stone",
                    weight: 25),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000011"),
                    itemId: "rare_pickaxe",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000012"),
                    itemId: "rare_hatchet",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000013"),
                    itemId: "rare_fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000014"),
                    itemId: "rare_skinning_knife",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000015"),
                    itemId: "epic_pickaxe",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000016"),
                    itemId: "epic_hatchet",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000017"),
                    itemId: "epic_fishing_rod",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0002-000000000018"),
                    itemId: "epic_skinning_knife",
                    weight: 1.5f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000003"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000001"),
                    itemId: "advancement_stone",
                    weight: 18),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000011"),
                    itemId: "epic_pickaxe",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000012"),
                    itemId: "epic_hatchet",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000013"),
                    itemId: "epic_fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000014"),
                    itemId: "epic_skinning_knife",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000015"),
                    itemId: "unique_pickaxe",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000016"),
                    itemId: "unique_hatchet",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000017"),
                    itemId: "unique_fishing_rod",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000018"),
                    itemId: "unique_skinning_knife",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000019"),
                    itemId: "legendary_pickaxe",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000020"),
                    itemId: "legendary_hatchet",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000021"),
                    itemId: "legendary_fishing_rod",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000022"),
                    itemId: "legendary_skinning_knife",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000023"),
                    itemId: "legacy_pickaxe",
                    weight: 0.2f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000024"),
                    itemId: "legacy_hatchet",
                    weight: 0.2f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000025"),
                    itemId: "legacy_fishing_rod",
                    weight: 0.2f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0003-000000000026"),
                    itemId: "legacy_skinning_knife",
                    weight: 0.2f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000101"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000001"),
                    itemId: "stoneguard_ring",
                    weight: 10),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000011"),
                    itemId: "pickaxe",
                    weight: 8),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000012"),
                    itemId: "fishing_rod",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000013"),
                    itemId: "rare_pickaxe",
                    weight: 1),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0101-000000000014"),
                    itemId: "rare_fishing_rod",
                    weight: 0.75f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000102"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0102-000000000001"),
                    itemId: "rare_pickaxe",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0102-000000000002"),
                    itemId: "rare_fishing_rod",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0102-000000000003"),
                    itemId: "epic_pickaxe",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0102-000000000004"),
                    itemId: "epic_fishing_rod",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0102-000000000005"),
                    itemId: "unique_pickaxe",
                    weight: 0.4f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000103"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000001"),
                    itemId: "epic_pickaxe",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000002"),
                    itemId: "epic_fishing_rod",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000003"),
                    itemId: "unique_pickaxe",
                    weight: 2.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000004"),
                    itemId: "unique_fishing_rod",
                    weight: 1.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000005"),
                    itemId: "legendary_pickaxe",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0103-000000000006"),
                    itemId: "legacy_pickaxe",
                    weight: 0.2f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000201"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0201-000000000001"),
                    itemId: "skinning_knife",
                    weight: 8),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0201-000000000002"),
                    itemId: "fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0201-000000000003"),
                    itemId: "rare_skinning_knife",
                    weight: 1),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0201-000000000004"),
                    itemId: "rare_fishing_rod",
                    weight: 0.75f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000202"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0202-000000000001"),
                    itemId: "rare_skinning_knife",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0202-000000000002"),
                    itemId: "rare_fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0202-000000000003"),
                    itemId: "epic_skinning_knife",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0202-000000000004"),
                    itemId: "epic_fishing_rod",
                    weight: 1.25f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0202-000000000005"),
                    itemId: "unique_skinning_knife",
                    weight: 0.4f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000203"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000001"),
                    itemId: "epic_skinning_knife",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000002"),
                    itemId: "epic_fishing_rod",
                    weight: 4),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000003"),
                    itemId: "unique_skinning_knife",
                    weight: 2.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000004"),
                    itemId: "unique_fishing_rod",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000005"),
                    itemId: "legendary_skinning_knife",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0203-000000000006"),
                    itemId: "legacy_skinning_knife",
                    weight: 0.2f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000301"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0301-000000000001"),
                    itemId: "woodcutting_hatchet",
                    weight: 8),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0301-000000000002"),
                    itemId: "skinning_knife",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0301-000000000003"),
                    itemId: "rare_hatchet",
                    weight: 1),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0301-000000000004"),
                    itemId: "rare_skinning_knife",
                    weight: 0.75f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000302"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0302-000000000001"),
                    itemId: "rare_hatchet",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0302-000000000002"),
                    itemId: "rare_skinning_knife",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0302-000000000003"),
                    itemId: "epic_hatchet",
                    weight: 2),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0302-000000000004"),
                    itemId: "epic_skinning_knife",
                    weight: 1.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0302-000000000005"),
                    itemId: "unique_hatchet",
                    weight: 0.4f)
            ]);

        await AddLootTableIfMissing(
            ctx,
            lootTableId: Guid.Parse("10000000-0000-0000-0000-000000000303"),
            entries:
            [
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000001"),
                    itemId: "epic_hatchet",
                    weight: 7),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000002"),
                    itemId: "epic_skinning_knife",
                    weight: 5),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000003"),
                    itemId: "unique_hatchet",
                    weight: 2.5f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000004"),
                    itemId: "unique_skinning_knife",
                    weight: 1.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000005"),
                    itemId: "legendary_hatchet",
                    weight: 0.75f),
                CreateLootTableItem(
                    id: Guid.Parse("10000000-0000-0000-0303-000000000006"),
                    itemId: "legacy_hatchet",
                    weight: 0.2f)
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
