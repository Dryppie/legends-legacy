namespace Application.Interfaces.Services.LL.Dungeons;

public sealed record DungeonAccessResult(
    bool CanEnter,
    string ReadinessState,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<DungeonEntryRequirementResult> EntryRequirements,
    int CurrentCombatRating,
    int MinimumCombatRating,
    int RecommendedCombatRating);

public sealed record DungeonEntryRequirementResult(
    string ItemId,
    string Name,
    int RequiredAmount,
    int OwnedAmount,
    bool ConsumedOnEntry);
