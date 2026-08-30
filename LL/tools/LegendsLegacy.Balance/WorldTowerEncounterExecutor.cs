using Common.Randomness;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Essences.Definitions;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.PowerRatings;
using Services.LL.WorldTower;

namespace LegendsLegacy.Balance;

internal sealed record WorldTowerPreparedBuild(
    string Id,
    int DisplayCr,
    CanonicalEquipmentBuild Build);

/// <summary>
/// Executes one exact World Tower roster through the production preparation and
/// combat path. Party selection remains the caller's responsibility.
/// </summary>
internal sealed class WorldTowerEncounterExecutor(
    ICombatSetupService combatSetup,
    ICombatEngineExecutor combatEngine)
{
    internal const int TelemetryCheckpointIntervalTicks = 10 * FastCombatEngine.TicksPerSecond;
    private const string InjectedDistributedDamageEffectId =
        "effect.creature.garran.slam_the_gates.damage.balance_distributed_attrition";

    public WorldTowerTrialSnapshot Execute(
        TowerFloorDefinition definition,
        IReadOnlyList<WorldTowerPreparedBuild> roster,
        Creature guardianSource,
        int runSeed,
        int trial,
        int maxTicks,
        double guardianAbilityHealingMultiplier = 1,
        int guardianAdditionalSummonCopies = 0,
        double guardianAdditionalSummonPotencyMultiplier = 1,
        double guardianDistributedDamageMultiplier = 1)
    {
        if (roster.Count != definition.RequiredSlots)
        {
            throw new InvalidOperationException(
                $"World Tower floor {definition.FloorNumber} requires exactly {definition.RequiredSlots} combatants, but {roster.Count} were supplied.");
        }

        var mappedBuilds = new Dictionary<Guid, WorldTowerPreparedBuild>();
        var friendlyRequests = roster.Select((build, index) =>
        {
            var partySlot = index + 1;
            var slotId = $"tower:f{definition.FloorNumber}:t{trial}:player:{partySlot}";
            var snapshotId = StableRandom.Guid("balance-world-tower-snapshot-v1", slotId);
            mappedBuilds.Add(snapshotId, build);
            var snapshot = new CharacterSnapshot
            {
                Id = snapshotId,
                CharacterId = build.Build.Character.Id,
                Name = build.Build.Character.Name,
                Level = build.Build.Character.Level
            };
            return new SnapshotCombatantRequest(
                snapshot,
                new CombatParticipantSlot(
                    slotId,
                    snapshot.CharacterId,
                    CombatSide.Friendly,
                    WorldTowerPartyRules.GetPartyNumber(partySlot)));
        }).ToArray();
        var combatSeed = StableRandom.Seed(
            "balance-world-tower-combat-v1",
            runSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            definition.FloorNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            trial.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var runtimeFactory = new WorldTowerCombatRuntimeFactory(
            new CombatPreparationPipeline(
                new BalanceSnapshotCombatantBuilder(mappedBuilds, combatSetup),
                combatSetup));
        var runtime = runtimeFactory.CreateAsync(
                new WorldTowerCombatRuntimeRequest(
                    StableRandom.Guid("balance-world-tower-encounter-v1", combatSeed.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    Guid.Empty,
                    definition,
                    friendlyRequests,
                    CloneCreature(guardianSource),
                    0,
                    0,
                    0,
                    DateTimeOffset.UnixEpoch,
                    combatSeed),
                CancellationToken.None)
            .GetAwaiter().GetResult();
        ApplyGuardianAbilityHealingMultiplier(runtime, guardianAbilityHealingMultiplier);
        ApplyGuardianAdditionalSummonCopies(
            runtime,
            guardianAdditionalSummonCopies,
            guardianAdditionalSummonPotencyMultiplier);
        ApplyGuardianDistributedDamageMultiplier(runtime, guardianDistributedDamageMultiplier);
        var execution = combatEngine.ExecuteRaidPlaybackAsync(
                runtime,
                TelemetryCheckpointIntervalTicks,
                new CombatRuleset(
                    combatSeed,
                    maxTicks,
                    StartActiveAbilitiesOnCooldown: true,
                    CaptureEventLog: guardianDistributedDamageMultiplier > 1),
                CancellationToken.None)
            .GetAwaiter().GetResult();
        var result = execution.Result;
        var maxHealth = result.PlayerTeam.Sum(member => Math.Max(1, member.MaxHealth));
        var currentHealth = result.PlayerTeam.Sum(member => Math.Max(0, member.Health));
        var friendlyIds = result.PlayerTeam.Select(member => member.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hostileIds = result.EnemyTeam.Select(member => member.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var friendlyStats = result.EntityStats.Where(stats => friendlyIds.Contains(stats.EntityId)).ToArray();
        var hostileStats = result.EntityStats.Where(stats =>
                hostileIds.Contains(stats.EntityId)
                || stats.Team.Equals("Hostile", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var primaryTarget = friendlyStats
            .OrderByDescending(stats => stats.AttentionSharePercent)
            .ThenByDescending(stats => stats.TargetedAttacks)
            .FirstOrDefault();
        var totalFriendlyDamageTaken = friendlyStats.Sum(stats => stats.DamageTaken);
        var nonPrimaryFriendlyDamageTaken = friendlyStats
            .Where(stats => !ReferenceEquals(stats, primaryTarget))
            .Sum(stats => stats.DamageTaken);
        var durationSeconds = Math.Max(
            1d / FastCombatEngine.TicksPerSecond,
            result.Duration / (double)FastCombatEngine.TicksPerSecond);
        var guardianId = definition.GuardianCreatureId.ToString();
        var guardian = result.EnemyTeam.Single(member =>
            member.Id.Equals(guardianId, StringComparison.OrdinalIgnoreCase));
        var guardianHealthRatio = guardian.MaxHealth <= 0
            ? 0
            : Math.Clamp(guardian.Health / (double)guardian.MaxHealth, 0, 1);
        var partySustain = friendlyStats.Sum(stats =>
            stats.HealingDone + stats.BarrierGenerated + stats.HealthRegenerated);
        var guardianStats = hostileStats.FirstOrDefault(stats =>
            stats.EntityId.Equals(guardianId, StringComparison.OrdinalIgnoreCase));
        var regenerationTimeline = execution.Checkpoints.Select(checkpoint =>
        {
            var guardianState = checkpoint.Hostile.FirstOrDefault(member =>
                member.Id.Equals(guardianId, StringComparison.OrdinalIgnoreCase));
            var guardianStats = checkpoint.EntityStats.FirstOrDefault(stats =>
                stats.EntityId.Equals(guardianId, StringComparison.OrdinalIgnoreCase));
            return new WorldTowerRegenerationPointSnapshot(
                checkpoint.Tick,
                guardianState?.Health ?? 0,
                guardianState?.MaxHealth ?? 0,
                guardianStats?.HealthRegenerated ?? 0,
                checkpoint.Hostile.Count(member => member.Health > 0),
                checkpoint.EntityStats.Count(stats =>
                    stats.Team.Equals("Hostile", StringComparison.OrdinalIgnoreCase)
                    && stats.IsSummonedEntity
                    && stats.Health > 0));
        }).ToArray();
        var failureDiagnostic = WorldTowerContentAnalyzer.AnalyzeFailure(
            result,
            maxTicks,
            friendlyStats,
            hostileStats,
            guardianId);
        var injectedDistributedDamageEvents = result.EventLog
            .Where(item => item.Source.StartsWith(
                    InjectedDistributedDamageEffectId,
                    StringComparison.OrdinalIgnoreCase)
                && item.EventType is EventType.Damage or EventType.DamageCrit)
            .ToArray();
        var injectedDistributedDamage = injectedDistributedDamageEvents.Sum(item => item.Magnitude);
        var injectedDistributedDamageWaves = injectedDistributedDamageEvents
            .GroupBy(item => item.Timestamp)
            .ToArray();

        return new WorldTowerTrialSnapshot(
            trial,
            combatSeed,
            result.Outcome.ToString(),
            result.Duration,
            result.PlayerTeam.Count(member => member.Health <= 0),
            WorldTowerContentAnalyzer.RoundMetric(currentHealth / (double)maxHealth, 4),
            WorldTowerContentAnalyzer.RoundMetric(roster.Average(build => build.DisplayCr), 2),
            roster.Sum(build => build.DisplayCr),
            roster.Select(build => build.Id).ToArray())
        {
            PartyNumbers = result.PlayerTeam.Select(member => member.PartyNumber ?? 0).ToArray(),
            GuardianHealthRemainingRatio = WorldTowerContentAnalyzer.RoundMetric(guardianHealthRatio, 4),
            HostileDamagePerSecond = WorldTowerContentAnalyzer.RoundMetric(
                hostileStats.Sum(stats => stats.DamageDone) / durationSeconds,
                2),
            GuardianPassiveRegeneration = guardianStats?.HealthRegenerated ?? 0,
            GuardianAbilityHealing = guardianStats?.HealingDone ?? 0,
            GuardianTotalSelfSustain = guardianStats is null
                ? 0
                : checked(guardianStats.HealthRegenerated + guardianStats.HealingDone),
            GuardianDamageTakenPerSecond = WorldTowerContentAnalyzer.RoundMetric(
                (guardianStats?.DamageTaken ?? 0) / durationSeconds,
                2),
            PrimaryTargetDamageTaken = primaryTarget?.DamageTaken ?? 0,
            NonPrimaryFriendlyDamageTakenPerSecond = WorldTowerContentAnalyzer.RoundMetric(
                nonPrimaryFriendlyDamageTaken / durationSeconds,
                2),
            FriendlyDamageTakenConcentration = totalFriendlyDamageTaken <= 0
                ? 0
                : WorldTowerContentAnalyzer.RoundMetric(
                    friendlyStats.Max(stats => stats.DamageTaken) / (double)totalFriendlyDamageTaken,
                    4),
            PartySustainPerSecond = WorldTowerContentAnalyzer.RoundMetric(partySustain / durationSeconds, 2),
            GuardianInjectedDistributedDamage = injectedDistributedDamage,
            GuardianInjectedDistributedDamagePerSecond = WorldTowerContentAnalyzer.RoundMetric(
                injectedDistributedDamage / durationSeconds,
                2),
            GuardianInjectedDistributedDamageHitCount = injectedDistributedDamageEvents.Length,
            GuardianInjectedDistributedDamageWaveCount = injectedDistributedDamageWaves.Length,
            GuardianInjectedDistributedDamagePeakTargetsPerWave = injectedDistributedDamageWaves.Length == 0
                ? 0
                : injectedDistributedDamageWaves.Max(wave => wave
                    .Select(item => item.TargetId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()),
            FirstFriendlyDeathTick = friendlyStats
                .Where(stats => stats.FirstDeathTick.HasValue)
                .Select(stats => stats.FirstDeathTick)
                .Min(),
            PeakActiveHostileCombatants = result.CompactTelemetry.PeakActiveHostileCombatants,
            PeakActiveHostileSummons = result.CompactTelemetry.PeakActiveHostileSummons,
            FinalActiveHostileCombatants = result.CompactTelemetry.FinalActiveHostileCombatants,
            FinalActiveHostileSummons = result.CompactTelemetry.FinalActiveHostileSummons,
            FirstAdditionalHostileTick = result.CompactTelemetry.FirstAdditionalHostileTick,
            FirstAdditionalHostileClearTick = result.CompactTelemetry.FirstAdditionalHostileClearTick,
            TotalHostileSummons = result.CompactTelemetry.TotalHostileSummons,
            AdditionalHostileWindowCount = result.CompactTelemetry.AdditionalHostileWindowCount,
            ClearedAdditionalHostileWindowCount = result.CompactTelemetry.ClearedAdditionalHostileWindowCount,
            HostileSummonActiveTicks = result.CompactTelemetry.HostileSummonActiveTicks,
            HostileSummonWaveCount = result.CompactTelemetry.HostileSummonWaveCount,
            HostileSummonWaveIntervalCount = result.CompactTelemetry.HostileSummonWaveIntervalCount,
            HostileSummonWaveIntervalTotalTicks = result.CompactTelemetry.HostileSummonWaveIntervalTotalTicks,
            MinimumHostileSummonWaveIntervalTicks = result.CompactTelemetry.MinimumHostileSummonWaveIntervalTicks,
            MaximumHostileSummonWaveIntervalTicks = result.CompactTelemetry.MaximumHostileSummonWaveIntervalTicks,
            CleansedEffects = friendlyStats.Sum(stats => stats.StatusEffectsCleansed),
            DispelledEffects = friendlyStats.Sum(stats => stats.StatusEffectsDispelled),
            HostileActionDeniedTicks = hostileStats.Sum(stats => stats.ActionDeniedTicks),
            FriendlyActionDeniedTicks = friendlyStats.Sum(stats => stats.ActionDeniedTicks),
            GuardianRegenerationTimeline = regenerationTimeline,
            FailureDiagnostic = failureDiagnostic
        };
    }

    private static void ApplyGuardianAbilityHealingMultiplier(
        CombatEncounterRuntime runtime,
        double multiplier)
    {
        if (!double.IsFinite(multiplier) || multiplier is < 0.25 or > 4)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        if (Math.Abs(multiplier - 1) < double.Epsilon)
            return;

        var guardian = runtime.HostileParticipants.Single().Combatant;
        var currentHealingPower = guardian.CombatAttributes.GetValueOrDefault(AttributeType.HealingPowerPercent);
        var currentHealingMultiplier = 1 + currentHealingPower / 100d;
        var targetHealingPower = (currentHealingMultiplier * multiplier - 1) * 100;
        guardian.ModifyAttribute(new InstanceAttributeModifier(
            AttributeType.HealingPowerPercent,
            checked((float)(targetHealingPower - currentHealingPower)),
            ModifierType.Flat));
    }

    private static void ApplyGuardianAdditionalSummonCopies(
        CombatEncounterRuntime runtime,
        int additionalCopies,
        double potencyMultiplier)
    {
        const string abilityId = "ability.creature.morrowmaw.hatch_the_brood";
        const string summonEffectId = "effect.creature.morrowmaw.hatch_the_brood.summon";
        const string summonId = "morrowmawBroodling";

        if (additionalCopies is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(additionalCopies));
        if (!double.IsFinite(potencyMultiplier) || potencyMultiplier is < 0.25 or > 1)
            throw new ArgumentOutOfRangeException(nameof(potencyMultiplier));
        if (additionalCopies == 0)
            return;

        var guardian = runtime.HostileParticipants.Single().Combatant;
        if (!guardian.NativeAbilityIds.Contains(abilityId, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The add-pressure override requires Guardian ability '{abilityId}'.");
        }

        for (var copy = 1; copy <= additionalCopies; copy++)
        {
            guardian.TemporaryAbilityModifiers.Add(new EssenceAbilityModifierDefinition
            {
                Target = summonEffectId,
                Operation = "AddEffect",
                Effect = new AbilityEffectSpec
                {
                    Id = $"{summonEffectId}.balance_add_pressure_{copy}",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.AllEnemies,
                    SummonId = summonId,
                    ProcCoefficient = 1m,
                    SummonPowerMultiplier = potencyMultiplier,
                    SummonHealthMultiplier = potencyMultiplier
                }
            });
        }
    }

    private static void ApplyGuardianDistributedDamageMultiplier(
        CombatEncounterRuntime runtime,
        double multiplier)
    {
        const string abilityId = "ability.creature.garran.slam_the_gates";
        const string damageEffectId = "effect.creature.garran.slam_the_gates.damage";

        if (!double.IsFinite(multiplier) || multiplier is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        if (Math.Abs(multiplier - 1) < double.Epsilon)
            return;

        var guardian = runtime.HostileParticipants.Single().Combatant;
        if (!guardian.NativeAbilityIds.Contains(abilityId, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The distributed-attrition override requires Guardian ability '{abilityId}'.");
        }

        guardian.TemporaryAbilityModifiers.Add(new EssenceAbilityModifierDefinition
        {
            Target = damageEffectId,
            Operation = "AddEffect",
            Effect = new AbilityEffectSpec
            {
                Id = $"{damageEffectId}.balance_distributed_attrition",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.AllEnemies,
                ScalingAttribute = AttributeType.Power,
                ScalingCoefficient = checked((float)(1.5 * (multiplier - 1))),
                AttackType = AttackType.None,
                DamageType = DamageType.Magical,
                ProcCoefficient = 1m
            }
        });
    }

    private static Creature CloneCreature(Creature source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        ImagePath = source.ImagePath,
        Archetype = source.Archetype,
        DamageProfile = source.DamageProfile,
        DefenseProfile = source.DefenseProfile,
        RewardTableId = source.RewardTableId,
        BaseLevel = source.BaseLevel,
        Level = source.Level,
        Tier = source.Tier,
        StatOverrides = source.StatOverrides.Select(value => new StatOverride
        {
            Id = value.Id,
            AttributeType = value.AttributeType,
            Multiplier = value.Multiplier,
            Additive = value.Additive
        }).ToArray(),
        BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(source.Id)
    };

    private sealed class BalanceSnapshotCombatantBuilder(
        IReadOnlyDictionary<Guid, WorldTowerPreparedBuild> builds,
        ICombatSetupService combatSetup) : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CombatRuntimeParticipant> participants = requests.Select(request =>
            {
                if (!builds.TryGetValue(request.Snapshot.Id, out var build))
                    throw new InvalidOperationException($"Balance snapshot '{request.Snapshot.Id}' was not mapped.");
                var combatant = combatSetup.CreatePlayerCombatEntities([build.Build.Character]).Single();
                combatant.EquippedEssences = [.. build.Build.EquippedEssences];
                combatant.HasEquippedEssenceSnapshot = true;
                combatant.Id = request.Slot.SlotId;
                combatant.OriginalId = request.Slot.SourceEntityId;
                return new CombatRuntimeParticipant(request.Slot, build.Build.Character, combatant);
            }).ToArray();
            return Task.FromResult(participants);
        }
    }
}
