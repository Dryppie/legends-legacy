using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonCheckpointService
{
    IReadOnlyList<DungeonCheckpointChoiceOption> EnsureChoices(DungeonRun run);
    DungeonCheckpointChoiceResult ApplyChoice(DungeonRun run, RoomInstance room, string choiceId);
}

public sealed class DungeonCheckpointChoiceResult
{
    public required DungeonCheckpointChoiceOption Choice { get; init; }
    public DungeonCheckpointChoiceOutcome Outcome { get; init; }
}

public enum DungeonCheckpointChoiceOutcome
{
    Withdraw = 0,
    Focus = 1,
    PushDeeper = 2,
    Rest = 3
}
