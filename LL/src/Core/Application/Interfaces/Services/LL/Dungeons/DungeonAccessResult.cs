namespace Application.Interfaces.Services.LL.Dungeons;

public sealed record DungeonAccessResult(
    bool CanEnter,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<DungeonEntryRequirementResult> EntryRequirements);

public sealed record DungeonEntryRequirementResult(
    string ItemId,
    string Name,
    int RequiredAmount,
    int OwnedAmount);
