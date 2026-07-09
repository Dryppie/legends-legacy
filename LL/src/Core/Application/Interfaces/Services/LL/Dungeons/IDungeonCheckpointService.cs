using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonCheckpointService
{
    IReadOnlyList<DungeonCheckpointChoiceOption> EnsureChoices(DungeonRun run);
    Task<DungeonCheckpointChoiceResult> ApplyChoiceAsync(DungeonRun run, RoomInstance room, string choiceId, CancellationToken cancellationToken);
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
