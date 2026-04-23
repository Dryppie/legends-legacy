namespace Domain.Models.Dungeons.Runs;


public enum DungeonActionOutcome
{
    None = 0,
    CombatVictory = 1,
    CombatDefeat = 2,
    EventResolved = 3,
    CheckpointResolved = 4,
    RunAbandoned = 5,
    RunCompleted = 6
}