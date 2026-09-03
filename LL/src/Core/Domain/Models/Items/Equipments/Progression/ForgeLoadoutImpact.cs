using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Sets;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments.Progression;

public sealed record ForgeLoadoutImpact(IReadOnlyDictionary<AttributeType, float> BeforeAttributes,
    IReadOnlyDictionary<AttributeType, float> AfterAttributes, IReadOnlyList<string> BeforeSetBonusIds,
    IReadOnlyList<string> AfterSetBonusIds, IReadOnlyList<string> BeforeAbilityIds, IReadOnlyList<string> AfterAbilityIds)
{
    public static ForgeLoadoutImpact? Project(ForgeContext context, ForgeQuote quote,
        IEnumerable<AttributeModifierBase> essenceModifiers, IEnumerable<EquipmentSetDefinition> setDefinitions)
    {
        if (!context.IsEquipped || quote.After == null || context.Equipment == null) return null;
        var proposed = new EquipmentInstance { Id = context.Equipment.Id, ItemBaseId = context.Equipment.ItemBaseId, ItemBase = context.Equipment.ItemBase };
        proposed.ApplyProgressionData(quote.After);
        var before = context.Character.EquipmentSlots.Where(x => x.EquipmentInstance != null)
            .Select(x => x.EquipmentInstance!).DistinctBy(x => x.Id).OrderBy(x => x.Id).ToArray();
        var after = before.Select(x => x.Id == proposed.Id ? proposed : x).ToArray();
        var definitions = setDefinitions.ToArray();
        var extras = essenceModifiers.ToArray();
        return new(
            Attributes(before), Attributes(after),
            BonusIds(before), BonusIds(after), EquipmentSetBonusResolver.ResolveGrantedAbilityIds(before, definitions),
            EquipmentSetBonusResolver.ResolveGrantedAbilityIds(after, definitions));

        IReadOnlyDictionary<AttributeType, float> Attributes(EquipmentInstance[] equipment)
        {
            // Match character overview projection, including universal base attributes and resources.
            var projected = new Character
            {
                Level = context.Character.Level,
                BaseAttributes = context.Character.BaseAttributes,
                EquipmentSlots = equipment.Select(item => new EquipmentSlot { EquipmentInstanceId = item.Id, EquipmentInstance = item }).ToList()
            };
            AttributeCalculator.CalculateBaseAttributes(projected,
                extras.Concat(EquipmentSetBonusResolver.ResolveAttributeModifiers(equipment, definitions)));
            return projected.BaseCombatAttributes.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        string[] BonusIds(IEnumerable<EquipmentInstance> equipment) => EquipmentSetBonusResolver.Resolve(equipment, definitions)
            .SelectMany(x => x.ActiveBonuses).Select(x => x.Bonus.Id).Order(StringComparer.Ordinal).ToArray();
    }
}
