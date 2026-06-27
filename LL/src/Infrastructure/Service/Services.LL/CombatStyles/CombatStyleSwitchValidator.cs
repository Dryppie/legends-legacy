using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleSwitchValidator : ICombatStyleSwitchValidator
{
    private readonly IDungeonRunRepository _dungeonRuns;

    public CombatStyleSwitchValidator(IDungeonRunRepository dungeonRuns)
    {
        _dungeonRuns = dungeonRuns;
    }

    public async Task<CombatStyleSwitchValidationResult> ValidateCanSwitchAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var hasActiveDungeonRun = await _dungeonRuns.HasActiveDungeonRunAsync(characterId, cancellationToken);

        return hasActiveDungeonRun
            ? CombatStyleSwitchValidationResult.Blocked("Cannot switch Combat Style during an active dungeon run.")
            : CombatStyleSwitchValidationResult.Allowed();
    }
}
