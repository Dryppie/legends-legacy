using Domain.Models.CharacterActions.Sessions;

namespace Domain.Models.Dungeons.Runs;

public sealed class ExecuteDungeonActionResult
{
    public required DungeonRun Run { get; init; }
    public required DungeonActionOutcome Outcome { get; init; }
    public CombatSession? CombatSession { get; init; }
    public string? Message { get; init; }
}