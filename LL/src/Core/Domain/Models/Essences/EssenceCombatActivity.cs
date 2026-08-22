namespace Domain.Models.Essences;

[Flags]
public enum EssenceCombatActivity
{
    None = 0,
    IdleCombat = 1 << 0,
    Dungeon = 1 << 1,
    Raid = 1 << 2,
    WorldTower = 1 << 3,
    Arena = 1 << 4,
    Tournament = 1 << 5,
    RegionBoss = 1 << 6
}
