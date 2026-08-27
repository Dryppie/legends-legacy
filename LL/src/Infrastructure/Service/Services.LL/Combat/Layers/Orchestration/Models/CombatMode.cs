namespace Services.LL.Combat.Layers.Orchestration.Models;

using Domain.Models.Essences;

public enum CombatMode
{
    Idle = 1,
    Dungeon = 2,
    Raid = 3,
    Pvp = 4,
    RegionBoss = 5
}

public enum CombatContentType
{
    Idle = 1,
    Dungeon = 2,
    Arena = 3,
    Tournament = 4,
    Raid = 5,
    WorldTower = 6,
    RegionBoss = 7,
    QuestTraining = 8
}

public static class CombatContentTypeExtensions
{
    public static CombatMode ToCombatMode(this CombatContentType contentType) => contentType switch
    {
        CombatContentType.Idle => CombatMode.Idle,
        CombatContentType.Dungeon => CombatMode.Dungeon,
        CombatContentType.Arena or CombatContentType.Tournament => CombatMode.Pvp,
        CombatContentType.Raid or CombatContentType.WorldTower => CombatMode.Raid,
        CombatContentType.RegionBoss => CombatMode.RegionBoss,
        CombatContentType.QuestTraining => CombatMode.Idle,
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unknown combat content type.")
    };

    public static EssenceCombatActivity ToEssenceActivity(this CombatContentType contentType) => contentType switch
    {
        CombatContentType.Idle => EssenceCombatActivity.IdleCombat,
        CombatContentType.Dungeon => EssenceCombatActivity.Dungeon,
        CombatContentType.Arena => EssenceCombatActivity.Arena,
        CombatContentType.Tournament => EssenceCombatActivity.Tournament,
        CombatContentType.Raid => EssenceCombatActivity.Raid,
        CombatContentType.WorldTower => EssenceCombatActivity.WorldTower,
        CombatContentType.RegionBoss => EssenceCombatActivity.RegionBoss,
        CombatContentType.QuestTraining => EssenceCombatActivity.IdleCombat,
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unknown combat content type.")
    };
}
