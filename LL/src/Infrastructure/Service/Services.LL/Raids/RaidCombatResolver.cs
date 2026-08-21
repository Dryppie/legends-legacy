using Common.Randomness;
using System.Globalization;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences.Definitions;
using Domain.Models.Raids;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Raids;

public interface IRaidCombatResolver
{
    Task<RaidCombatResolution> ResolveAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RaidCombatResolution>> PreviewAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int sampleCount,
        CancellationToken cancellationToken);
}

public sealed record RaidCombatResolution(
    decimal ReinforcementPenalty,
    decimal GuardianBreak,
    decimal SignatureDisruption,
    decimal BossHealthRemainingPercent,
    RaidOutcome Outcome,
    IReadOnlyList<RaidLaneResult> LaneResults,
    IReadOnlyList<RaidParticipantResult> ParticipantResults,
    IReadOnlyList<RaidLanePlaybackCapture> PlaybackCaptures);

public sealed record RaidLanePlaybackCapture(
    RaidLane Lane,
    CombatResult Result,
    IReadOnlyList<CombatCheckpoint> Checkpoints);

public sealed class RaidCombatResolver(
    ISnapshotCombatantBuilder snapshotCombatants,
    IEntityService entities,
    ICombatSetupService combatSetup,
    ICombatEngineExecutor combatEngine,
    TimeProvider timeProvider) : IRaidCombatResolver
{
    public async Task<RaidCombatResolution> ResolveAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        CancellationToken cancellationToken)
    {
        return await ResolvePipelineAsync(
            run,
            tier,
            lane => Seed(run.Id, lane),
            capturePlayback: true,
            cancellationToken);
    }

    public async Task<IReadOnlyList<RaidCombatResolution>> PreviewAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int sampleCount,
        CancellationToken cancellationToken)
    {
        var output = new List<RaidCombatResolution>(sampleCount);
        for (var sample = 0; sample < sampleCount; sample++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sampleIndex = sample;
            output.Add(await ResolvePipelineAsync(
                run,
                tier,
                lane => PreviewSeed(run.Id, sampleIndex, lane),
                capturePlayback: false,
                cancellationToken));
        }

        return output;
    }

    private async Task<RaidCombatResolution> ResolvePipelineAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        Func<RaidLane, int> seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var rearguard = await ResolveRearguardAsync(
            run, tier, seed(RaidLane.Rearguard), capturePlayback, cancellationToken);
        var vanguard = await ResolveVanguardAsync(
            run, tier, seed(RaidLane.Vanguard), capturePlayback, cancellationToken);
        var mainGuard = await ResolveMainGuardAsync(
            run, tier, seed(RaidLane.MainGuard), capturePlayback, cancellationToken);
        var finalAssault = await ResolveFinalAssaultAsync(
            run,
            tier,
            rearguard,
            vanguard.GuardianBreak,
            mainGuard.SignatureDisruption,
            seed(RaidLane.FinalAssault),
            capturePlayback,
            cancellationToken);
        var participantResults = CalculateParticipantResults(run, new Dictionary<RaidLane, CombatResult>
        {
            [RaidLane.Rearguard] = rearguard.Result,
            [RaidLane.Vanguard] = vanguard.Result,
            [RaidLane.MainGuard] = mainGuard.Result
        }, finalAssault.Result);

        return new RaidCombatResolution(
            rearguard.ReinforcementPenalty,
            vanguard.GuardianBreak,
            mainGuard.SignatureDisruption,
            finalAssault.BossHealthRemainingPercent,
            finalAssault.Outcome,
            [rearguard.LaneResult, vanguard.LaneResult, mainGuard.LaneResult, finalAssault.LaneResult],
            participantResults,
            capturePlayback
                ? [rearguard.Playback, vanguard.Playback, mainGuard.Playback, finalAssault.Playback]
                : []);
    }

    private async Task<RearguardResolution> ResolveRearguardAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.Rearguard, cancellationToken);
        var waves = new List<IReadOnlyList<CombatRuntimeParticipant>>(tier.Rearguard.WaveCount);
        for (var waveNumber = 1; waveNumber <= tier.Rearguard.WaveCount; waveNumber++)
        {
            waves.Add(await CreateCreatureGroupAsync(
                tier.Rearguard.Adds,
                $"rearguard-wave-{waveNumber}",
                seed,
                cancellationToken));
        }

        var hostile = waves[0];
        var reinforcementWaves = waves.Skip(1).ToArray();
        var execution = await ExecuteAsync(
            run.Id,
            RaidLane.Rearguard,
            friendly,
            hostile,
            seed,
            tier.TickBudget.Rearguard,
            capturePlayback,
            cancellationToken,
            reinforcementWaves);
        var result = execution.Result;
        var allHostile = waves.SelectMany(x => x).ToArray();
        var spawnedState = result.EnemyTeam.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var totalMax = Math.Max(1L, allHostile.Sum(x => (long)x.Combatant.GetAttributeValue(AttributeType.MaxHealth)));
        var remaining = allHostile.Sum(x => spawnedState.TryGetValue(x.Slot.SlotId, out var state)
            ? Math.Max(0L, state.Health)
            : (long)x.Combatant.GetAttributeValue(AttributeType.MaxHealth));
        var penalty = Math.Clamp((decimal)remaining / totalMax, 0m, 1m);
        var survivors = allHostile
            .Select(participant =>
            {
                var maxHealth = Math.Max(1, (int)participant.Combatant.GetAttributeValue(AttributeType.MaxHealth));
                var health = spawnedState.TryGetValue(participant.Slot.SlotId, out var state)
                    ? state.Health
                    : maxHealth;
                return (Participant: participant, Health: health, MaxHealth: maxHealth);
            })
            .Where(x => x.Health > 0)
            .Select(x => new SurvivingAdd(
                x.Participant.Slot.SourceEntityId,
                tier.Rearguard.Adds.First(a => a.CreatureId == x.Participant.Slot.SourceEntityId).Scaling,
                (decimal)x.Health / x.MaxHealth))
            .ToArray();
        var totalDamage = SumFriendlyDamage(result, friendly);
        return new RearguardResolution(
            result,
            penalty,
            survivors,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.Rearguard,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = totalDamage,
                ObjectiveDamage = totalDamage,
                SurvivingHostileHealthFraction = penalty,
                DerivedModifier = penalty
            },
            new RaidLanePlaybackCapture(RaidLane.Rearguard, result, execution.Checkpoints));
    }

    private async Task<VanguardResolution> ResolveVanguardAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.Vanguard, cancellationToken);
        var objectiveEntry = new RaidCreatureGroupEntry
        {
            CreatureId = tier.Vanguard.GuardianCreatureId,
            Count = 1,
            Scaling = tier.Vanguard.GuardianScaling
        };
        var hostile = (await CreateCreatureGroupAsync(
            [objectiveEntry, .. tier.Vanguard.Escorts],
            "vanguard",
            seed,
            cancellationToken)).ToList();
        hostile[0].Combatant.Id = "raid-guardian";
        hostile[0] = new CombatRuntimeParticipant(
            hostile[0].Slot with { SlotId = "raid-guardian" },
            hostile[0].SourceEntity,
            hostile[0].Combatant);
        var execution = await ExecuteAsync(
            run.Id,
            RaidLane.Vanguard,
            friendly,
            hostile,
            seed,
            tier.TickBudget.Vanguard,
            capturePlayback,
            cancellationToken);
        var result = execution.Result;
        var objective = result.EnemyTeam.Single(x => x.Id == "raid-guardian");
        var objectiveStats = result.EntityStats.FirstOrDefault(x => x.EntityId == "raid-guardian");
        var healthRemoved = Math.Max(0, objective.MaxHealth - objective.Health);
        var barrierAbsorbed = Math.Max(0, objectiveStats?.DamageBlocked ?? 0);
        var guardianBreak = Math.Clamp(
            (decimal)(healthRemoved + barrierAbsorbed) / Math.Max(1, objective.MaxHealth),
            0m,
            1m);
        if (guardianBreak >= 1m)
            result.Outcome = BattleOutcome.Victory;
        var hostileMax = Math.Max(1L, result.EnemyTeam.Sum(x => (long)x.MaxHealth));
        var hostileRemaining = result.EnemyTeam.Sum(x => Math.Max(0L, x.Health));
        return new VanguardResolution(
            result,
            guardianBreak,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.Vanguard,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = SumFriendlyDamage(result, friendly),
                ObjectiveDamage = healthRemoved,
                ObjectiveBarrierAbsorbed = barrierAbsorbed,
                SurvivingHostileHealthFraction = Math.Clamp((decimal)hostileRemaining / hostileMax, 0m, 1m),
                DerivedModifier = guardianBreak
            },
            new RaidLanePlaybackCapture(RaidLane.Vanguard, result, execution.Checkpoints));
    }

    private async Task<MainGuardResolution> ResolveMainGuardAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.MainGuard, cancellationToken);
        var projectionEntry = new RaidCreatureGroupEntry
        {
            CreatureId = tier.MainGuard.ProjectionCreatureId,
            Count = 1,
            Scaling = tier.MainGuard.ProjectionScaling
        };
        var hostile = (await CreateCreatureGroupAsync(
            [projectionEntry],
            "main-guard-projection",
            seed,
            cancellationToken)).ToList();
        hostile[0].Combatant.Id = "boss-projection";
        hostile[0] = new CombatRuntimeParticipant(
            hostile[0].Slot with { SlotId = "boss-projection" },
            hostile[0].SourceEntity,
            hostile[0].Combatant);
        var execution = await ExecuteAsync(
            run.Id,
            RaidLane.MainGuard,
            friendly,
            hostile,
            seed,
            tier.TickBudget.MainGuard,
            capturePlayback,
            cancellationToken);
        var result = execution.Result;
        var survivedPercent = result.Outcome == BattleOutcome.Victory
            ? 100m
            : Math.Clamp(100m * result.Duration / Math.Max(1, tier.TickBudget.MainGuard), 0m, 100m);
        var thresholdsReached = tier.MainGuard.SurvivalThresholdsPercent.Count(
            threshold => survivedPercent >= threshold);
        var disruption = tier.MainGuard.SurvivalThresholdsPercent.Count == 0
            ? survivedPercent / 100m
            : thresholdsReached / (decimal)tier.MainGuard.SurvivalThresholdsPercent.Count;
        if (disruption >= 1m)
            result.Outcome = BattleOutcome.Victory;
        var hostileMax = Math.Max(1L, result.EnemyTeam.Sum(x => (long)x.MaxHealth));
        var hostileRemaining = result.EnemyTeam.Sum(x => Math.Max(0L, x.Health));
        return new MainGuardResolution(
            result,
            disruption,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.MainGuard,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = SumFriendlyDamage(result, friendly),
                ObjectiveDamage = result.Duration,
                SurvivingHostileHealthFraction = Math.Clamp((decimal)hostileRemaining / hostileMax, 0m, 1m),
                DerivedModifier = disruption
            },
            new RaidLanePlaybackCapture(RaidLane.MainGuard, result, execution.Checkpoints));
    }

    private async Task<FinalAssaultResolution> ResolveFinalAssaultAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        RearguardResolution rearguard,
        decimal guardianBreak,
        decimal signatureDisruption,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateAllFriendlyAsync(run, cancellationToken);
        var bossEntry = SelectBoss(tier.Boss, seed);
        var survivorEntries = rearguard.Survivors.Select(x => new RaidCreatureGroupEntry
        {
            CreatureId = x.CreatureId,
            Count = 1,
            Scaling = x.Scaling
        }).ToArray();
        var hostile = (await CreateCreatureGroupAsync(
            [bossEntry, .. survivorEntries],
            "final-assault",
            seed,
            cancellationToken)).ToList();
        hostile[0].Combatant.Id = "raid-boss";
        hostile[0] = new CombatRuntimeParticipant(
            hostile[0].Slot with { SlotId = "raid-boss" },
            hostile[0].SourceEntity,
            hostile[0].Combatant);
        hostile[0].Combatant.StaggerDefinition = tier.Boss.Stagger;
        hostile[0].Combatant.StaggerParticipantCount = friendly.Count;
        var defenceReduction = -(guardianBreak * tier.Boss.MaxGuardianBreakPercent);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.Armor, defenceReduction);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.Resistance, defenceReduction);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.DamageReduction, defenceReduction);
        RaidCombatScaling.AddPercent(
            hostile[0].Combatant,
            AttributeType.Power,
            -(signatureDisruption * tier.Boss.MaxSignaturePowerReductionPercent));
        var cooldownDelay = (double)(signatureDisruption * tier.Boss.MaxSignatureCooldownDelayPercent / 100m);
        hostile[0].Combatant.TemporaryAbilityModifiers.AddRange(
            hostile[0].Combatant.NativeAbilityIds.Select(abilityId => new EssenceAbilityModifierDefinition
            {
                Target = abilityId,
                Operation = "DelayCooldowns",
                Value = cooldownDelay
            }));
        await combatSetup.PrepareEntitiesForCombat(hostile.Select(x => x.Combatant).ToList());
        for (var i = 1; i < hostile.Count; i++)
        {
            var remainingFraction = rearguard.Survivors[i - 1].HealthFraction;
            hostile[i].Combatant.SetCurrentHealth(
                hostile[i].Combatant.GetAttributeValue(AttributeType.MaxHealth) * (float)remainingFraction);
        }
        await PrepareFriendlyAsync(friendly);
        var execution = await ExecutePreparedAsync(
            run.Id,
            RaidLane.FinalAssault,
            friendly,
            hostile,
            new CombatSimulationOptions(
                seed,
                tier.TickBudget.FinalAssault,
                OvertimeStartsAtTick: tier.Boss.OvertimeStartsAtTick,
                OvertimePowerIncreaseIntervalTicks: 300,
                OvertimePowerIncreasePercent: tier.Boss.OvertimePowerIncreasePercent,
                CaptureEventLog: false),
            capturePlayback,
            cancellationToken);
        var result = execution.Result;
        var boss = result.EnemyTeam.Single(x => x.Id == "raid-boss");
        var remainingPercent = Math.Clamp(100m * boss.Health / Math.Max(1, boss.MaxHealth), 0m, 100m);
        var outcome = boss.Health <= 0
            ? RaidOutcome.Slain
            : remainingPercent < 25m
                ? RaidOutcome.Broken
                : remainingPercent < 60m
                    ? RaidOutcome.Wounded
                    : RaidOutcome.Repelled;
        var hostileMax = Math.Max(1L, result.EnemyTeam.Sum(x => (long)x.MaxHealth));
        var hostileRemaining = result.EnemyTeam.Sum(x => Math.Max(0L, x.Health));
        return new FinalAssaultResolution(
            result,
            remainingPercent,
            outcome,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.FinalAssault,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = SumFriendlyDamage(result, friendly),
                ObjectiveDamage = Math.Max(0, boss.MaxHealth - boss.Health),
                SurvivingHostileHealthFraction = Math.Clamp((decimal)hostileRemaining / hostileMax, 0m, 1m),
                DerivedModifier = remainingPercent / 100m
            },
            new RaidLanePlaybackCapture(RaidLane.FinalAssault, result, execution.Checkpoints));
    }

    private async Task<IReadOnlyList<CombatRuntimeParticipant>> CreateFriendlyAsync(
        RaidRun run,
        RaidLane lane,
        CancellationToken cancellationToken)
    {
        var requests = run.Signups.Where(x =>
                x.Status == RaidSignupStatus.Approved && x.Lane == lane)
            .OrderBy(x => x.WingSlotIndex)
            .Select(x => new SnapshotCombatantRequest(
                x.CharacterSnapshot,
                new CombatParticipantSlot(
                    x.CharacterId.ToString("N"),
                    x.CharacterId,
                    CombatSide.Friendly,
                    RaidParties.FormationNumber(lane))))
            .ToArray();
        return await snapshotCombatants.BuildAsync(requests, cancellationToken);
    }

    private async Task<IReadOnlyList<CombatRuntimeParticipant>> CreateAllFriendlyAsync(
        RaidRun run,
        CancellationToken cancellationToken)
    {
        var requests = run.Signups
            .Where(x => x.Status == RaidSignupStatus.Approved
                        && x.Lane.HasValue
                        && RaidParties.IsAssignable(x.Lane.Value))
            .OrderBy(x => RaidParties.FormationNumber(x.Lane!.Value))
            .ThenBy(x => x.WingSlotIndex)
            .Select(x => new SnapshotCombatantRequest(
                x.CharacterSnapshot,
                new CombatParticipantSlot(
                    x.CharacterId.ToString("N"),
                    x.CharacterId,
                    CombatSide.Friendly,
                    RaidParties.FormationNumber(x.Lane!.Value))))
            .ToArray();
        return await snapshotCombatants.BuildAsync(requests, cancellationToken);
    }

    private async Task<IReadOnlyList<CombatRuntimeParticipant>> CreateCreatureGroupAsync(
        IReadOnlyList<RaidCreatureGroupEntry> entries,
        string slotPrefix,
        int seed,
        CancellationToken cancellationToken)
    {
        var ids = entries.Select(x => x.CreatureId).Distinct().ToArray();
        var sources = (await entities.GetEntitiesByIdsForCombatAsync(ids.ToList(), cancellationToken))
            .OfType<Creature>()
            .ToDictionary(x => x.Id);
        var output = new List<CombatRuntimeParticipant>();
        var ordinal = 0;
        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var entry = entries[entryIndex];
            if (!sources.TryGetValue(entry.CreatureId, out var source))
                throw new InvalidOperationException($"Raid creature '{entry.CreatureId}' was not found.");
            if (!ShouldSpawn(seed, $"{slotPrefix}:{entryIndex}:{entry.CreatureId:N}", entry.SpawnChancePercent))
                continue;
            var template = combatSetup.CreateCreatureCombatEntities([source], new Area { DifficultyTier = 1 }).Single();
            RaidCombatScaling.Apply(template, entry.Scaling);
            for (var i = 0; i < entry.Count; i++)
            {
                var combatant = i == 0 ? template : template.DeepCloneForEncounter();
                var slot = new CombatParticipantSlot(
                    $"{slotPrefix}-{ordinal++}",
                    entry.CreatureId,
                    CombatSide.Hostile);
                combatant.Id = slot.SlotId;
                combatant.OriginalId = entry.CreatureId;
                output.Add(new CombatRuntimeParticipant(slot, source, combatant));
            }
        }
        return output;
    }

    private static RaidCreatureGroupEntry SelectBoss(RaidBossCombatDefinition boss, int seed)
    {
        var roll = RollPercent(seed, "final-assault:boss-variant");
        var cumulativeChance = 0m;
        foreach (var variant in boss.Variants)
        {
            cumulativeChance += variant.SpawnChancePercent;
            if (roll < cumulativeChance)
            {
                return new RaidCreatureGroupEntry
                {
                    CreatureId = variant.CreatureId,
                    Scaling = variant.Scaling ?? boss.Scaling
                };
            }
        }

        return new RaidCreatureGroupEntry
        {
            CreatureId = boss.CreatureId,
            Scaling = boss.Scaling
        };
    }

    private static bool ShouldSpawn(int seed, string rollKey, decimal chancePercent) =>
        chancePercent >= 100 || RollPercent(seed, rollKey) < chancePercent;

    private static decimal RollPercent(int seed, string rollKey)
    {
        var value = unchecked((uint)StableRandom.Seed(
            "raid-spawn-roll-v1",
            seed.ToString(CultureInfo.InvariantCulture),
            rollKey));
        return value * 100m / ((decimal)uint.MaxValue + 1m);
    }

    private async Task<RaidLaneCombatExecution> ExecuteAsync(
        Guid raidRunId,
        RaidLane lane,
        IReadOnlyList<CombatRuntimeParticipant> friendly,
        IReadOnlyList<CombatRuntimeParticipant> hostile,
        int seed,
        int maxTicks,
        bool capturePlayback,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<CombatRuntimeParticipant>>? hostileReinforcementWaves = null)
    {
        await PrepareFriendlyAsync(friendly);
        var allHostile = hostile
            .Concat((hostileReinforcementWaves ?? []).SelectMany(x => x))
            .ToList();
        await combatSetup.PrepareEntitiesForCombat(allHostile.Select(x => x.Combatant).ToList());
        return await ExecutePreparedAsync(
            raidRunId,
            lane,
            friendly,
            hostile,
            new CombatSimulationOptions(seed, maxTicks, CaptureEventLog: false),
            capturePlayback,
            cancellationToken,
            hostileReinforcementWaves);
    }

    private Task PrepareFriendlyAsync(IReadOnlyList<CombatRuntimeParticipant> friendly) =>
        combatSetup.PrepareEntitiesForCombat(friendly.Select(x => x.Combatant).ToList());

    private async Task<RaidLaneCombatExecution> ExecutePreparedAsync(
        Guid raidRunId,
        RaidLane lane,
        IReadOnlyList<CombatRuntimeParticipant> friendly,
        IReadOnlyList<CombatRuntimeParticipant> hostile,
        CombatSimulationOptions options,
        bool capturePlayback,
        CancellationToken cancellationToken,
        IReadOnlyList<IReadOnlyList<CombatRuntimeParticipant>>? hostileReinforcementWaves = null)
    {
        var allHostile = hostile.Concat((hostileReinforcementWaves ?? []).SelectMany(x => x));
        var plan = new CombatEncounterPlan(
            StableRandom.Guid("raid-encounter-v1", raidRunId.ToString("N"), lane.ToString()),
            CombatMode.Raid,
            (int)lane,
            timeProvider.GetUtcNow(),
            [.. friendly.Select(x => x.Slot), .. allHostile.Select(x => x.Slot)],
            new RaidEncounterSourceContext(raidRunId, (int)lane, lane.ToString().ToLowerInvariant()))
        {
            RandomSeed = options.RandomSeed,
            CaptureEventLog = false
        };
        var runtime = new CombatEncounterRuntime(plan, friendly, hostile, hostileReinforcementWaves);
        if (capturePlayback)
        {
            var execution = await combatEngine.ExecuteRaidPlaybackAsync(
                runtime,
                checkpointIntervalTicks: 10,
                options,
                cancellationToken);
            return new RaidLaneCombatExecution(execution.Result, execution.Checkpoints);
        }

        return new RaidLaneCombatExecution(
            await combatEngine.ExecuteSimulationAsync(runtime, options, cancellationToken),
            []);
    }

    private static long SumFriendlyDamage(
        CombatResult result,
        IReadOnlyList<CombatRuntimeParticipant> friendly) =>
        friendly.Sum(x => DamageFor(result, x.Slot.SlotId));

    private static long DamageFor(CombatResult result, string participantId) =>
        result.EntityStats
            .Where(x => x.EntityId.Equals(participantId, StringComparison.OrdinalIgnoreCase)
                        || x.EntityId.StartsWith($"{participantId}:summon:", StringComparison.OrdinalIgnoreCase))
            .Sum(x => (long)x.DamageDone);

    private static IReadOnlyList<RaidParticipantResult> CalculateParticipantResults(
        RaidRun run,
        IReadOnlyDictionary<RaidLane, CombatResult> preparationResults,
        CombatResult finalAssaultResult)
    {
        var damageByCharacter = new Dictionary<Guid, long>();
        foreach (var signup in run.Signups.Where(x =>
                     x.Status == RaidSignupStatus.Approved && x.Lane.HasValue))
        {
            var participantId = signup.CharacterId.ToString("N");
            damageByCharacter[signup.CharacterId] =
                DamageFor(preparationResults[signup.Lane!.Value], participantId)
                + DamageFor(finalAssaultResult, participantId);
        }

        var scores = new Dictionary<Guid, decimal>();
        var approvedSignups = run.Signups
            .Where(x => x.Status == RaidSignupStatus.Approved && x.Lane.HasValue)
            .ToArray();
        foreach (var laneGroup in approvedSignups.GroupBy(x => x.Lane!.Value))
        {
            var wingDamage = Math.Max(1L, laneGroup.Sum(x => damageByCharacter.GetValueOrDefault(x.CharacterId)));
            var wingSizeFactor = (decimal)laneGroup.Count() / Math.Max(1, approvedSignups.Length);
            foreach (var signup in laneGroup)
                scores[signup.CharacterId] = (decimal)damageByCharacter.GetValueOrDefault(signup.CharacterId) / wingDamage / 3m * wingSizeFactor;
        }
        var ranked = scores.OrderByDescending(x => x.Value).Select((x, index) => (x.Key, Rank: index + 1)).ToDictionary(x => x.Key, x => x.Rank);

        return approvedSignups.Select(signup =>
        {
            var score = scores.GetValueOrDefault(signup.CharacterId);
            return new RaidParticipantResult
            {
                RaidRunId = run.Id,
                CharacterId = signup.CharacterId,
                Lane = signup.Lane!.Value,
                DamageDone = damageByCharacter.GetValueOrDefault(signup.CharacterId),
                ContributionScore = score,
                ContributionRank = ranked[signup.CharacterId]
            };
        }).ToArray();
    }

    private static int Seed(Guid raidRunId, RaidLane lane) =>
        StableRandom.Seed("raid-resolution-v1", raidRunId.ToString("N"), lane.ToString());

    private static int PreviewSeed(Guid raidRunId, int sample, RaidLane lane) =>
        StableRandom.Seed(
            "raid-battle-plan-v1",
            raidRunId.ToString("N"),
            sample.ToString(System.Globalization.CultureInfo.InvariantCulture),
            lane.ToString());

    private sealed record SurvivingAdd(Guid CreatureId, RaidAttributeScalingDefinition Scaling, decimal HealthFraction);
    private sealed record RaidLaneCombatExecution(CombatResult Result, IReadOnlyList<CombatCheckpoint> Checkpoints);
    private sealed record RearguardResolution(CombatResult Result, decimal ReinforcementPenalty, IReadOnlyList<SurvivingAdd> Survivors, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
    private sealed record VanguardResolution(CombatResult Result, decimal GuardianBreak, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
    private sealed record MainGuardResolution(CombatResult Result, decimal SignatureDisruption, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
    private sealed record FinalAssaultResolution(CombatResult Result, decimal BossHealthRemainingPercent, RaidOutcome Outcome, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
}
