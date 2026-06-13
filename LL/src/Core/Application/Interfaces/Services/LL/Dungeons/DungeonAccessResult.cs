namespace Application.Interfaces.Services.LL.Dungeons;

public sealed record DungeonAccessResult(
    bool CanEnter,
    IReadOnlyList<string> MissingRequirements,
    int CurrentCombatRating,
    int MinimumCombatRating);
