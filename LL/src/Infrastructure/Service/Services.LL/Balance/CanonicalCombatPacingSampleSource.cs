using Application.Interfaces.Services.LL.Balance;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Engine;
using Services.LL.PowerRatings;

namespace Services.LL.Balance;

/// <summary>
/// Runs the pacing contract through the production combat engine. Reference controls are
/// deliberately shared by every role and remain fixed until ReferenceControlVersion changes.
/// </summary>
public sealed class CanonicalCombatPacingSampleSource : ICanonicalCombatPacingSampleSource
{
    private const string MixedPressureAbilityId = "equipment-pacing.mixed-magical-pressure";
    private const int FullCanonicalEssenceCount = 6;
    private const int ReferenceCharacterLevel = (FullCanonicalEssenceCount - 1) * 10;
    private const int StandardHealth = 520;
    private const int EliteHealth = 2_040;
    private const int SoloBossHealth = 7_100;
    private const int PartyBoss5Health = 26_000;
    private const int PartyBoss10Health = 52_000;
    private const int TargetDummyHealth = 1_000_000_000;
    private const float MixedPressurePower = 160f;

    private static readonly IReadOnlyList<AbilitySpec> SupplementalAbilities =
    [
        .. PowerAnalysisSimulationRunner.CanonicalAbilities,
        new AbilitySpec
        {
            Id = MixedPressureAbilityId,
            Kind = AbilitySpecKind.Active,
            Name = "Canonical mixed magical pressure",
            Description = "Versioned magical half of the equipment-pacing pressure control.",
            CooldownTicks = 30,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = [$"{MixedPressureAbilityId}.damage"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{MixedPressureAbilityId}.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 1,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = AttributeCombatRules.BasicAttackPowerCoefficient,
                    AttackType = AttackType.Ranged,
                    DamageType = DamageType.Magical,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        }
    ];

    private readonly CanonicalEquipmentBuildFactory _builds;
    private readonly PowerAnalysisSimulationRunner _simulations;

    public CanonicalCombatPacingSampleSource(
        CanonicalEquipmentBuildFactory builds,
        PowerAnalysisSimulationRunner simulations)
    {
        _builds = builds;
        _simulations = simulations;
    }

    public async Task<CombatPacingSample> RunAsync(
        CanonicalCombatRole role,
        int tier,
        CombatPacingScenario scenario,
        int seed,
        CancellationToken cancellationToken)
    {
        if (scenario is CombatPacingScenario.OvergearTtk or
            CombatPacingScenario.OvergearRawTtd)
        {
            if (tier <= EquipmentStatBudgetCatalog.MinimumTier)
                throw new ArgumentOutOfRangeException(nameof(tier));
            return await RunOvergearAsync(role, tier, scenario, seed, cancellationToken);
        }

        return await RunSingleAsync(
            role,
            buildTier: tier,
            encounterTier: tier,
            scenario,
            seed,
            cancellationToken);
    }

    private async Task<CombatPacingSample> RunOvergearAsync(
        CanonicalCombatRole role,
        int buildTier,
        CombatPacingScenario scenario,
        int seed,
        CancellationToken cancellationToken)
    {
        var encounterTier = buildTier - 1;
        var baseScenario = scenario == CombatPacingScenario.OvergearTtk
            ? CombatPacingScenario.StandardEnemyTtk
            : CombatPacingScenario.RawTtd;
        var reference = await RunSingleAsync(
            role,
            encounterTier,
            encounterTier,
            baseScenario,
            seed,
            cancellationToken);
        var overgeared = await RunSingleAsync(
            role,
            buildTier,
            encounterTier,
            baseScenario,
            seed,
            cancellationToken);
        return overgeared with { ReferenceDurationTicks = reference.DurationTicks };
    }

    private async Task<CombatPacingSample> RunSingleAsync(
        CanonicalCombatRole role,
        int buildTier,
        int encounterTier,
        CombatPacingScenario scenario,
        int seed,
        CancellationToken cancellationToken)
    {
        CanonicalPartySetup? cooperativeParty = null;
        if (scenario is CombatPacingScenario.PartyBossTtk or
            CombatPacingScenario.PartyBoss5Ttk or
            CombatPacingScenario.PartyBoss10Ttk)
        {
            var partySize = scenario == CombatPacingScenario.PartyBoss10Ttk ? 10 : 5;
            cooperativeParty = await CreatePartyAsync(
                partySize,
                buildTier,
                encounterTier,
                cancellationToken);
        }

        var friendly = cooperativeParty?.Combatants ??
        [
            await CreatePlayerAsync(
                role,
                buildTier,
                encounterTier,
                rawDurability: scenario == CombatPacingScenario.RawTtd,
                cancellationToken)
        ];
        var targetHealth = scenario switch
        {
            CombatPacingScenario.StandardEnemyTtk => StandardHealth,
            CombatPacingScenario.EliteEnemyTtk => EliteHealth,
            CombatPacingScenario.SoloBossTtk => SoloBossHealth,
            CombatPacingScenario.PartyBossTtk or CombatPacingScenario.PartyBoss5Ttk =>
                PartyBoss5Health,
            CombatPacingScenario.PartyBoss10Ttk => PartyBoss10Health,
            CombatPacingScenario.OffensiveWindow90 or CombatPacingScenario.OffensiveWindow120 =>
                TargetDummyHealth,
            CombatPacingScenario.RawTtd or CombatPacingScenario.EffectiveTtd =>
                TargetDummyHealth,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
        var isDurability = scenario is CombatPacingScenario.RawTtd or CombatPacingScenario.EffectiveTtd;
        var hostile = isDurability
            ? CreatePressureSources(1d)
            : cooperativeParty is null
                ? new[] { CreateTarget(targetHealth) }
                : new[] { CreatePartyBoss(targetHealth) };
        var maxTicks = scenario switch
        {
            CombatPacingScenario.StandardEnemyTtk => 600,
            CombatPacingScenario.EliteEnemyTtk => 900,
            CombatPacingScenario.SoloBossTtk => 2_400,
            CombatPacingScenario.PartyBossTtk or
                CombatPacingScenario.PartyBoss5Ttk or
                CombatPacingScenario.PartyBoss10Ttk => 2_700,
            CombatPacingScenario.RawTtd => 1_200,
            CombatPacingScenario.EffectiveTtd => 1_800,
            CombatPacingScenario.OffensiveWindow90 => 900,
            CombatPacingScenario.OffensiveWindow120 => 1_200,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
        var result = await _simulations.RunCombatAsync(
            friendly,
            hostile,
            seed,
            maxTicks,
            SupplementalAbilities,
            cancellationToken,
            basicAttackIntervalTicks: 30,
            startActiveAbilitiesOnCooldown: false,
            captureEventLog: false,
            friendlyPartyNumbers: cooperativeParty?.Slots
                .Select(slot => (int?)slot.PartyNumber)
                .ToArray());

        CombatResult? openingDiagnostic = null;
        if (scenario == CombatPacingScenario.StandardEnemyTtk)
        {
            openingDiagnostic = await _simulations.RunCombatAsync(
                friendly,
                new[] { CreateTarget(targetHealth) },
                seed,
                30,
                SupplementalAbilities,
                cancellationToken,
                basicAttackIntervalTicks: 30,
                startActiveAbilitiesOnCooldown: false,
                captureEventLog: true);
        }

        double? pressureBreakpoint = null;
        if (isDurability && result.Outcome == BattleOutcome.Draw &&
            role is CanonicalCombatRole.Sustain or CanonicalCombatRole.Defensive)
        {
            pressureBreakpoint = await FindPressureBreakpointAsync(
                friendly,
                seed,
                maxTicks,
                cancellationToken);
        }

        return ToSample(
            result,
            seed,
            targetHealth,
            scenario,
            pressureBreakpoint,
            openingDiagnostic,
            cooperativeParty?.Slots);
    }

    private async Task<CanonicalPartySetup> CreatePartyAsync(
        int partySize,
        int buildTier,
        int encounterTier,
        CancellationToken cancellationToken)
    {
        var slots = CanonicalCooperativeRosterCatalog.CreateParty(partySize);
        var party = new List<CombatEntity>(partySize);
        foreach (var slot in slots)
        {
            party.Add(await CreatePlayerAsync(
                slot.Role,
                buildTier,
                encounterTier,
                cancellationToken));
        }
        return new CanonicalPartySetup(party, slots);
    }

    private async Task<CombatEntity> CreatePlayerAsync(
        CanonicalCombatRole role,
        int buildTier,
        int encounterTier,
        bool rawDurability,
        CancellationToken cancellationToken)
    {
        var rung = _builds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == buildTier &&
            candidate.Quality == Domain.Models.Items.ItemQuality.Standard &&
            candidate.Rarity == Domain.Models.Items.Rarity.Common);
        var build = _builds.CreateBuild(ToProfile(role), rung, FullCanonicalEssenceCount);
        var combatant = await _simulations.CreateCanonicalCombatantAsync(build, cancellationToken);
        NormalizeFlatProgression(combatant, encounterTier);
        if (rawDurability)
            DisableRecoveryAndDefensiveActives(combatant);
        return combatant;
    }

    private async Task<CombatEntity> CreatePlayerAsync(
        CanonicalCooperativeRole role,
        int buildTier,
        int encounterTier,
        CancellationToken cancellationToken)
    {
        var rung = _builds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == buildTier &&
            candidate.Quality == Domain.Models.Items.ItemQuality.Standard &&
            candidate.Rarity == Domain.Models.Items.Rarity.Common);
        var build = _builds.CreateBuild(role, rung, FullCanonicalEssenceCount);
        var combatant = await _simulations.CreateCanonicalCombatantAsync(build, cancellationToken);
        NormalizeFlatProgression(combatant, encounterTier);
        return combatant;
    }

    private static void NormalizeFlatProgression(CombatEntity combatant, int encounterTier)
    {
        var divisor = EquipmentTierBudgetCurve.GetScale(encounterTier);
        foreach (var attribute in new[]
                 {
                     AttributeType.Power,
                     AttributeType.MaxHealth,
                     AttributeType.HealthRegeneration
                 })
        {
            if (combatant.BaseCombatAttributes.TryGetValue(attribute, out var value))
            {
                var characterBase = combatant.BaseAttributes
                    .FirstOrDefault(candidate => candidate.AttributeType == attribute)
                    ?.Value ?? 0f;
                var referenceBase = EntityBaseAttributeHelper.GetValueForCharacterLevel(
                    attribute,
                    ReferenceCharacterLevel);
                combatant.BaseCombatAttributes[attribute] = (float)(
                    referenceBase + (value - characterBase) / divisor);
            }
        }

        AttributeCalculator.InitializeCombatAttributesFromBase(combatant);
    }

    private static void DisableRecoveryAndDefensiveActives(CombatEntity combatant)
    {
        combatant.NativeAbilityIds.Clear();
        combatant.EquippedEssences.Clear();
        combatant.Tags.RemoveWhere(tag =>
            tag.StartsWith("Essence.", StringComparison.OrdinalIgnoreCase));
        foreach (var attribute in new[]
                 {
                     AttributeType.HealingPowerPercent,
                     AttributeType.HealthRegeneration,
                     AttributeType.LifeSteal,
                     AttributeType.Cooldown
                 })
        {
            combatant.BaseCombatAttributes[attribute] = 0;
        }
        combatant.TemporaryModifiers.RemoveAll(modifier =>
            modifier.AttributeType is AttributeType.HealingPowerPercent or
                AttributeType.HealthRegeneration or
                AttributeType.LifeSteal or
                AttributeType.Cooldown);
        AttributeCalculator.InitializeCombatAttributesFromBase(combatant);
    }

    private async Task<double?> FindPressureBreakpointAsync(
        IReadOnlyList<CombatEntity> friendly,
        int seed,
        int maxTicks,
        CancellationToken cancellationToken)
    {
        foreach (var multiplier in new[] { 1.10d, 1.25d, 1.50d, 2d, 3d, 4d })
        {
            var result = await _simulations.RunCombatAsync(
                friendly,
                CreatePressureSources(multiplier),
                seed,
                Math.Max(maxTicks, 2_400),
                SupplementalAbilities,
                cancellationToken,
                basicAttackIntervalTicks: 30,
                startActiveAbilitiesOnCooldown: false,
                captureEventLog: false);
            if (result.Outcome == BattleOutcome.Defeat)
                return multiplier;
        }
        return null;
    }

    private static CombatEntity[] CreatePressureSources(double multiplier)
    {
        var pressure = CreateCombatant(
            "Canonical mixed-pressure source",
            TargetDummyHealth,
            (float)(MixedPressurePower * multiplier),
            armor: 80,
            resistance: 80);
        pressure.NativeAbilityIds.Add(MixedPressureAbilityId);
        return [pressure];
    }

    private static CombatEntity CreateTarget(int maxHealth) =>
        CreateCombatant(
            "Canonical pacing reference target",
            maxHealth,
            power: 0,
            armor: 20,
            resistance: 20,
            attackSpeed: -75);

    private static CombatEntity CreatePartyBoss(int maxHealth)
    {
        var boss = CreateCombatant(
            "Canonical cooperative reference boss",
            maxHealth,
            power: 135,
            armor: 20,
            resistance: 20,
            attackSpeed: -20);
        boss.NativeAbilityIds.Add(MixedPressureAbilityId);
        return boss;
    }

    private static CombatEntity CreateCombatant(
        string name,
        float maxHealth,
        float power,
        float armor,
        float resistance,
        float attackSpeed = 0)
    {
        var source = new Character
        {
            Id = Guid.Empty,
            Name = name,
            Level = 1,
            BaseAttributes =
            [
                Attribute(AttributeType.MaxHealth, maxHealth),
                Attribute(AttributeType.Power, power),
                Attribute(AttributeType.Armor, armor),
                Attribute(AttributeType.Resistance, resistance),
                Attribute(AttributeType.AttackSpeed, attackSpeed),
                Attribute(AttributeType.CritChance, 0),
                Attribute(AttributeType.CritDamage, 0)
            ]
        };
        var combatant = new CombatEntity(source) { HasEquippedEssenceSnapshot = true };
        AttributeCalculator.CalculateBaseCombatAttributes(combatant);
        combatant.Reset();
        return combatant;
    }

    private static EntityAttribute Attribute(AttributeType type, float value) => new()
    {
        AttributeType = type,
        Value = value
    };

    private static CombatPacingSample ToSample(
        CombatResult result,
        int seed,
        int initialTargetHealth,
        CombatPacingScenario scenario,
        double? pressureBreakpoint,
        CombatResult? openingDiagnostic,
        IReadOnlyList<CanonicalCooperativeRosterSlot>? cooperativeSlots)
    {
        var fixedWindow = scenario is CombatPacingScenario.OffensiveWindow90 or
            CombatPacingScenario.OffensiveWindow120;
        var outcome = fixedWindow
            ? CombatPacingOutcome.WindowCompleted
            : result.Outcome switch
            {
                BattleOutcome.Victory => CombatPacingOutcome.Victory,
                BattleOutcome.Defeat => CombatPacingOutcome.Defeat,
                _ => CombatPacingOutcome.Draw
            };
        var friendlyStats = result.EntityStats.Where(stat =>
            stat.Team.Equals("Friendly", StringComparison.OrdinalIgnoreCase)).ToArray();
        var friendlyDamageEvents = (openingDiagnostic?.EventLog ?? result.EventLog).Where(item =>
            item.ActorId.StartsWith("power-friendly-", StringComparison.OrdinalIgnoreCase) &&
            item.EventType is EventType.Damage or EventType.DamageCrit or EventType.DamageOverTime)
            .ToArray();
        var firstBasic = friendlyDamageEvents
            .FirstOrDefault(item => item.Source.Equals("Basic Attack", StringComparison.OrdinalIgnoreCase))
            ?.Magnitude ?? 0;
        var openingBurst = friendlyDamageEvents
            .Where(item => item.Timestamp <= 30)
            .Sum(item => Math.Max(0, item.Magnitude));
        var totalMaxHealth = result.PlayerTeam.Sum(entity => Math.Max(0, entity.MaxHealth));
        var remainingHealth = result.PlayerTeam.Sum(entity => Math.Max(0, entity.Health));
        var abilityStats = friendlyStats.SelectMany(stat => stat.Abilities).ToArray();
        var telemetry = new CombatPacingTelemetry(
            BasicAttacks: abilityStats
                .Where(ability => ability.Name.Equals("Basic Attack", StringComparison.OrdinalIgnoreCase))
                .Sum(ability => (double)ability.Uses),
            AbilityActivations: abilityStats
                .Where(ability => !ability.Name.Equals("Basic Attack", StringComparison.OrdinalIgnoreCase))
                .Sum(ability => (double)ability.Uses),
            DamageDone: friendlyStats.Sum(stat => (double)stat.DamageDone),
            HealingDone: friendlyStats.Sum(stat => (double)stat.HealingDone),
            LifeStealHealing: abilityStats
                .Where(ability => ability.Name.Contains("life steal", StringComparison.OrdinalIgnoreCase)
                    || ability.Name.Contains("lifesteal", StringComparison.OrdinalIgnoreCase))
                .Sum(ability => (double)ability.TotalHealing),
            HealthRegenerated: friendlyStats.Sum(stat => (double)stat.HealthRegenerated),
            BarrierGenerated: friendlyStats.Sum(stat => (double)stat.BarrierGenerated),
            BarrierAbsorbed: friendlyStats.Sum(stat => (double)stat.DamageBlocked),
            IncomingRawDamage: friendlyStats.Sum(stat => (double)stat.IncomingRawDamage),
            AvoidedDamage: friendlyStats.Sum(stat => (double)stat.AvoidedDamage),
            AvoidedAttacks: friendlyStats.Sum(stat => (double)stat.AvoidedAttacks),
            TypedMitigationPrevented: friendlyStats.Sum(stat => (double)stat.TypedMitigationPrevented),
            PhysicalMitigationPrevented: friendlyStats.Sum(stat => (double)stat.PhysicalMitigationPrevented),
            MagicalMitigationPrevented: friendlyStats.Sum(stat => (double)stat.MagicalMitigationPrevented),
            BlockPrevented: friendlyStats.Sum(stat => (double)stat.BlockPrevented),
            DamageReductionPrevented: friendlyStats.Sum(stat => (double)stat.DamageReductionPrevented),
            FinalHealthDamage: friendlyStats.Sum(stat => (double)stat.FinalHealthDamage));

        CooperativeCombatPacingTelemetry? cooperativeTelemetry = null;
        if (cooperativeSlots is not null)
        {
            var rolesByEntityId = cooperativeSlots.ToDictionary(
                slot => $"power-friendly-{slot.SlotIndex + 1}",
                slot => slot.Role,
                StringComparer.OrdinalIgnoreCase);
            var guardianStats = friendlyStats.Where(stat =>
                rolesByEntityId.TryGetValue(stat.EntityId, out var mappedRole) &&
                mappedRole == CanonicalCooperativeRole.Guardian).ToArray();
            var restorerStats = friendlyStats.Where(stat =>
                rolesByEntityId.TryGetValue(stat.EntityId, out var mappedRole) &&
                mappedRole == CanonicalCooperativeRole.Restorer).ToArray();
            cooperativeTelemetry = new CooperativeCombatPacingTelemetry(
                cooperativeSlots.Count,
                guardianStats.Sum(stat => stat.AttentionSharePercent),
                restorerStats.Sum(stat => stat.AttentionSharePercent),
                friendlyStats
                    .Where(stat => rolesByEntityId.TryGetValue(stat.EntityId, out var mappedRole) &&
                        mappedRole != CanonicalCooperativeRole.Guardian)
                    .Sum(stat => stat.AttentionSharePercent),
                guardianStats.Sum(stat => (double)stat.ThreatGenerated),
                restorerStats.Sum(stat => (double)stat.ThreatGenerated),
                guardianStats.Sum(stat => (double)stat.IncomingRawDamage),
                restorerStats.Sum(stat => (double)stat.HealingDone),
                guardianStats.Sum(stat => (double)stat.DamageRedirectedTo),
                result.PlayerTeam.Count(entity => entity.Health > 0));
        }

        return new CombatPacingSample(
            seed,
            result.Duration,
            outcome,
            friendlyStats.Sum(stat => (double)stat.DamageDone),
            firstBasic / (double)initialTargetHealth * 100d,
            openingBurst / (double)initialTargetHealth * 100d,
            totalMaxHealth <= 0 ? 0 : remainingHealth / (double)totalMaxHealth * 100d,
            PressureBreakpoint: pressureBreakpoint,
            Telemetry: telemetry,
            CooperativeTelemetry: cooperativeTelemetry);
    }

    private static CanonicalPartyProfile ToProfile(CanonicalCombatRole role) => role switch
    {
        CanonicalCombatRole.Offense => CanonicalPartyProfile.Offense,
        CanonicalCombatRole.Balanced => CanonicalPartyProfile.Balanced,
        CanonicalCombatRole.Sustain => CanonicalPartyProfile.Sustain,
        CanonicalCombatRole.Defensive => CanonicalPartyProfile.Defensive,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    private sealed record CanonicalPartySetup(
        IReadOnlyList<CombatEntity> Combatants,
        IReadOnlyList<CanonicalCooperativeRosterSlot> Slots);
}
