namespace Domain.Models.Dungeons.Runs;


public enum DungeonActionOutcome
{
    None = 0,
    CombatVictory = 1,
    CombatDefeat = 2,
    EventResolved = 3,
    RestSiteResolved = 4,
    RunRetreated = 5,
    RunCompleted = 6,
    RunFailed = 7
}
