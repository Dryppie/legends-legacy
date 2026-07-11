using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.JsonSeeding.JsonConverters;

namespace Persistence.LL.Seeds.JsonSeeding;
public static class DbJsonSeeder
{

    public static async Task RunAsync(IDbContext ctx)
    {
        var opt = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(), new ItemBaseConverter() } };
        await SeedBaseItems(ctx, opt);
        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task SeedBaseItems(IDbContext ctx, JsonSerializerOptions opt)
    {
        var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "items", "items.json");
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

}
