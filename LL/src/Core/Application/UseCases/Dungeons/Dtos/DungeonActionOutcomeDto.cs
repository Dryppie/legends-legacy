namespace Application.UseCases.Dungeons.Dtos;

public enum DungeonActionOutcomeDto
{
    None = 0,
    CombatVictory = 1,
    CombatDefeat = 2,
    EventResolved = 3,
    RestSiteResolved = 4,
    RunRetreated = 5,
    RunCompleted = 6,
    RunFailed = 7,
    InvalidAction = 8
}
