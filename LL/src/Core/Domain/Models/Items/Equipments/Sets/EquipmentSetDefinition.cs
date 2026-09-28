using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments.Sets;

public sealed class EquipmentSetDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    // Authoring-budget validation only; unlocked bonuses always grant their authored amount.
    public bool UsesReservedIdentity { get; init; }
    public IReadOnlyList<EquipmentSetBonusDefinition> Bonuses { get; init; } = [];
}

public sealed class EquipmentSetBonusDefinition
{
    public double IdentityBudgetCost { get; init; }
    public string Id { get; init; } = string.Empty;
    public int RequiredEquippedItems { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentSetAttributeModifierDefinition> AttributeModifiers { get; init; } = [];
    public IReadOnlyList<string> GrantedAbilityIds { get; init; } = [];
    public bool Enabled { get; init; } = true;
}

public sealed class EquipmentSetAttributeModifierDefinition
{
    public AttributeType AttributeType { get; init; }
    public float Amount { get; init; }
    public ModifierType ModifierType { get; init; } = ModifierType.Flat;
}
