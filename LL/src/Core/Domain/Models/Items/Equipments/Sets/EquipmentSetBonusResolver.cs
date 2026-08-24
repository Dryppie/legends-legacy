using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Items.Equipments.Sets;

public static class EquipmentSetBonusResolver
{
    public static IReadOnlyList<EquipmentSetState> Resolve(
        IEnumerable<EquipmentInstance> equipment,
        IEnumerable<EquipmentSetDefinition> definitions)
    {
        var definitionsById = definitions.ToDictionary(
            definition => definition.Id,
            StringComparer.OrdinalIgnoreCase);

        return equipment
            .DistinctBy(item => item.Id)
            .Where(item => !string.IsNullOrWhiteSpace(item.EquipmentSetId))
            .GroupBy(item => item.EquipmentSetId!, StringComparer.OrdinalIgnoreCase)
            .Select(group => Resolve(group.Key, group.ToArray(), definitionsById))
            .Where(state => state is not null)
            .Cast<EquipmentSetState>()
            .OrderBy(state => state.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<AttributeModifierBase> ResolveAttributeModifiers(
        IEnumerable<EquipmentInstance> equipment,
        IEnumerable<EquipmentSetDefinition> definitions) =>
        Resolve(equipment, definitions)
            .SelectMany(state => state.ActiveBonuses)
            .SelectMany(active => active.Bonus.AttributeModifiers.Select(modifier =>
                (AttributeModifierBase)new EquipmentSetAttributeModifier(
                    active.SetId,
                    active.Bonus.Id,
                    modifier.AttributeType,
                    modifier.Amount,
                    modifier.ModifierType)))
            .ToArray();

    public static IReadOnlyList<string> ResolveGrantedAbilityIds(
        IEnumerable<EquipmentInstance> equipment,
        IEnumerable<EquipmentSetDefinition> definitions) =>
        Resolve(equipment, definitions)
            .SelectMany(state => state.ActiveBonuses)
            .SelectMany(active => active.Bonus.GrantedAbilityIds)
            .Where(abilityId => !string.IsNullOrWhiteSpace(abilityId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static EquipmentSetState? Resolve(
        string setId,
        IReadOnlyList<EquipmentInstance> equipment,
        IReadOnlyDictionary<string, EquipmentSetDefinition> definitions)
    {
        if (!definitions.TryGetValue(setId, out var definition))
            return null;

        var activeBonuses = definition.Bonuses
            .Where(bonus => bonus.Enabled && equipment.Count >= bonus.RequiredEquippedItems)
            .OrderBy(bonus => bonus.RequiredEquippedItems)
            .ThenBy(bonus => bonus.Id, StringComparer.OrdinalIgnoreCase)
            .Select(bonus => new ActiveEquipmentSetBonus(definition.Id, bonus))
            .ToArray();

        return new EquipmentSetState(
            definition,
            equipment.Select(item => item.Id).Order().ToArray(),
            activeBonuses);
    }
}

public sealed record EquipmentSetState(
    EquipmentSetDefinition Definition,
    IReadOnlyList<Guid> EquippedItemInstanceIds,
    IReadOnlyList<ActiveEquipmentSetBonus> ActiveBonuses)
{
    public int EquippedCount => EquippedItemInstanceIds.Count;
}

public sealed record ActiveEquipmentSetBonus(
    string SetId,
    EquipmentSetBonusDefinition Bonus);

public sealed class EquipmentSetAttributeModifier(
    string setId,
    string bonusId,
    Domain.Models.Attributes.AttributeType attributeType,
    float amount,
    ModifierType modifierType = ModifierType.Flat)
    : AttributeModifierBase(attributeType, amount, modifierType)
{
    public string SetId { get; } = setId;
    public string BonusId { get; } = bonusId;
}
