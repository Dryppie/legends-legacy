using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEngineExecutor
{
    Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken);

    Task<CombatResult> ExecuteSimulationAsync(
        CombatEncounterRuntime runtime,
        CombatSimulationOptions options,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("This combat executor does not support isolated simulation.");
}

public sealed record CombatSimulationOptions(
    int RandomSeed,
    int MaxTicks = 1800,
    bool StartActiveAbilitiesOnCooldown = true,
    IReadOnlyList<AbilitySpec>? SupplementalAbilities = null,
    int BasicAttackIntervalTicks = 30);
