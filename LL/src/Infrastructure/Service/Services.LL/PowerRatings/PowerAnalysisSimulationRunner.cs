using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.PowerRatings;

public enum PowerBenchmarkScenario
{
    Overall,
    SingleTarget,
    MultiTarget,
    PhysicalDurability,
    MagicalDurability,
    Sustain
}

public enum CanonicalPartyProfile
{
    Balanced,
    Offense,
    Sustain,
    Defensive,
    Area
}

public sealed record DungeonSimulationAggregate(
    int Attempts,
    int Completions,
    int CheckpointsReached,
    int TotalCombatTicks,
    decimal CompletionRate,
    decimal CheckpointRate);

public sealed class PowerAnalysisSimulationRunner
{
    public const int MaximumBenchmarkIntensity = 8192;
    public const int DisplayPowerPerIntensity = 10;

    private const string BenchmarkMagicAbilityId = "power-benchmark.magic-pressure";
    private const string BenchmarkPhysicalDurabilityAbilityId = "power-benchmark.physical-durability-pressure";
    private const string BenchmarkMagicalDurabilityAbilityId = "power-benchmark.magical-durability-pressure";
    private const int DurabilityPressureCooldownTicks = 50;
    private const string CanonicalStrikeAbilityId = "power-benchmark.canonical-strike";
    private const string CanonicalAreaAbilityId = "power-benchmark.canonical-area";
    private const string CanonicalHealAbilityId = "power-benchmark.canonical-heal";
    private const string CanonicalBarrierAbilityId = "power-benchmark.canonical-barrier";
    private const float AreaDamageAnchorHealth = 1_000_000_000;
    private const int AreaDamageSecondaryCount = 3;

    private readonly ICombatEngineExecutor _combatEngine;
    private readonly ICombatSetupService _combatSetup;
    private readonly DungeonRunFactory _runFactory;
    private readonly Application.Interfaces.Services.AdminDashboard.ICreatureService _creatures;
    private readonly IEntityService _entities;
    private readonly IDungeonVigorService _vigor;

    public PowerAnalysisSimulationRunner(
        ICombatEngineExecutor combatEngine,
        ICombatSetupService combatSetup,
        DungeonRunFactory runFactory,
        Application.Interfaces.Services.AdminDashboard.ICreatureService creatures,
        IEntityService entities,
        IDungeonVigorService vigor)
    {
        _combatEngine = combatEngine;
        _combatSetup = combatSetup;
        _runFactory = runFactory;
        _creatures = creatures;
        _entities = entities;
        _vigor = vigor;
    }

    public async Task<bool> MeetsBenchmarkAsync(
        IReadOnlyList<CombatEntity> party,
        PowerBenchmarkScenario scenario,
        int intensity,
        int seed,
        CancellationToken cancellationToken)
    {
        var maxTicks = scenario is PowerBenchmarkScenario.Sustain ? 1500 : 900;
        var hostiles = CreateBenchmarkEnemies(scenario, intensity);
        var result = await RunCombatAsync(
            party,
            hostiles,
            seed,
            maxTicks,
            BenchmarkAbilities,
            cancellationToken,
            basicAttackIntervalTicks: scenario is PowerBenchmarkScenario.PhysicalDurability or
                PowerBenchmarkScenario.MagicalDurability
                    ? int.MaxValue
                    : 30);

        return scenario switch
        {
            PowerBenchmarkScenario.Overall =>
                result.Outcome == BattleOutcome.Victory && RemainingHealthFraction(result) >= 0.50m,
            PowerBenchmarkScenario.MultiTarget => AreaDamageSecondariesDefeated(result),
            PowerBenchmarkScenario.PhysicalDurability or
            PowerBenchmarkScenario.MagicalDurability or
            PowerBenchmarkScenario.Sustain =>
                result.Outcome == BattleOutcome.Victory ||
                (result.Outcome == BattleOutcome.Draw && result.Duration >= maxTicks),
            _ => result.Outcome == BattleOutcome.Victory
        };
    }

