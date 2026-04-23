namespace Application.UseCases.Dungeons.Dtos;

public enum DungeonActionOutcomeDto
{
    None = 0,
    CombatVictory = 1,
    CombatDefeat = 2,
    EventResolved = 3,
    CheckpointResolved = 4,
    RunAbandoned = 5,
    RunCompleted = 6,
    InvalidAction = 7
}