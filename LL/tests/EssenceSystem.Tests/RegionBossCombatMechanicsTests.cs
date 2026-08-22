using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

public sealed class RegionBossCombatMechanicsTests
{
    [Fact]
    public void Endless_wave_factory_spawns_lazily_and_applies_level_recovery()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly, maxHealth: 100, power: 500);
        friendly.SetHealth(50);
        var initial = Combatant("boss-1", CombatTeam.Hostile, maxHealth: 1, power: 0, canBasicAttack: false);
        var requestedLevels = new List<int>();
        var checkpoints = new List<CombatCheckpoint>();
        var engine = Engine(new FastCombatEngineOptions(
            MaxTicks: 3,
            BasicAttackIntervalTicks: 1,
            WaveRecovery: new CombatWaveRecoveryOptions(20, 25)));

        engine.Run(
            [friendly],
            [initial],
            checkpointObserver: checkpoints.Add,
            checkpointIntervalTicks: 1,
            hostileWaveFactory: level =>
            {
                requestedLevels.Add(level);
                return [Combatant($"boss-{level}", CombatTeam.Hostile, maxHealth: 1, power: 0, canBasicAttack: false)];
            });

        Assert.Equal([2, 3, 4], requestedLevels);
        Assert.Equal(100, friendly.Health);
        Assert.Equal(4, checkpoints[^1].Context?.WaveNumber);
    }

    [Fact]
    public void Fury_increases_only_hostile_damage_over_time()
    {
        var baseline = RunHostileDamage(null);
        var withFury = RunHostileDamage(new CombatHostileFuryOptions(
            IntervalTicks: 2,
            PowerPercentPerStack: 100,
            AttackSpeedPercentPerStack: 0));

        Assert.True(withFury > baseline, $"Expected Fury damage to exceed baseline. Baseline={baseline}, Fury={withFury}.");
    }

    [Fact]
    public void Downed_party_member_revives_after_fifteen_seconds_at_half_health_while_an_ally_remains_alive()
    {
        var strike = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "region-boss-test-strike",
            Name = "Region Boss Test Strike",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "region-boss-test-strike-effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.AllEnemies,
                    BaseValue = 50,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Physical,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        });
        var downed = Combatant("downed", CombatTeam.Friendly, maxHealth: 20, power: 0, canBasicAttack: false);
        var survivor = Combatant("survivor", CombatTeam.Friendly, maxHealth: 1_000, power: 0, canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, maxHealth: 1_000, power: 0,
            abilities: [strike], canBasicAttack: false);
        var engine = Engine(
            new FastCombatEngineOptions(
                MaxTicks: 152,
                BasicAttackIntervalTicks: 1_000,
                Downed: new CombatDownedOptions(150, 100, 600, 50)),
            [strike]);

        var result = engine.Run([downed, survivor], [hostile]);
        var stats = Assert.Single(result.EntityStats, x => x.EntityId == downed.Id);

        Assert.True(downed.IsAlive);
        Assert.Equal(10, downed.Health);
        Assert.Equal(1, stats.Deaths);
        Assert.Equal(1, stats.Revivals);
        Assert.Equal(150, stats.DownedTicks);
    }

    private static int RunHostileDamage(CombatHostileFuryOptions? fury)
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly, maxHealth: 100_000, power: 0, canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, maxHealth: 100_000, power: 100);
        var result = Engine(new FastCombatEngineOptions(
            MaxTicks: 6,
            BasicAttackIntervalTicks: 1,
            RandomSeed: 42,
            HostileFury: fury)).Run([friendly], [hostile]);
        return Assert.Single(result.EntityStats, x => x.EntityId == hostile.Id).DamageDone;
    }

    private static FastCombatEngine Engine(
        FastCombatEngineOptions options,
        IReadOnlyList<CompiledAbility>? abilities = null) =>
        new(
            new Dictionary<string, CompiledStatus>(),
            new Dictionary<string, CompiledSummon>(),
            (abilities ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase),
            options);

    private static RuntimeCombatant Combatant(
        string id,
        CombatTeam team,
        int maxHealth,
        int power,
        IReadOnlyList<CompiledAbility>? abilities = null,
        bool canBasicAttack = true) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = power,
                [AttributeType.CritDamage] = 100,
                [AttributeType.AttackSpeed] = 0
            },
            abilities ?? [],
            canBasicAttack: canBasicAttack);
}
