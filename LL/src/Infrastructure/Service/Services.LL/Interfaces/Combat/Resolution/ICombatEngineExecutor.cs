using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEngineExecutor
{
    Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken);

    async Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        bool captureEventLog,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(runtime, cancellationToken);
        if (!captureEventLog)
        {
            result.EventLog.Clear();
        }

        return result;
    }

    async Task<CombatExecutionWithCheckpoints> ExecuteWithCheckpointsAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(runtime, cancellationToken);
        var initialFriendly = result.PlayerTeam.Select(ToInitialState).ToArray();
        var initialHostile = result.EnemyTeam.Select(ToInitialState).ToArray();
        return new CombatExecutionWithCheckpoints(result,
        [
            new CombatCheckpoint(0, 0, initialFriendly, initialHostile, [], [], false),
            new CombatCheckpoint(
                1,
                result.Duration,
                result.PlayerTeam,
                result.EnemyTeam,
                result.EntityStats,
                result.EventLog,
                true)
        ]);
    }

    Task<CombatExecutionWithCheckpoints> ExecuteTowerPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken) =>
        ExecuteCompactPlaybackAsync(runtime, checkpointIntervalTicks, cancellationToken);

    Task<CombatExecutionWithCheckpoints> ExecuteCompactPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken) =>
        ExecuteWithCheckpointsAsync(runtime, checkpointIntervalTicks, cancellationToken);

    Task<CombatExecutionWithCheckpoints> ExecuteRaidPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CombatSimulationOptions options,
        CancellationToken cancellationToken) =>
        ExecuteCompactPlaybackAsync(runtime, checkpointIntervalTicks, cancellationToken);

    Task<CombatExecutionWithCheckpoints> ExecuteTournamentPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        TournamentCombatSimulationOptions options,
        CancellationToken cancellationToken) =>
        ExecuteCompactPlaybackAsync(runtime, checkpointIntervalTicks, cancellationToken);

    Task<CombatResult> ExecuteSimulationAsync(
        CombatEncounterRuntime runtime,
        CombatSimulationOptions options,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("This combat executor does not support isolated simulation.");

    private static SimpleCombatEntity ToInitialState(SimpleCombatEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ImagePath = entity.ImagePath,
        Health = entity.MaxHealth,
        MaxHealth = entity.MaxHealth,
        Barrier = 0,
        Level = entity.Level
    };
}

public sealed record CombatSimulationOptions(
    int RandomSeed,
    int MaxTicks = 1800,
    bool StartActiveAbilitiesOnCooldown = true,
    IReadOnlyList<AbilitySpec>? SupplementalAbilities = null,
    int BasicAttackIntervalTicks = 30,
    int? OvertimeStartsAtTick = null,
    int OvertimePowerIncreaseIntervalTicks = 0,
    float OvertimePowerIncreasePercent = 0,
    bool CaptureEventLog = true);

public sealed record TournamentCombatSimulationOptions(
    int RegulationTicks,
    int OvertimeTicks,
    int OvertimePowerIncreaseIntervalTicks,
    float OvertimePowerIncreasePercent);
