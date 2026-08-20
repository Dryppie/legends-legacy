using Common.Randomness;
using System.Globalization;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
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
    decimal WardBreak,
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
        var flank = await ResolveFlankAsync(run, tier, seed(RaidLane.Flank), capturePlayback, cancellationToken);
        var ward = await ResolveWardAsync(run, tier, seed(RaidLane.Ward), capturePlayback, cancellationToken);
        var vanguard = await ResolveVanguardAsync(run, tier, flank, ward.WardBreak, seed(RaidLane.Vanguard), capturePlayback, cancellationToken);
        var participantResults = CalculateParticipantResults(run, new Dictionary<RaidLane, CombatResult>
        {
            [RaidLane.Flank] = flank.Result,
            [RaidLane.Ward] = ward.Result,
            [RaidLane.Vanguard] = vanguard.Result
        });

        return new RaidCombatResolution(
            flank.ReinforcementPenalty,
            ward.WardBreak,
            vanguard.BossHealthRemainingPercent,
            vanguard.Outcome,
            [flank.LaneResult, ward.LaneResult, vanguard.LaneResult],
            participantResults,
            capturePlayback
                ? [flank.Playback, ward.Playback, vanguard.Playback]
                : []);
    }

    private async Task<FlankResolution> ResolveFlankAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.Flank, cancellationToken);
        var hostile = await CreateCreatureGroupAsync(
            tier.Flank.Adds,
            "flank-add",
            seed,
            cancellationToken);
        var execution = await ExecuteAsync(
            run.Id,
            RaidLane.Flank,
            friendly,
            hostile,
            seed,
            tier.TickBudget.Flank,
            capturePlayback,
            cancellationToken);
        var result = execution.Result;
        var totalMax = Math.Max(1L, result.EnemyTeam.Sum(x => (long)x.MaxHealth));
        var remaining = result.EnemyTeam.Sum(x => Math.Max(0L, x.Health));
        var penalty = Math.Clamp((decimal)remaining / totalMax, 0m, 1m);
        var survivors = result.EnemyTeam
            .Where(x => x.Health > 0)
            .Select(x => new SurvivingAdd(
                hostile.Single(h => h.Slot.SlotId == x.Id).Slot.SourceEntityId,
                tier.Flank.Adds.First(a => a.CreatureId == hostile.Single(h => h.Slot.SlotId == x.Id).Slot.SourceEntityId).Scaling,
                (decimal)x.Health / Math.Max(1, x.MaxHealth)))
            .ToArray();
        var totalDamage = SumFriendlyDamage(result, friendly);
        return new FlankResolution(
            result,
            penalty,
            survivors,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.Flank,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = totalDamage,
                ObjectiveDamage = totalDamage,
                SurvivingHostileHealthFraction = penalty,
                DerivedModifier = penalty
            },
            new RaidLanePlaybackCapture(RaidLane.Flank, result, execution.Checkpoints));
    }

    private async Task<WardResolution> ResolveWardAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.Ward, cancellationToken);
        var objectiveEntry = new RaidCreatureGroupEntry
        {
            CreatureId = tier.Ward.ObjectiveCreatureId,
            Count = 1,
            Scaling = tier.Ward.ObjectiveScaling
        };
        var hostile = (await CreateCreatureGroupAsync(
            [objectiveEntry, .. tier.Ward.Guards],
            "ward",
            seed,
            cancellationToken)).ToList();
        hostile[0].Combatant.Id = "ward-objective";
        hostile[0] = new CombatRuntimeParticipant(
            hostile[0].Slot with { SlotId = "ward-objective" },
            hostile[0].SourceEntity,
            hostile[0].Combatant);
        var execution = await ExecuteAsync(
            run.Id,
            RaidLane.Ward,
            friendly,
            hostile,
            seed,
            tier.TickBudget.Ward,
            capturePlayback,
            cancellationToken);
        var result = execution.Result;
        var objective = result.EnemyTeam.Single(x => x.Id == "ward-objective");
        var objectiveStats = result.EntityStats.FirstOrDefault(x => x.EntityId == "ward-objective");
        var healthRemoved = Math.Max(0, objective.MaxHealth - objective.Health);
        var barrierAbsorbed = Math.Max(0, objectiveStats?.DamageBlocked ?? 0);
        var wardBreak = Math.Clamp(
            (decimal)(healthRemoved + barrierAbsorbed) / Math.Max(1, objective.MaxHealth),
            0m,
            1m);
        var hostileMax = Math.Max(1L, result.EnemyTeam.Sum(x => (long)x.MaxHealth));
        var hostileRemaining = result.EnemyTeam.Sum(x => Math.Max(0L, x.Health));
        return new WardResolution(
            result,
            wardBreak,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.Ward,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = SumFriendlyDamage(result, friendly),
                ObjectiveDamage = healthRemoved,
                ObjectiveBarrierAbsorbed = barrierAbsorbed,
                SurvivingHostileHealthFraction = Math.Clamp((decimal)hostileRemaining / hostileMax, 0m, 1m),
                DerivedModifier = wardBreak
            },
            new RaidLanePlaybackCapture(RaidLane.Ward, result, execution.Checkpoints));
    }

    private async Task<VanguardResolution> ResolveVanguardAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        FlankResolution flank,
        decimal wardBreak,
        int seed,
        bool capturePlayback,
        CancellationToken cancellationToken)
    {
        var friendly = await CreateFriendlyAsync(run, RaidLane.Vanguard, cancellationToken);
        var bossEntry = SelectBoss(tier.Boss, seed);
        var survivorEntries = flank.Survivors.Select(x => new RaidCreatureGroupEntry
        {
            CreatureId = x.CreatureId,
            Count = 1,
            Scaling = x.Scaling
        }).ToArray();
        var hostile = (await CreateCreatureGroupAsync(
            [bossEntry, .. survivorEntries],
            "vanguard",
            seed,
            cancellationToken)).ToList();
        hostile[0].Combatant.Id = "raid-boss";
        hostile[0] = new CombatRuntimeParticipant(
            hostile[0].Slot with { SlotId = "raid-boss" },
            hostile[0].SourceEntity,
            hostile[0].Combatant);
        RaidCombatScaling.AddPercent(
            hostile[0].Combatant,
            AttributeType.Power,
            flank.ReinforcementPenalty * tier.Boss.MaxReinforceOffensePercent);
        var defenceReduction = -(wardBreak * tier.Boss.MaxWardBreakPercent);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.Armor, defenceReduction);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.Resistance, defenceReduction);
        RaidCombatScaling.AddPercent(hostile[0].Combatant, AttributeType.DamageReduction, defenceReduction);
        await combatSetup.PrepareEntitiesForCombat(hostile.Select(x => x.Combatant).ToList());
        for (var i = 1; i < hostile.Count; i++)
        {
            var remainingFraction = flank.Survivors[i - 1].HealthFraction;
            hostile[i].Combatant.SetCurrentHealth(
                hostile[i].Combatant.GetAttributeValue(AttributeType.MaxHealth) * (float)remainingFraction);
        }
        await PrepareFriendlyAsync(friendly);
        var execution = await ExecutePreparedAsync(
            run.Id,
            RaidLane.Vanguard,
            friendly,
            hostile,
            new CombatSimulationOptions(
                seed,
                tier.TickBudget.Vanguard,
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
        return new VanguardResolution(
            result,
            remainingPercent,
            outcome,
            new RaidLaneResult
            {
                RaidRunId = run.Id,
                Lane = RaidLane.Vanguard,
                Seed = seed,
                DurationTicks = result.Duration,
                BattleOutcome = result.Outcome,
                TotalFriendlyDamage = SumFriendlyDamage(result, friendly),
                ObjectiveDamage = Math.Max(0, boss.MaxHealth - boss.Health),
                SurvivingHostileHealthFraction = Math.Clamp((decimal)hostileRemaining / hostileMax, 0m, 1m),
                DerivedModifier = remainingPercent / 100m
            },
            new RaidLanePlaybackCapture(RaidLane.Vanguard, result, execution.Checkpoints));
    }

    private async Task<IReadOnlyList<CombatRuntimeParticipant>> CreateFriendlyAsync(
        RaidRun run,
        RaidLane lane,
        CancellationToken cancellationToken)
    {
        var requests = run.Signups.Where(x => x.Lane == lane)
            .OrderBy(x => x.WingSlotIndex)
            .Select(x => new SnapshotCombatantRequest(
                x.CharacterSnapshot,
                new CombatParticipantSlot(
                    x.CharacterId.ToString("N"),
                    x.CharacterId,
                    CombatSide.Friendly,
                    (int)lane + 1)))
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
        var roll = RollPercent(seed, "vanguard:boss-variant");
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
        CancellationToken cancellationToken)
    {
        await PrepareFriendlyAsync(friendly);
        await combatSetup.PrepareEntitiesForCombat(hostile.Select(x => x.Combatant).ToList());
        return await ExecutePreparedAsync(
            raidRunId,
            lane,
            friendly,
            hostile,
            new CombatSimulationOptions(seed, maxTicks, CaptureEventLog: false),
            capturePlayback,
            cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var plan = new CombatEncounterPlan(
            StableRandom.Guid("raid-encounter-v1", raidRunId.ToString("N"), lane.ToString()),
            CombatMode.Raid,
            (int)lane,
            timeProvider.GetUtcNow(),
            [.. friendly.Select(x => x.Slot), .. hostile.Select(x => x.Slot)],
            new RaidEncounterSourceContext(raidRunId, (int)lane, lane.ToString().ToLowerInvariant()))
        {
            RandomSeed = options.RandomSeed,
            CaptureEventLog = false
        };
        var runtime = new CombatEncounterRuntime(plan, friendly, hostile);
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
        IReadOnlyDictionary<RaidLane, CombatResult> laneResults)
    {
        var damageByCharacter = new Dictionary<Guid, long>();
        foreach (var signup in run.Signups.Where(x => x.Lane.HasValue))
        {
            var result = laneResults[signup.Lane!.Value];
            damageByCharacter[signup.CharacterId] = DamageFor(result, signup.CharacterId.ToString("N"));
        }

        var scores = new Dictionary<Guid, decimal>();
        foreach (var laneGroup in run.Signups.Where(x => x.Lane.HasValue).GroupBy(x => x.Lane!.Value))
        {
            var wingDamage = Math.Max(1L, laneGroup.Sum(x => damageByCharacter.GetValueOrDefault(x.CharacterId)));
            var wingSizeFactor = (decimal)laneGroup.Count() / Math.Max(1, run.Signups.Count);
            foreach (var signup in laneGroup)
                scores[signup.CharacterId] = (decimal)damageByCharacter.GetValueOrDefault(signup.CharacterId) / wingDamage / 3m * wingSizeFactor;
        }
        var orderedScores = scores.Values.Order().ToArray();
        var median = orderedScores.Length == 0
            ? 1m
            : orderedScores.Length % 2 == 1
                ? orderedScores[orderedScores.Length / 2]
                : (orderedScores[orderedScores.Length / 2 - 1] + orderedScores[orderedScores.Length / 2]) / 2m;
        median = Math.Max(0.000001m, median);
        var ranked = scores.OrderByDescending(x => x.Value).Select((x, index) => (x.Key, Rank: index + 1)).ToDictionary(x => x.Key, x => x.Rank);

        return run.Signups.Where(x => x.Lane.HasValue).Select(signup =>
        {
            var score = scores.GetValueOrDefault(signup.CharacterId);
            var payout = 0.70m + 0.30m * Math.Min(1.5m, score / median) / 1.5m;
            return new RaidParticipantResult
            {
                RaidRunId = run.Id,
                CharacterId = signup.CharacterId,
                Lane = signup.Lane!.Value,
                DamageDone = damageByCharacter.GetValueOrDefault(signup.CharacterId),
                ContributionScore = score,
                PayoutMultiplier = Math.Clamp(payout, 0.70m, 1m),
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
    private sealed record FlankResolution(CombatResult Result, decimal ReinforcementPenalty, IReadOnlyList<SurvivingAdd> Survivors, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
    private sealed record WardResolution(CombatResult Result, decimal WardBreak, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
    private sealed record VanguardResolution(CombatResult Result, decimal BossHealthRemainingPercent, RaidOutcome Outcome, RaidLaneResult LaneResult, RaidLanePlaybackCapture Playback);
}
