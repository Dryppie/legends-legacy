using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public sealed class BossProfile
{
    public BossRank Rank { get; init; }
    public float HealthMultiplier { get; init; } = 1.0f;
    public float DamageMultiplier { get; init; } = 1.0f;
    public float DefenseMultiplier { get; init; } = 1.0f;
    public float SpeedMultiplier { get; init; } = 1.0f;
    public float CdrMultiplier { get; init; } = 1.0f;
}