    private static decimal RemainingHealthFraction(CombatResult result)
    {
        var maximumHealth = result.PlayerTeam.Sum(combatant => Math.Max(0, combatant.MaxHealth));
        if (maximumHealth <= 0)
            return 0;

        var remainingHealth = result.PlayerTeam.Sum(combatant =>
            Math.Clamp(combatant.Health, 0, Math.Max(0, combatant.MaxHealth)));
        return remainingHealth / (decimal)maximumHealth;
    }

    private static bool AreaDamageSecondariesDefeated(CombatResult result) =>
        // The first hostile is the durable anchor; only damage reaching the remaining hostiles is scored.
        result.EnemyTeam.Count == AreaDamageSecondaryCount + 1 &&
        result.EnemyTeam.Skip(1).All(combatant => combatant.Health <= 0);

    public async Task<decimal> MeasureControlUtilityAsync(
        IReadOnlyList<CombatEntity> party,
        int intensity,
        int seed,
        CancellationToken cancellationToken)
    {
        const int maxTicks = 600;
        var enemies = CreateBenchmarkEnemies(PowerBenchmarkScenario.PhysicalDurability, intensity);
        var actual = await RunCombatAsync(
            party,
            enemies,
            seed,
            maxTicks,
            BenchmarkAbilities,
            cancellationToken);

        var withoutAbilities = party.Select(x => x.DeepCloneForEncounter()).ToList();
        foreach (var combatant in withoutAbilities)
        {
            combatant.NativeAbilityIds.Clear();
            combatant.EquippedEssences.Clear();
            combatant.Tags.RemoveWhere(tag => tag.StartsWith("Essence.", StringComparison.OrdinalIgnoreCase));
        }

        var baseline = await RunCombatAsync(
            withoutAbilities,
            enemies,
            seed,
            maxTicks,
            BenchmarkAbilities,
            cancellationToken);

        var baselineActions = CountHostileActions(baseline);
        if (baselineActions == 0)
            return 0;

        var actualActions = CountHostileActions(actual);
        return Math.Clamp((baselineActions - actualActions) / (decimal)baselineActions, 0m, 1m);
    }

