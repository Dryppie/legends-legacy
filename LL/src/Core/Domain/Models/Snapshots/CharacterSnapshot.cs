namespace Domain.Models.Snapshots;

public sealed class CharacterSnapshot
{
    // Missing historical fields must deserialize as legacy, never as today's units.
    public int AttributeRulesVersion { get; init; } = Domain.Models.Attributes.AttributeRules.LegacyVersion;
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public string Name { get; init; } = default!;
    public string ImagePath { get; init; } = string.Empty;
    public int Level { get; init; }
    public Domain.Models.CombatStyles.CombatStyleSnapshot? CombatStyle { get; init; }

    public ICollection<EntityAttributeSnapshot> BaseAttributes { get; init; } = [];

    public ICollection<EquipmentSnapshot> Equipment { get; init; } = [];

    public ICollection<EquippedEssenceSnapshot> EquippedEssences { get; init; } = [];
}
