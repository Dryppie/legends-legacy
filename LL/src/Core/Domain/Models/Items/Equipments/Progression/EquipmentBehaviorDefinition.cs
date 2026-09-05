namespace Domain.Models.Items.Equipments.Progression;

public sealed class EquipmentBehaviorDefinition
{
    public string Handedness { get; init; } = string.Empty;
    public string AttackCategory { get; init; } = string.Empty;
    public string RangeCategory { get; init; } = string.Empty;
    public double BasicAttackIntervalMultiplier { get; init; } = 1d;
    public double BasicAttackDamageMultiplier { get; init; } = 1d;
    public string Role { get; init; } = string.Empty;
}
