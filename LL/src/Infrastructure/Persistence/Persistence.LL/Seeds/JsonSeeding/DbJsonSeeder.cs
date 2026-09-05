using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
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
        NormalizeEssenceItemMappings(items);
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

    private static void NormalizeEssenceItemMappings(IEnumerable<ItemBase> items)
    {
        var essenceItems = items.OfType<EssenceItemBase>().ToList();

        foreach (var item in essenceItems)
        {
            var definitionId = item.ResolveDefinitionId();
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new InvalidOperationException(
                    $"Essence item '{item.Id}' must declare essenceDefinitionId or use the 'item.essence.*' convention.");
            }

            item.EssenceDefinitionId = definitionId;
        }

        var duplicateMapping = essenceItems
            .GroupBy(item => item.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Skip(1).Any());

        if (duplicateMapping is not null)
        {
            var itemIds = string.Join(", ", duplicateMapping.Select(item => item.Id).Order(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(
                $"Multiple Essence items map to '{duplicateMapping.Key}': {itemIds}.");
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

}
