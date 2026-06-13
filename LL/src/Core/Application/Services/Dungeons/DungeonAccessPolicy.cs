using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;

namespace Application.Services.Dungeons;

public sealed class DungeonAccessPolicy : IDungeonAccessPolicy
{
    private readonly IDungeonRunRepository _dungeonRuns;

    public DungeonAccessPolicy(IDungeonRunRepository dungeonRuns)
    {
        _dungeonRuns = dungeonRuns;
    }

    public async Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        int currentCombatRating,
        CancellationToken cancellationToken)
    {
        var missingRequirements = new List<string>();

        if (currentCombatRating < dungeon.MinimumCombatRating)
        {
            missingRequirements.Add($"Requires {dungeon.MinimumCombatRating} Combat Rating.");
        }

        if (!string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
            && !await _dungeonRuns.HasCompletedDungeonAsync(
                characterId,
                dungeon.RequiredPreviousDungeonId,
                cancellationToken))
        {
            missingRequirements.Add("Complete the previous difficulty first.");
        }

        return new DungeonAccessResult(
            missingRequirements.Count == 0,
            missingRequirements,
            currentCombatRating,
            dungeon.MinimumCombatRating);
    }
}
