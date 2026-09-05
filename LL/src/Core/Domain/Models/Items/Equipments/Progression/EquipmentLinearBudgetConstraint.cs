using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentLinearBudgetConstraint(
    AttributeType EffectiveAttribute,
    double MaximumAddedValue);
