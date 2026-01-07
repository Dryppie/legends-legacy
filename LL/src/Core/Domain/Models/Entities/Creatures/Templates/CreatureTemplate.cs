using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public sealed class CreatureTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public CreatureArchetype Archetype { get; set; } = CreatureArchetype.Bruiser;
    public DamageProfile DamageProfile { get; set; } = DamageProfile.Physical;
    public DefenseProfile DefenseProfile { get; set; } = DefenseProfile.Balanced;
    //public ElementProfileId ElementProfileId { get; set; } = ElementProfileId.Neutral;

    public float SpeedFactor { get; set; } = 1.0f;
    public float HealthFactor { get; set; } = 1.0f;
    public float DamageFactor { get; set; } = 1.0f;
    public float DefenseFactor { get; set; } = 1.0f;

    public bool IsBoss { get; set; }
    public BossRank BossRank { get; set; } = BossRank.None;

    public ICollection<StatOverride> StatOverrides { get; set; } = new List<StatOverride>();

    public ICollection<string> AbilityIds { get; set; } = new List<string>();
}