    public async Task<CombatEntity> CreateCanonicalCombatantAsync(
        CanonicalPartyProfile profile,
        int intensity,
        CancellationToken cancellationToken)
    {
        intensity = Math.Clamp(intensity, 1, MaximumBenchmarkIntensity);
        var healthMultiplier = profile switch
        {
            CanonicalPartyProfile.Sustain => 1.3f,
            CanonicalPartyProfile.Defensive => 1.4f,
            _ => 1f
        };
        var offenseMultiplier = profile switch
        {
            CanonicalPartyProfile.Offense => 1.3f,
            CanonicalPartyProfile.Area => 1.15f,
            _ => 1f
        };
        var defenseMultiplier = profile switch
        {
            CanonicalPartyProfile.Sustain => 1.25f,
            CanonicalPartyProfile.Defensive => 1.4f,
            _ => 1f
        };
        var source = CreateEntity(
            $"Canonical {profile} Party",
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = (100 + intensity * 18) * healthMultiplier,
                [AttributeType.Power] = (10 + intensity * 3) * offenseMultiplier,
                [AttributeType.Fortitude] = intensity * defenseMultiplier,
                [AttributeType.Spirit] = profile == CanonicalPartyProfile.Sustain ? intensity * 2 : intensity,
                [AttributeType.Armor] = intensity * 1.5f * defenseMultiplier,
                [AttributeType.Resistance] = intensity * 1.5f * defenseMultiplier,
                [AttributeType.Precision] = intensity,
                [AttributeType.CritChance] = Math.Min(35, 5 + intensity / 20f),
                [AttributeType.CritDamage] = 50,
                [AttributeType.AttackSpeed] = Math.Min(150, intensity / 10f),
                [AttributeType.HealthRegeneration] = profile == CanonicalPartyProfile.Sustain ? Math.Max(1, intensity / 8f) : 0
            });
        var combatant = new CombatEntity(source)
        {
            HasEquippedEssenceSnapshot = true,
            NativeAbilityIds = profile switch
            {
                CanonicalPartyProfile.Offense => [CanonicalStrikeAbilityId, CanonicalAreaAbilityId],
                CanonicalPartyProfile.Sustain => [CanonicalStrikeAbilityId, CanonicalHealAbilityId, CanonicalBarrierAbilityId],
                CanonicalPartyProfile.Defensive => [CanonicalStrikeAbilityId, CanonicalBarrierAbilityId],
                CanonicalPartyProfile.Area => [CanonicalStrikeAbilityId, CanonicalAreaAbilityId],
                _ => [CanonicalStrikeAbilityId, CanonicalHealAbilityId]
            }
        };
        await _combatSetup.PrepareEntitiesForCombat([combatant]);
        return combatant;
    }

    public async Task<DungeonSimulationAggregate> RunDungeonAsync(
        string dungeonDefinitionId,
        int dungeonTier,
        IReadOnlyList<CombatEntity> party,
        IReadOnlyList<int> seeds,
        IReadOnlyList<AbilitySpec>? supplementalAbilities,
        CancellationToken cancellationToken)
    {
        var completions = 0;
        var checkpoints = 0;
        var totalTicks = 0;

        foreach (var seed in seeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = await RunDungeonOnceAsync(
                dungeonDefinitionId,
                dungeonTier,
                party,
                seed,
                supplementalAbilities,
                cancellationToken);
            if (outcome.Completed)
                completions++;
            if (outcome.CheckpointReached)
                checkpoints++;
            totalTicks += outcome.CombatTicks;
        }

        return new DungeonSimulationAggregate(
            seeds.Count,
            completions,
            checkpoints,
            totalTicks,
            seeds.Count == 0 ? 0 : completions / (decimal)seeds.Count,
            seeds.Count == 0 ? 0 : checkpoints / (decimal)seeds.Count);
    }

    private async Task<(bool Completed, bool CheckpointReached, int CombatTicks)> RunDungeonOnceAsync(
        string dungeonDefinitionId,
        int dungeonTier,
        IReadOnlyList<CombatEntity> party,
        int seed,
        IReadOnlyList<AbilitySpec>? supplementalAbilities,
        CancellationToken cancellationToken)
    {
        var run = _runFactory.CreateForSimulation(dungeonDefinitionId, seed);
        _vigor.RefreshState(run);
        var random = new Random(seed);
        var checkpointReached = false;
        var totalTicks = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = run.State.MapNodes.FirstOrDefault(x => x.RoomIndex == run.CurrentRoomIndex);
            if (node is null || node.NextRoomIndexes.Count == 0)
            {
                var finalRoom = run.Rooms.FirstOrDefault(x => x.RoomIndex == run.CurrentRoomIndex);
                return (finalRoom?.Type == RoomType.Boss, checkpointReached, totalTicks);
            }

            var nextRoomIndex = SelectRoute(node, random);
            run.CurrentRoomIndex = nextRoomIndex;
            var room = run.Rooms.Single(x => x.RoomIndex == nextRoomIndex);

            if (room.Type == RoomType.RestSite)
            {
                checkpointReached = true;
                _vigor.RecoverAtRestSite(run, room);
                continue;
            }

            if (room.Type is not (RoomType.Combat or RoomType.MiniBoss or RoomType.Boss))
                continue;

            var hostiles = await CreateDungeonEnemiesAsync(room, dungeonTier, cancellationToken);
            var roomParty = party.Select(x => x.DeepCloneForEncounter()).ToList();
            if (run.State.VigorState == "Exhausted")
            {
                foreach (var combatant in roomParty)
                {
                    combatant.ModifyAttribute(new DungeonAttributeModifier(
                        AttributeType.MaxHealth,
                        -10,
                        ModifierType.Additive));
                }
            }

            var result = await RunCombatAsync(
                roomParty,
                hostiles,
                unchecked(seed + room.RoomIndex * 7919),
                1800,
                supplementalAbilities,
                cancellationToken);
            totalTicks += result.Duration;

            if (result.Outcome != BattleOutcome.Victory)
                return (false, checkpointReached, totalTicks);

            _vigor.ApplyCombatToll(run, room, result);
            if (run.State.Vigor <= 0)
                return (false, checkpointReached, totalTicks);

            if (room.Type == RoomType.Boss)
                return (true, checkpointReached, totalTicks);
        }
    }

    private static int SelectRoute(DungeonMapNode currentNode, Random random) =>
        currentNode.NextRoomIndexes[random.Next(currentNode.NextRoomIndexes.Count)];

    private async Task<List<CombatEntity>> CreateDungeonEnemiesAsync(
        RoomInstance room,
        int dungeonTier,
        CancellationToken cancellationToken)
    {
        var creatureKeys = room.EncounterIds.Select(DungeonEncounterIdentity.NormalizeCreatureKey).ToList();
        var creatureIds = await _creatures.GetCreaturesByKey(creatureKeys, cancellationToken);
        var sourceEntities = await _entities.GetEntitiesByIdsForCombatAsync(
            creatureIds.Distinct().ToList(),
            cancellationToken);
        var creaturesById = sourceEntities.OfType<Creature>().ToDictionary(x => x.Id);
        var creatureEntities = creatureIds.Where(creaturesById.ContainsKey).Select(x => creaturesById[x]).ToList();
        if (creatureEntities.Count != creatureKeys.Count)
            throw new InvalidOperationException($"Could not resolve every creature in simulated room {room.RoomIndex}.");

        var hostiles = _combatSetup.CreateCreatureCombatEntities(
            [.. creatureEntities],
            new Area { DifficultyTier = 1 });
        foreach (var hostile in hostiles)
            DungeonEnemyDifficultyScaling.Apply(hostile, dungeonTier);
        await _combatSetup.PrepareEntitiesForCombat([.. hostiles]);
        return hostiles;
    }

    private async Task<CombatResult> RunCombatAsync(
        IReadOnlyList<CombatEntity> friendlyTemplates,
        IReadOnlyList<CombatEntity> hostileTemplates,
        int seed,
        int maxTicks,
        IReadOnlyList<AbilitySpec>? supplementalAbilities,
        CancellationToken cancellationToken,
        int basicAttackIntervalTicks = 30)
    {
        var friendly = friendlyTemplates.Select(x => x.DeepCloneForEncounter()).ToList();
        var hostile = hostileTemplates.Select(x => x.DeepCloneForEncounter()).ToList();
        var slots = new List<CombatParticipantSlot>();
        var friendlyParticipants = new List<CombatRuntimeParticipant>();
        var hostileParticipants = new List<CombatRuntimeParticipant>();

        AddParticipants(friendly, CombatSide.Friendly, "power-friendly", slots, friendlyParticipants);
        AddParticipants(hostile, CombatSide.Hostile, "power-hostile", slots, hostileParticipants);

        var plan = new CombatEncounterPlan(
            Guid.Empty,
            CombatMode.Dungeon,
            1,
            DateTimeOffset.UnixEpoch,
            slots,
            new DungeonEncounterSourceContext(Guid.Empty));
        var runtime = new CombatEncounterRuntime(plan, friendlyParticipants, hostileParticipants);
        return await _combatEngine.ExecuteSimulationAsync(
            runtime,
            new CombatSimulationOptions(
                seed,
                maxTicks,
                StartActiveAbilitiesOnCooldown: true,
                SupplementalAbilities: supplementalAbilities,
                BasicAttackIntervalTicks: basicAttackIntervalTicks),
            cancellationToken);
    }

    private static void AddParticipants(
        IReadOnlyList<CombatEntity> combatants,
        CombatSide side,
        string prefix,
        List<CombatParticipantSlot> slots,
        List<CombatRuntimeParticipant> participants)
    {
        for (var index = 0; index < combatants.Count; index++)
        {
            var source = new Character
            {
                Id = DeterministicGuid(side == CombatSide.Friendly ? index + 1 : index + 10_001),
                Name = combatants[index].Name
            };
            var slot = new CombatParticipantSlot($"{prefix}-{index + 1}", source.Id, side);
            combatants[index].Id = slot.SlotId;
            slots.Add(slot);
            participants.Add(new CombatRuntimeParticipant(slot, source, combatants[index]));
        }
    }

    private static IReadOnlyList<CombatEntity> CreateBenchmarkEnemies(
        PowerBenchmarkScenario scenario,
        int intensity)
    {
        intensity = Math.Clamp(intensity, 1, MaximumBenchmarkIntensity);
        var count = scenario switch
        {
            PowerBenchmarkScenario.MultiTarget => AreaDamageSecondaryCount + 1,
            PowerBenchmarkScenario.Overall => 2,
            _ => 1
        };
        var durable = scenario is PowerBenchmarkScenario.PhysicalDurability or PowerBenchmarkScenario.MagicalDurability or PowerBenchmarkScenario.Sustain;
        var enemies = new List<CombatEntity>(count);

        for (var index = 0; index < count; index++)
        {
            // Current-target attacks stay pinned to the anchor instead of clearing the scored enemies sequentially.
            var health = scenario == PowerBenchmarkScenario.MultiTarget && index == 0
                ? AreaDamageAnchorHealth
                : durable
                    ? 1_000_000 + intensity * 5000
                    : scenario == PowerBenchmarkScenario.SingleTarget
                        ? 25 + intensity * 9
                        : 12 + intensity * 4;
            var source = CreateEntity(
                scenario == PowerBenchmarkScenario.MultiTarget && index == 0
                    ? "Power Benchmark Area Anchor"
                    : $"Power Benchmark {scenario} {index + 1}",
                new Dictionary<AttributeType, float>
                {
                    [AttributeType.MaxHealth] = health,
                    [AttributeType.Power] = scenario switch
                    {
                        PowerBenchmarkScenario.Overall => 3 + intensity,
                        _ when durable => 3 + intensity * 1.8f,
                        _ => 1
                    },
                    [AttributeType.Armor] = MathF.Sqrt(intensity) * 1.5f,
                    [AttributeType.Resistance] = MathF.Sqrt(intensity) * 1.5f,
                    [AttributeType.CritChance] = 0,
                    [AttributeType.CritDamage] = 0,
                    [AttributeType.AttackSpeed] = scenario == PowerBenchmarkScenario.Sustain ? 30 : 0
                });
            var combatant = new CombatEntity(source)
            {
                HasEquippedEssenceSnapshot = true
            };
            AttributeCalculator.CalculateBaseCombatAttributes(combatant);
            switch (scenario)
            {
                case PowerBenchmarkScenario.PhysicalDurability:
                    combatant.NativeAbilityIds.Add(BenchmarkPhysicalDurabilityAbilityId);
                    break;
                case PowerBenchmarkScenario.MagicalDurability:
                    combatant.NativeAbilityIds.Add(BenchmarkMagicalDurabilityAbilityId);
                    break;
                case PowerBenchmarkScenario.Sustain:
                    combatant.NativeAbilityIds.Add(BenchmarkMagicAbilityId);
                    break;
                case PowerBenchmarkScenario.Overall when index == 1:
                    combatant.NativeAbilityIds.Add(BenchmarkMagicAbilityId);
                    break;
            }
            enemies.Add(combatant);
        }

        return enemies;
    }

    private static Entity CreateEntity(string name, IReadOnlyDictionary<AttributeType, float> attributes) =>
        new Character
        {
            Id = Guid.Empty,
            Name = name,
            Level = 1,
            BaseAttributes = attributes.Select(x => new EntityAttribute
            {
                AttributeType = x.Key,
                Value = x.Value
            }).ToList()
        };

    private static int CountHostileActions(CombatResult result) =>
        result.EventLog.Count(x =>
            x.ActorId.StartsWith("power-hostile-", StringComparison.OrdinalIgnoreCase) &&
            x.EventType == EventType.AbilityUse);

    private static Guid DeterministicGuid(int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, value);
        return new Guid(bytes);
    }

    public static IReadOnlyList<AbilitySpec> CanonicalAbilities { get; } =
    [
        CreateDamageAbility(CanonicalStrikeAbilityId, AbilityTargetSelector.CurrentTarget, DamageType.Physical, 8, 0.45f, 25),
        CreateDamageAbility(CanonicalAreaAbilityId, AbilityTargetSelector.AllEnemies, DamageType.Magical, 5, 0.25f, 40),
        CreateRecoveryAbility(CanonicalHealAbilityId, AbilityEffectOperation.Heal, AbilityTargetSelector.LowestHealthAlly, 7, 0.25f, 45),
        CreateRecoveryAbility(CanonicalBarrierAbilityId, AbilityEffectOperation.GrantBarrier, AbilityTargetSelector.LowestHealthAlly, 5, 0.2f, 35)
    ];

    private static IReadOnlyList<AbilitySpec> BenchmarkAbilities { get; } =
    [
        CreateDamageAbility(BenchmarkMagicAbilityId, AbilityTargetSelector.CurrentTarget, DamageType.Magical, 1, 0.8f, 20),
        CreateDamageAbility(
            BenchmarkPhysicalDurabilityAbilityId,
            AbilityTargetSelector.CurrentTarget,
            DamageType.Physical,
            1,
            0.8f,
            DurabilityPressureCooldownTicks),
        CreateDamageAbility(
            BenchmarkMagicalDurabilityAbilityId,
            AbilityTargetSelector.CurrentTarget,
            DamageType.Magical,
            1,
            0.8f,
            DurabilityPressureCooldownTicks)
    ];

    private static AbilitySpec CreateDamageAbility(
        string id,
        AbilityTargetSelector target,
        DamageType damageType,
        int baseValue,
        float coefficient,
        int cooldown) => new()
    {
        Id = id,
        Kind = AbilitySpecKind.Active,
        Name = id,
        Description = "Dedicated non-player power benchmark ability.",
        CooldownTicks = cooldown,
        Triggers = [new AbilityTriggerSpec { Event = AbilityTriggerEvent.OnAbilityUsed, EffectIds = [$"{id}.effect"] }],
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = $"{id}.effect",
                Operation = AbilityEffectOperation.Damage,
                Target = target,
                BaseValue = baseValue,
                ScalingAttribute = AttributeType.Power,
                ScalingCoefficient = coefficient,
                AttackType = damageType == DamageType.Physical ? AttackType.Melee : AttackType.Ranged,
                DamageType = damageType
            }
        ]
    };

    private static AbilitySpec CreateRecoveryAbility(
        string id,
        AbilityEffectOperation operation,
        AbilityTargetSelector target,
        int baseValue,
        float coefficient,
        int cooldown) => new()
    {
        Id = id,
        Kind = AbilitySpecKind.Active,
        Name = id,
        Description = "Dedicated non-player canonical party ability.",
        CooldownTicks = cooldown,
        Triggers = [new AbilityTriggerSpec { Event = AbilityTriggerEvent.OnAbilityUsed, EffectIds = [$"{id}.effect"] }],
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = $"{id}.effect",
                Operation = operation,
                Target = target,
                BaseValue = baseValue,
                ScalingAttribute = AttributeType.Spirit,
                ScalingCoefficient = coefficient
            }
        ]
    };
}
