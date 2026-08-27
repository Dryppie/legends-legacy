using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.RegionBosses;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.RegionBosses;

public interface IRegionBossCombatResolver
{
    Task<RegionBossCombatResolution> ResolveAsync(RegionBossRun run, RegionBossDefinition definition, CancellationToken cancellationToken);
}

public sealed record RegionBossCombatResolution(
    int HighestLevelDefeated,
    int CurrentBossLevel,
    int CurrentBossHealthRemaining,
    int CurrentBossMaxHealth,
    int CurrentBossProgressBasisPoints,
    int DurationTicks,
    int FuryStacks,
    RegionBossTerminationReason TerminationReason,
    IReadOnlyList<RegionBossParticipantResult> ParticipantResults,
    CombatResult Result,
    IReadOnlyList<CombatCheckpoint> Checkpoints);

public sealed class RegionBossCombatResolver(
    IEntityService entities,
    ICombatPreparationPipeline combatPreparation,
    ICombatEngineExecutor combatEngine,
    TimeProvider timeProvider) : IRegionBossCombatResolver
{
    public async Task<RegionBossCombatResolution> ResolveAsync(
        RegionBossRun run,
        RegionBossDefinition definition,
        CancellationToken cancellationToken)
    {
        var members = run.Members.OrderBy(x => x.PartySlot).ToArray();
        if (members.Length == 0)
            throw new InvalidOperationException("A Region Boss run cannot resolve without party members.");

        var missingSnapshotCharacterIds = members
            .Where(x => x.CharacterSnapshot is null)
            .Select(x => x.CharacterId)
            .ToArray();
        if (missingSnapshotCharacterIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Region Boss participants do not have combat snapshots: {string.Join(", ", missingSnapshotCharacterIds)}.");
        }

        var source = (await entities.GetEntitiesByIdsForCombatAsync([definition.CreatureId], cancellationToken))
            .OfType<Creature>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException($"Region Boss creature '{definition.CreatureId}' was not found.");
        var templateSlot = new CombatParticipantSlot(
            "region-boss-template",
            definition.CreatureId,
            CombatSide.Hostile);
        var preparedParticipants = await combatPreparation.PrepareAsync(
            CombatContentType.RegionBoss,
            [
                .. members.Select(member => new CombatantPreparationRequest(
                    new CombatParticipantSlot(
                        member.CharacterId.ToString("N"),
                        member.CharacterId,
                        CombatSide.Friendly,
                        1),
                    new SnapshotCombatantPreparationSource(member.CharacterSnapshot!))),
                new CombatantPreparationRequest(
                    templateSlot,
                    new LiveCombatantPreparationSource(
                        source,
                        new Domain.Models.Regions.Areas.Area { DifficultyTier = 1 }))
            ],
            cancellationToken);
        var friendly = preparedParticipants.Where(x => x.Slot.Side == CombatSide.Friendly).ToArray();
        var template = preparedParticipants.Single(x => x.Slot == templateSlot).Combatant;

        CombatRuntimeParticipant CreateBoss(int level)
        {
            var combatant = template.DeepCloneForEncounter();
            var slot = new CombatParticipantSlot($"region-boss-level-{level}", definition.CreatureId, CombatSide.Hostile);
            combatant.Id = slot.SlotId;
            combatant.OriginalId = definition.CreatureId;
            RegionBossCombatScaling.Apply(combatant, definition, level, members.Length);
            combatant.SyncCurrentHealthToMax();
            return new CombatRuntimeParticipant(slot, source, combatant);
        }

        var firstBoss = CreateBoss(1);
        var plan = new CombatEncounterPlan(
            run.Id,
            CombatMode.RegionBoss,
            1,
            timeProvider.GetUtcNow(),
            [.. friendly.Select(x => x.Slot), firstBoss.Slot],
            new RegionBossEncounterSourceContext(run.Id))
        {
            ContentType = CombatContentType.RegionBoss,
            RandomSeed = run.RandomSeed,
            CaptureEventLog = false
        };
        var runtime = new CombatEncounterRuntime(
            plan,
            friendly,
            [firstBoss],
            hostileWaveFactory: level => [CreateBoss(level)]);
        var options = new CombatRuleset(
            run.RandomSeed,
            RegionBossRules.EncounterTicks,
            StartActiveAbilitiesOnCooldown: true,
            CaptureEventLog: false,
            Downed: new CombatDownedOptions(
                definition.Revival.BaseDelaySeconds * FastCombatEngine.TicksPerSecond,
                definition.Revival.AdditionalDelaySecondsPerDeath * FastCombatEngine.TicksPerSecond,
                definition.Revival.MaximumDelaySeconds * FastCombatEngine.TicksPerSecond,
                definition.Revival.ReviveHealthPercent),
            WaveRecovery: new CombatWaveRecoveryOptions(
                definition.Recovery.LivingHealPercent,
                definition.Recovery.DownedReviveHealthPercent),
            HostileFury: new CombatHostileFuryOptions(
                definition.Fury.IntervalSeconds * FastCombatEngine.TicksPerSecond,
                definition.Fury.PowerPercentPerStack,
                definition.Fury.AttackSpeedPercentPerStack));
        var execution = await combatEngine.ExecuteRaidPlaybackAsync(
            runtime,
            checkpointIntervalTicks: 10,
            options,
            cancellationToken);
        var final = execution.Checkpoints.LastOrDefault()
            ?? throw new InvalidOperationException("Region Boss combat produced no checkpoints.");
        var currentLevel = Math.Max(1, final.Context?.WaveNumber ?? 1);
        var currentBoss = final.Hostile.FirstOrDefault(x =>
            x.Id.Equals($"region-boss-level-{currentLevel}", StringComparison.OrdinalIgnoreCase));
        var maxHealth = Math.Max(1, currentBoss?.MaxHealth ?? 1);
        var health = Math.Clamp(currentBoss?.Health ?? 0, 0, maxHealth);
        var highestLevel = currentBoss is null || health <= 0 ? currentLevel : currentLevel - 1;
        var progress = (int)Math.Round((1d - health / (double)maxHealth) * 10_000d);
        var livingFriendly = final.Friendly.Any(x => x.Health > 0);
        var telemetry = execution.Result.EntityStats.ToDictionary(x => x.EntityId, StringComparer.OrdinalIgnoreCase);
        var participantResults = members.Select(member =>
        {
            telemetry.TryGetValue(member.CharacterId.ToString("N"), out var stats);
            return new RegionBossParticipantResult
            {
                RegionBossRunId = run.Id,
                CharacterId = member.CharacterId,
                DamageDone = stats?.DamageDone ?? 0,
                DamageTaken = stats?.DamageTaken ?? 0,
                HealingDone = stats?.HealingDone ?? 0,
                HealingReceived = stats?.HealingReceived ?? 0,
                BarrierGenerated = stats?.BarrierGenerated ?? 0,
                DamagePrevented = stats is null ? 0 : stats.DamageBlocked + stats.TypedMitigationPrevented + stats.AvoidedDamage,
                ThreatGenerated = stats?.ThreatGenerated ?? 0,
                Deaths = stats?.Deaths ?? 0,
                Revivals = stats?.Revivals ?? 0,
                DownedTicks = stats?.DownedTicks ?? 0
            };
        }).ToArray();

        return new RegionBossCombatResolution(
            Math.Max(0, highestLevel),
            currentLevel,
            health,
            maxHealth,
            Math.Clamp(progress, 0, 10_000),
            execution.Result.Duration,
            final.Context?.FuryStacks ?? 0,
            livingFriendly ? RegionBossTerminationReason.TimeExpired : RegionBossTerminationReason.PartyDefeated,
            participantResults,
            execution.Result,
            execution.Checkpoints);
    }
}
