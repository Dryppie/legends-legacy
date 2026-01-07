using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public static class BossProfiles
{
    public static readonly BossProfile Elite = new()
    {
        Rank = BossRank.Elite,
        HealthMultiplier = 1.8f,
        DamageMultiplier = 1.4f,
        DefenseMultiplier = 1.2f,
        SpeedMultiplier = 1.0f,
        CdrMultiplier = 1.0f,
    };

    public static readonly BossProfile Boss = new()
    {
        Rank = BossRank.Boss,
        HealthMultiplier = 3.0f,
        DamageMultiplier = 1.6f,
        DefenseMultiplier = 1.5f,
        SpeedMultiplier = 1.05f,
        CdrMultiplier = 1.2f,
    };

    public static readonly BossProfile RaidBoss = new()
    {
        Rank = BossRank.RaidBoss,
        HealthMultiplier = 6.0f,
        DamageMultiplier = 2.0f,
        DefenseMultiplier = 2.0f,
        SpeedMultiplier = 1.1f,
        CdrMultiplier = 1.3f,
    };

    public static BossProfile Get(BossRank rank) => rank switch
    {
        BossRank.Elite => Elite,
        BossRank.Boss => Boss,
        BossRank.RaidBoss => RaidBoss,
        _ => Elite
    };
}