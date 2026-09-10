using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatStyleMilestoneEngineTests
{
    private static readonly Guid Channeled = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly CombatStyleMilestoneTuning Milestones = new()
    {
        OpeningBarrierFraction = .05,
        OpeningCharge = 1,
        PreparedWallEmptyBarrier = true,
        HoldTheBreachHealthBonus = .20,
        MeasuredRecoveryOverhealBarrierFraction = 1,
        FullCircuitMinimumCharge = 2,
        PartialFlowMaximumCharge = 2,
        EmergencyChannelHealthThreshold = 1
    };

    [Theory]
    [InlineData(6, 0)]
    [InlineData(7, 50)]
    public void Entrenched_unlocks_at_seven_and_is_present_in_initial_checkpoint(int level, int barrier)
    {
        var actor = Actor([], Bastion() with { Level = level, MilestoneTuning = Milestones });
        var checkpoints = new List<CombatCheckpoint>();
        var result = Engine().Run([actor], [Enemy()], checkpointObserver: checkpoints.Add, checkpointIntervalTicks: 1);

        Assert.Equal(barrier, Assert.Single(checkpoints[0].Friendly).Barrier);
        Assert.Equal(barrier, actor.Barrier);
        Assert.Equal(barrier, result.EntityStats.Single(x => x.EntityId == actor.Id).BarrierGenerated);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ConvertedBarrierGranted);
        if (level == 7)
        {
            var opening = Assert.Single(checkpoints[0].Events);
            Assert.Equal("Entrenched", opening.Source);
            Assert.Equal(0, opening.Timestamp);
            Assert.Equal(barrier, opening.CombatEntity!.Barrier);
        }
        else
            Assert.Empty(checkpoints[0].Events);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Opening_barrier_obeys_cap_and_records_only_accepted_amount_without_gain_reactions(bool captureLog)
    {
        var actor = Actor([Passive("gain", AbilityTriggerEvent.OnBarrierApplied, Heal(200))],
            Bastion() with { Level = 7, MilestoneTuning = Milestones });
        actor.SetHealth(500);
        actor.GrantBarrier(actor, 2490);
        var checkpoints = new List<CombatCheckpoint>();
        var result = Engine(captureLog: captureLog).Run([actor], [Enemy()],
            checkpointObserver: checkpoints.Add, checkpointIntervalTicks: 1);

        Assert.Equal(500, actor.Health);
        Assert.Equal(2500, actor.Barrier);
        Assert.Equal(10, checkpoints[0].EntityStats.Single(x => x.EntityId == actor.Id).BarrierGenerated);
        Assert.Equal(10, result.EntityStats.Single(x => x.EntityId == actor.Id).BarrierGenerated);
        Assert.Equal(0, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Theory]
    [InlineData(6, 0, 160)]
    [InlineData(7, 1, 200)]
    public void Primed_circuit_unlocks_at_seven_before_first_channeled_cast(int level, int charge, int damage)
    {
        var (abilities, origins) = Circuit(0, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit() with { Level = level, MilestoneTuning = Milestones }, origins);
        var enemy = Enemy();
        var result = Run(actor, enemy);

        Assert.Equal(damage, 10000 - enemy.Health);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(charge, summary.ChargeGenerated);
        Assert.Equal(charge, summary.ChargeSpent);
        Assert.Equal(1, summary.ChanneledCastsByCharge[charge]);
        if (level == 7)
            Assert.Equal("Primed Circuit", result.EventLog[0].Source);
    }

    [Fact]
    public void Opening_charge_is_capped_and_does_not_claim_a_contributor()
    {
        var (abilities, origins) = Circuit(1, Ability("channeled", Damage(200)));
        var style = Conduit() with
        {
            Level = 7, Tuning = new() { ChargeCap = 2 },
            MilestoneTuning = Milestones with { OpeningCharge = 5 }
        };
        var summary = Assert.Single(Run(Actor(abilities, style, origins)).CombatStyles);
        Assert.Equal(2, summary.ChargeGenerated);
        Assert.Equal(2, summary.ChargeSpent);
        Assert.Equal(1, summary.ChanneledCastsByCharge[2]);
        Assert.Empty(summary.Contributors);
    }

    [Fact]
    public void Summons_receive_no_opening_or_mastery_even_when_owner_has_both()
    {
        var style = Bastion(CombatStyleIds.PreparedWall) with { MilestoneTuning = Milestones };
        var actor = Actor([], style);
        var summon = Actor([Ability("heal", Heal(200))], style, id: "summon", owner: actor);
        summon.SetHealth(500);
        var result = Run(actor, allies: [summon]);

        Assert.Equal(50, actor.Barrier);
        Assert.Equal(700, summon.Health);
        Assert.Equal(0, summon.Barrier);
        Assert.Null(summon.CombatStyle);
        Assert.Equal(actor.Id, Assert.Single(result.CombatStyles).EntityId);
    }

    [Fact]
    public void Opening_resets_for_new_encounters_and_does_not_repeat_between_continuous_waves()
    {
        var style = Conduit() with { Level = 7, MilestoneTuning = Milestones };
        var firstEnemy = Enemy([Ability("end-wave", Damage(10000, AbilityTargetSelector.Self))]);
        var initial = Actor([], style);
        var firstResult = Engine(ticks: 2).Run([initial], [firstEnemy], hostileReinforcementWaves: [[Enemy(id: "second")]]);
        var nextResult = Run(Actor([], style));

        Assert.Equal(1, Assert.Single(firstResult.CombatStyles).ChargeGenerated);
        Assert.Equal(1, Assert.Single(firstResult.CombatStyles).Charge);
        Assert.Single(firstResult.EventLog.Where(x => x.Source == "Primed Circuit"));
        Assert.Equal(1, Assert.Single(nextResult.CombatStyles).ChargeGenerated);
        Assert.Equal(1, Assert.Single(nextResult.CombatStyles).Charge);
    }

    [Fact]
    public void Old_committed_snapshot_without_milestone_tuning_does_not_gain_opening_or_mastery()
    {
        var snapshot = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"bastion","Kind":1,"Level":10,
             "UpgradeIds":["hold-the-breach"],"MasteredUpgradeId":"hold-the-breach"}
            """)!;
        var actor = Actor([Ability("heal", Heal(200))], snapshot);
        actor.SetHealth(500);
        var result = Run(actor);

        Assert.Equal(550, actor.Health);
        Assert.Equal(165, actor.Barrier);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "Entrenched");
    }

    [Theory]
    [InlineData(8, 500, 0, 150)]
    [InlineData(9, 500, 0, 165)]
    [InlineData(9, 500, 1, 151)]
    [InlineData(9, 800, 0, 165)]
    public void Prepared_wall_mastery_adds_empty_barrier_condition_once(int level, int health, int initialBarrier, int barrier)
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.PreparedWall) with { Level = level });
        actor.SetHealth(health);
        actor.GrantBarrier(actor, initialBarrier);
        Run(actor);

        Assert.Equal(barrier, actor.Barrier);
        Assert.Equal(health + 50, actor.Health);
    }

    [Theory]
    [InlineData(8, null, 500, 0, 560, 165)]
    [InlineData(9, null, 500, 0, 572, 165)]
    [InlineData(9, null, 500, 1, 560, 151)]
    [InlineData(9, "rebuild", 200, 0, 440, 0)]
    public void Hold_the_breach_mastery_multiplies_allocated_health_after_other_upgrades(
        int level, string? refinement, int health, int initialBarrier, int finalHealth, int barrier)
    {
        var style = Bastion(CombatStyleIds.HoldTheBreach) with
        {
            Level = level, RefinementId = refinement,
            UpgradeIds = [CombatStyleIds.HoldTheBreach, CombatStyleIds.MeasuredRecovery]
        };
        var actor = Actor([Ability("heal", Heal(200))], style);
        actor.SetHealth(health);
        actor.GrantBarrier(actor, initialBarrier);
        Run(actor);

        Assert.Equal(finalHealth, actor.Health);
        Assert.Equal(barrier, actor.Barrier);
    }

    [Theory]
    [InlineData(8, 980, 150, 40)]
    [InlineData(9, 980, 190, 0)]
    [InlineData(9, 1000, 210, 0)]
    [InlineData(9, 500, 150, 0)]
    public void Measured_recovery_mastery_converts_actual_excess_allocated_health(int level, int health, int barrier, int wasted)
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.MeasuredRecovery) with { Level = level });
        actor.SetHealth(health);
        var result = Run(actor);

        Assert.Equal(Math.Min(1000, health + 60), actor.Health);
        Assert.Equal(barrier, actor.Barrier);
        Assert.Equal(wasted, Assert.Single(result.CombatStyles).HealthRecoveryWasted);
    }

    [Fact]
    public void Measured_recovery_mastery_uses_shelter_distribution_and_each_recipients_cap_without_gain_loops()
    {
        var style = Bastion(CombatStyleIds.MeasuredRecovery) with { RefinementId = CombatStyleIds.Shelter };
        var reaction = Passive("gain", AbilityTriggerEvent.OnBarrierApplied, Heal(200));
        var actor = Actor([Ability("heal", Heal(200)), reaction], style);
        actor.GrantBarrier(actor, 2490);
        var ally = Actor([reaction], id: "ally");
        ally.GrantBarrier(ally, 2495);
        var result = Run(actor, allies: [ally]);

        Assert.Equal(2500, actor.Barrier);
        Assert.Equal(2500, ally.Barrier);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(200, summary.HealingConverted);
        Assert.Equal(15, summary.ConvertedBarrierGranted);
        Assert.Equal(195, summary.BarrierOverflow);
        Assert.Equal(5, summary.ShelterRecipients[ally.Id]);
        Assert.Equal(0, summary.HealthRecoveryWasted);
    }

    [Fact]
    public void Measured_recovery_mastery_converts_rebuild_overheal_without_amplifying_full_heal()
    {
        var actor = Actor([Ability("heal", Heal(1000))],
            Bastion(CombatStyleIds.MeasuredRecovery) with { RefinementId = CombatStyleIds.Rebuild });
        actor.SetHealth(350);
        Run(actor);

        Assert.Equal(1000, actor.Health);
        Assert.Equal(350, actor.Barrier);
    }

    [Fact]
    public void Only_the_selected_equipped_upgrade_receives_mastery()
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.PreparedWall) with
        {
            UpgradeIds = [CombatStyleIds.PreparedWall, CombatStyleIds.HoldTheBreach]
        });
        actor.SetHealth(500);
        Run(actor);
        Assert.Equal(550, actor.Health);
        Assert.Equal(180, actor.Barrier);

        var missing = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.HoldTheBreach) with
        {
            UpgradeIds = [CombatStyleIds.PreparedWall]
        });
        missing.SetHealth(500);
        Run(missing);
        Assert.Equal(550, missing.Health);
        Assert.Equal(150, missing.Barrier);
    }

    [Theory]
    [InlineData(8, "full-circuit", 2, 240)]
    [InlineData(9, "full-circuit", 0, 160)]
    [InlineData(9, "full-circuit", 1, 200)]
    [InlineData(9, "full-circuit", 2, 250)]
    [InlineData(9, "full-circuit", 3, 290)]
    [InlineData(8, "partial-flow", 2, 240)]
    [InlineData(9, "partial-flow", 0, 160)]
    [InlineData(9, "partial-flow", 1, 210)]
    [InlineData(9, "partial-flow", 2, 250)]
    [InlineData(9, "partial-flow", 3, 280)]
    public void Charge_upgrade_masteries_expand_conditions_without_stacking_their_bonus(int level, string upgrade, int charge, int damage)
    {
        var (abilities, origins) = Circuit(charge, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit(upgrade) with { Level = level }, origins);
        var enemy = Enemy();
        Run(actor, enemy);
        Assert.Equal(damage, 10000 - enemy.Health);
    }

    [Theory]
    [InlineData(8, 1, 350, 210)]
    [InlineData(8, 1, 500, 200)]
    [InlineData(9, 0, 500, 160)]
    [InlineData(9, 1, 500, 210)]
    [InlineData(9, 3, 500, 290)]
    public void Emergency_mastery_boosts_only_immediate_self_recovery_on_charged_channeled(
        int level, int charge, int health, int selfRecovery)
    {
        var healAlly = Heal(200); healAlly.Id = "ally-heal"; healAlly.Target = AbilityTargetSelector.AllAllies;
        var periodic = Heal(20); periodic.Id = "periodic"; periodic.DurationTicks = 10; periodic.IntervalTicks = 1;
        var (abilities, origins) = Circuit(charge, Ability("channeled", Damage(200), Heal(200), Barrier(200), healAlly, periodic));
        abilities.Add(Passive("passive", AbilityTriggerEvent.OnAbilityUsed, Barrier(10)));
        var actor = Actor(abilities, Conduit(CombatStyleIds.EmergencyChannel) with { Level = level }, origins);
        actor.SetHealth(health);
        var ally = Actor([], id: "ally"); ally.SetHealth(100);
        var enemy = Enemy();
        Run(actor, enemy, [ally]);
        var ordinary = 160 + charge * 40;

        Assert.Equal(ordinary, 10000 - enemy.Health);
        Assert.Equal(100 + ordinary, ally.Health);
        Assert.Equal(selfRecovery + 10 * (charge + 1), actor.Barrier);
        Assert.Equal(Math.Min(1000, health + selfRecovery * 2 + 20), actor.Health);
    }

    private static CombatStyleSnapshot Bastion(string? mastered = null) => new()
    {
        CombatStyleId = CombatStyleIds.Bastion, Kind = CombatStyleKind.Bastion, Level = 9,
        UpgradeIds = mastered is null ? [] : [mastered], MasteredUpgradeId = mastered,
        // Isolate milestone behavior from the separately tested automatic level bonus.
        Tuning = new() { BarrierPerMasteryLevel = 0, ChanneledPerMasteryLevel = 0 },
        MilestoneTuning = Milestones with { OpeningBarrierFraction = 0, OpeningCharge = 0 }
    };

    private static CombatStyleSnapshot Conduit(string? mastered = null) => new()
    {
        CombatStyleId = CombatStyleIds.Conduit, Kind = CombatStyleKind.Conduit, Level = 9,
        ChanneledPlayerEssenceId = Channeled, UpgradeIds = mastered is null ? [] : [mastered], MasteredUpgradeId = mastered,
        Tuning = new() { BarrierPerMasteryLevel = 0, ChanneledPerMasteryLevel = 0 },
        MilestoneTuning = Milestones with { OpeningBarrierFraction = 0, OpeningCharge = 0 }
    };

    private static RuntimeCombatant Actor(IEnumerable<CompiledAbility> abilities, CombatStyleSnapshot? style = null,
        IReadOnlyDictionary<string, Guid>? origins = null, string id = "actor", RuntimeCombatant? owner = null) =>
        new(id, id, CombatTeam.Friendly, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000 },
            abilities, canBasicAttack: false, combatStyle: style, essenceOrigins: origins,
            isSummoned: owner is not null, summonOwner: owner);

    private static RuntimeCombatant Enemy(IEnumerable<CompiledAbility>? abilities = null, string id = "enemy") =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000 },
            abilities ?? [], canBasicAttack: false);

    private static FastCombatEngine Engine(int ticks = 1, bool captureLog = true) =>
        new(new Dictionary<string, CompiledStatus>(), new(MaxTicks: ticks, CaptureEventLog: captureLog));

    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant? enemy = null,
        IReadOnlyList<RuntimeCombatant>? allies = null) => Engine().Run([actor, .. allies ?? []], [enemy ?? Enemy()]);

    private static CompiledAbility Ability(string id, params AbilityEffectSpec[] effects) =>
        AbilityCompiler.CompileAbility(new AbilitySpec
        { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 1000, Effects = [.. effects] });

    private static CompiledAbility Passive(string id, AbilityTriggerEvent trigger, AbilityEffectSpec effect) =>
        AbilityCompiler.CompileAbility(new AbilitySpec
        { Id = id, Name = id, Kind = AbilitySpecKind.Passive, Triggers = [new() { Event = trigger }], Effects = [effect] });

    private static AbilityEffectSpec Heal(int value) => new()
    { Id = "heal", Operation = AbilityEffectOperation.Heal, Target = AbilityTargetSelector.Self, BaseValue = value, CritEligibility = CritEligibility.Disallowed };

    private static AbilityEffectSpec Barrier(int value) => new()
    { Id = "barrier", Operation = AbilityEffectOperation.GrantBarrier, Target = AbilityTargetSelector.Self, BaseValue = value, CritEligibility = CritEligibility.Disallowed };

    private static AbilityEffectSpec Damage(int value, AbilityTargetSelector target = AbilityTargetSelector.CurrentTarget) => new()
    { Id = "damage", Operation = AbilityEffectOperation.Damage, Target = target, BaseValue = value, CritEligibility = CritEligibility.Disallowed };

    private static (List<CompiledAbility> Abilities, Dictionary<string, Guid> Origins) Circuit(int contributors, CompiledAbility channeled)
    {
        var abilities = new List<CompiledAbility>();
        var origins = new Dictionary<string, Guid>();
        for (var index = 0; index < contributors; index++)
        {
            var id = "contributor-" + index;
            abilities.Add(Ability(id, new AbilityEffectSpec
            { Id = id, Operation = AbilityEffectOperation.ModifyThreat, Target = AbilityTargetSelector.Self, BaseValue = 1 }));
            origins.Add(id, Guid.NewGuid());
        }
        abilities.Add(channeled);
        origins.Add(channeled.Id, Channeled);
        return (abilities, origins);
    }
}
