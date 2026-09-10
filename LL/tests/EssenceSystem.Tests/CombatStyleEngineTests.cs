using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CombatStyleEngineTests
{
    private static readonly Guid Channeled = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly CombatStyleTuning CurrentTuning = new()
    { BarrierPerMasteryLevel = .01, ChanneledPerMasteryLevel = .01 };

    [Theory]
    [InlineData(800, false, 850, 150)]
    [InlineData(1000, false, 1000, 150)]
    [InlineData(800, true, 835, 105)]
    public void Bastion_converts_modified_healing_before_overheal(int start, bool wound, int health, int barrier)
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion());
        actor.SetHealth(start);
        if (wound) AddCondition(actor, StandardConditionType.Wound);
        var result = Run(actor);
        Assert.Equal(health, actor.Health);
        Assert.Equal(barrier, actor.Barrier);
        Assert.Equal(wound ? 140 : 200, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Theory]
    [InlineData(350, 550, 0)]
    [InlineData(351, 401, 150)]
    public void Rebuild_samples_inclusive_threshold_once(int start, int health, int barrier)
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.Rebuild));
        actor.SetHealth(start);
        Run(actor);
        Assert.Equal(health, actor.Health);
        Assert.Equal(barrier, actor.Barrier);
    }

    [Theory]
    [InlineData(800, 0, 10, 195)]
    [InlineData(799, 0, 10, 180)]
    [InlineData(800, 1, 10, 181)]
    [InlineData(799, 1, 10, 166)]
    public void Bastion_mastery_level_and_conditional_upgrades_add_before_distribution(int health, int initialBarrier, int level, int barrier)
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion() with
        {
            Level = level, UpgradeIds = [CombatStyleIds.PreparedWall, CombatStyleIds.HoldTheBreach]
        });
        actor.SetHealth(health);
        actor.GrantBarrier(actor, initialBarrier);
        Run(actor);
        Assert.Equal(barrier, actor.Barrier);
        Assert.Equal(health + 50, actor.Health);
    }

    [Fact]
    public void Measured_recovery_does_not_amplify_rebuild_or_barrier()
    {
        var style = Bastion() with { UpgradeIds = [CombatStyleIds.MeasuredRecovery] };
        var actor = Actor([Ability("heal", Heal(200))], style);
        actor.SetHealth(800);
        Run(actor);
        Assert.Equal(860, actor.Health);
        Assert.Equal(150, actor.Barrier);
        var rebuilding = Actor([Ability("heal", Heal(200))], style with { RefinementId = CombatStyleIds.Rebuild });
        rebuilding.SetHealth(350);
        Run(rebuilding);
        Assert.Equal(550, rebuilding.Health);
        Assert.Equal(0, rebuilding.Barrier);
    }

    [Theory]
    [InlineData(0, 150)]
    [InlineData(1, 151.5)]
    [InlineData(2, 153)]
    [InlineData(9, 163.5)]
    [InlineData(10, 165)]
    public void Every_bastion_mastery_level_improves_received_healing_barrier_without_changing_health(int level, double barrier)
    {
        var bastion = Actor([], Bastion() with { Level = level }); bastion.SetHealth(500);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var healer = Actor([Ability("ally-heal", heal)], id: "healer");
        Run(bastion, allies: [healer]);

        Assert.Equal(550, bastion.Health);
        Assert.Equal(barrier, bastion.Barrier, 5);
    }

    [Theory]
    [InlineData(CombatStyleIds.Rebuild, 350, 550, 0, 0)]
    [InlineData(CombatStyleIds.Rebuild, 351, 401, 163.5, 0)]
    [InlineData(CombatStyleIds.Reprisal, 500, 550, 163.5, 0)]
    [InlineData(CombatStyleIds.Shelter, 500, 550, 81.75, 81.75)]
    public void Odd_level_bastion_bonus_preserves_refinement_allocations(
        string refinement, int health, int expectedHealth, double barrier, double allyBarrier)
    {
        var bastion = Actor([Ability("heal", Heal(200))], Bastion(refinement) with { Level = 9 });
        bastion.SetHealth(health);
        var ally = Actor([], id: "ally"); ally.SetHealth(600);
        Run(bastion, allies: [ally]);

        Assert.Equal(expectedHealth, bastion.Health);
        Assert.Equal(barrier, bastion.Barrier, 5);
        Assert.Equal(allyBarrier, ally.Barrier, 5);
    }

    [Fact]
    public void Shelter_remains_useful_with_full_owner_pools_and_selects_first_lowest_percentage_ally()
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion(CombatStyleIds.Shelter));
        actor.GrantBarrier(actor, 2500);
        var first = Actor([], id: "first"); first.SetHealth(500);
        var second = Actor([], id: "second"); second.SetHealth(500);
        var result = Run(actor, allies: [first, second]);
        Assert.Equal(2500, actor.Barrier);
        Assert.Equal(75, first.Barrier);
        Assert.Equal(0, second.Barrier);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(75, summary.BarrierOverflow);
        Assert.Equal(75, summary.ShelterRecipients["first"]);
        Assert.Equal(50, summary.HealthRecoveryWasted);
    }

    [Fact]
    public void Recovery_only_cast_is_skipped_when_every_output_is_wasted_but_authored_conditions_are_preserved()
    {
        var actor = Actor([Ability("heal", Heal(200))], Bastion());
        actor.GrantBarrier(actor, 2500);
        Assert.DoesNotContain(Run(actor).EventLog, x => x.EventType == EventType.AbilityUse);
        var limited = Heal(200);
        limited.Conditions = [new() { Type = AbilityConditionType.HealthBelowPercent, Subject = AbilityConditionSubject.Source, Value = 35 }];
        var healthy = Actor([Ability("limited", limited)], Bastion());
        Run(healthy);
        Assert.Equal(0, healthy.Barrier);
    }

    [Fact]
    public void Converted_barrier_suppresses_gain_reactions_for_both_shelter_recipients_but_absorption_reacts()
    {
        var gainHeal = Passive("gain", AbilityTriggerEvent.OnBarrierApplied, Heal(40));
        var absorbHeal = Passive("absorb", AbilityTriggerEvent.OnBarrierAbsorbed, Heal(20));
        var actor = Actor([Ability("heal", Heal(200)), gainHeal, absorbHeal], Bastion(CombatStyleIds.Shelter));
        var ally = Actor([], id: "ally"); ally.SetHealth(500);
        var enemy = Enemy([Ability("hit", Damage(50))]);
        var result = Run(actor, enemy, [ally]);
        Assert.Equal(220, Assert.Single(result.CombatStyles).HealingConverted);
        Assert.Equal(50, Assert.Single(result.CombatStyles).ConvertedBarrierAbsorbed);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "gain.effect");
    }

    [Fact]
    public void Healing_received_from_own_and_another_players_summons_converts()
    {
        var actor = Actor([], Bastion()); actor.SetHealth(500);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var summon = Actor([Ability("heal", heal)], id: "summon", owner: actor);
        Run(actor, allies: [summon]);
        Assert.Equal(550, actor.Health); Assert.Equal(150, actor.Barrier);
        var outsideOwner = Actor([], id: "outside");
        var second = Actor([], Bastion()); second.SetHealth(500);
        var outsideSummon = Actor([Ability("heal", heal)], id: "outside-summon", owner: outsideOwner);
        Run(second, allies: [outsideOwner, outsideSummon]);
        Assert.Equal(550, second.Health); Assert.Equal(150, second.Barrier);
        Assert.Null(summon.CombatStyle);
    }

    [Fact]
    public void Allied_healer_keeps_lowest_health_target_even_when_bastion_has_large_barrier()
    {
        var bastion = Actor([], Bastion()); bastion.SetHealth(300); bastion.GrantBarrier(bastion, 2000);
        var other = Actor([], id: "other"); other.SetHealth(400);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var healer = Actor([Ability("ally-heal", heal)], id: "healer");

        var result = Run(bastion, allies: [other, healer]);

        Assert.Equal(350, bastion.Health);
        Assert.Equal(2150, bastion.Barrier);
        Assert.Equal(400, other.Health);
        Assert.Equal(0, other.Barrier);
        Assert.Equal(200, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Healing_non_bastion_ally_remains_normal_regardless_of_healers_style(bool healerIsBastion)
    {
        var recipient = Actor([], id: "recipient"); recipient.SetHealth(500);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var healer = Actor([Ability("ally-heal", heal)], healerIsBastion ? Bastion() : null, id: "healer");

        var result = Run(recipient, allies: [healer]);

        Assert.Equal(700, recipient.Health);
        Assert.Equal(0, recipient.Barrier);
        Assert.Equal(0, healer.Barrier);
        Assert.All(result.CombatStyles, summary => Assert.Equal(0, summary.HealingConverted));
    }

    [Fact]
    public void Bastion_healing_its_summon_does_not_give_the_summon_fortification()
    {
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var owner = Actor([Ability("summon-heal", heal)], Bastion());
        var summon = Actor([], id: "summon", owner: owner); summon.SetHealth(500);

        var result = Run(owner, allies: [summon]);

        Assert.Equal(700, summon.Health);
        Assert.Equal(0, summon.Barrier);
        Assert.Equal(0, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Fact]
    public void External_healing_uses_rebuild_at_its_inclusive_threshold()
    {
        var bastion = Actor([], Bastion(CombatStyleIds.Rebuild)); bastion.SetHealth(350);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var healer = Actor([Ability("ally-heal", heal)], id: "healer");

        var result = Run(bastion, allies: [healer]);

        Assert.Equal(550, bastion.Health);
        Assert.Equal(0, bastion.Barrier);
        Assert.Equal(200, Assert.Single(result.CombatStyles).HealthRestored);
        Assert.Equal(0, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Fact]
    public void External_healing_applies_recipient_level_upgrades_mastery_and_shelter_caps_once()
    {
        var style = Bastion(CombatStyleIds.Shelter) with
        {
            Level = 10,
            UpgradeIds = [CombatStyleIds.PreparedWall, CombatStyleIds.MeasuredRecovery],
            MasteredUpgradeId = CombatStyleIds.MeasuredRecovery,
            MilestoneTuning = new() { MeasuredRecoveryOverhealBarrierFraction = 1 }
        };
        var gainReaction = Passive("barrier-gain-heal", AbilityTriggerEvent.OnBarrierApplied, Heal(200));
        var bastion = Actor([gainReaction], style); bastion.SetHealth(950); bastion.GrantBarrier(bastion, 2490);
        var sheltered = Actor([gainReaction], Bastion(), id: "sheltered"); sheltered.SetHealth(980); sheltered.GrantBarrier(sheltered, 2495);
        var heal = Heal(400); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var healer = Actor([Ability("ally-heal", heal)], id: "healer");

        var result = Run(bastion, allies: [sheltered, healer]);

        Assert.Equal(1000, bastion.Health);
        Assert.Equal(980, sheltered.Health);
        Assert.Equal(2500, bastion.Barrier);
        Assert.Equal(2500, sheltered.Barrier);
        var summary = result.CombatStyles.Single(x => x.EntityId == bastion.Id);
        // Health: 120 allocated, 50 restored, 70 converted; Barrier: 300 * (1 + .10 + .10) + 70 = 430, split once.
        Assert.Equal(400, summary.HealingConverted);
        Assert.Equal(15, summary.ConvertedBarrierGranted);
        Assert.Equal(415, summary.BarrierOverflow);
        Assert.Equal(5, summary.ShelterRecipients[sheltered.Id]);
        Assert.Equal(0, summary.HealthRecoveryWasted);
        Assert.Equal(0, result.CombatStyles.Single(x => x.EntityId == sheltered.Id).HealingConverted);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "barrier-gain-heal");
    }

    [Fact]
    public void External_group_heal_converts_before_clamping_full_health_bastion_recovery()
    {
        var bastion = Actor([], Bastion());
        var other = Actor([], id: "other"); other.SetHealth(500);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.AllAllies;
        var healer = Actor([Ability("group-heal", heal)], id: "healer");

        var result = Run(bastion, allies: [other, healer]);

        Assert.Equal(1000, bastion.Health);
        Assert.Equal(150, bastion.Barrier);
        Assert.Equal(700, other.Health);
        Assert.Equal(0, other.Barrier);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(200, summary.HealingConverted);
        Assert.Equal(50, summary.HealthRecoveryWasted);
    }

    [Fact]
    public void External_healer_preserves_authored_health_condition_when_bastion_has_barrier_capacity()
    {
        var bastion = Actor([], Bastion());
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        heal.Conditions = [new()
        {
            Type = AbilityConditionType.HealthBelowPercent, Subject = AbilityConditionSubject.Target, Value = 99
        }];
        var healer = Actor([Ability("conditional-heal", heal)], id: "healer");

        var result = Run(bastion, allies: [healer]);

        Assert.Equal(0, bastion.Barrier);
        Assert.Equal(0, Assert.Single(result.CombatStyles).HealingConverted);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public void Conduit_channeled_scales_external_healing_once_before_recipient_fortification()
    {
        var bastion = Actor([], Bastion() with { Level = 10 }); bastion.SetHealth(500);
        var heal = Heal(200); heal.Target = AbilityTargetSelector.LowestHealthAlly;
        var (abilities, origins) = Circuit(2, Ability("channeled", heal));
        var healer = Actor(abilities, Conduit() with { Level = 10 }, origins, id: "conduit-healer");

        var result = Run(bastion, allies: [healer]);

        // Channeled: 200 * (.80 + .40 + .10) = 260. Fortification then allocates 65 Health and 214.5 Barrier.
        Assert.Equal(565, bastion.Health);
        Assert.Equal(214.5f, bastion.Barrier);
        Assert.Equal(260, result.CombatStyles.Single(x => x.EntityId == bastion.Id).HealingConverted);
        Assert.Equal(2, result.CombatStyles.Single(x => x.EntityId == healer.Id).ChargeSpent);
    }

    [Fact]
    public void Regeneration_uses_its_own_modifiers_once_and_does_not_gain_healing_power()
    {
        var actor = Actor([], Bastion()); actor.SetHealth(500);
        actor.AdjustAttribute(AttributeType.HealthRegeneration, 200);
        actor.AdjustAttribute(AttributeType.HealingPowerPercent, 100);
        AddCondition(actor, StandardConditionType.Wound);
        var result = Run(actor, ticks: 50);
        Assert.Equal(535, actor.Health); Assert.Equal(105, actor.Barrier);
        Assert.Equal(140, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Fact]
    public void Legacy_counterweight_adds_once_without_crit_lifesteal_or_extra_hit_events()
    {
        var hit = Damage(100); hit.CritEligibility = CritEligibility.Allowed; hit.RepeatCount = 2;
        var onHit = Passive("onhit", AbilityTriggerEvent.OnHit, new AbilityEffectSpec
        {
            Id = "onhit.effect", Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self, EventMagnitudeCoefficient = 1
        });
        var actor = Actor([Ability("hit", hit), onHit], Bastion(CombatStyleIds.Counterweight),
            new Dictionary<string, Guid> { ["hit"] = Channeled });
        actor.SetHealth(500); actor.GrantBarrier(actor, 200);
        actor.AdjustAttribute(AttributeType.CritChance, 100);
        actor.AdjustAttribute(AttributeType.CritDamage, 100);
        actor.AdjustAttribute(AttributeType.LifeSteal, 100);
        var enemy = Enemy();
        var result = Run(actor, enemy);
        var damage = result.EventLog.Where(x => x.Source == "damage" && x.EventType == EventType.DamageCrit).ToArray();
        Assert.Equal(2, damage.Length);
        Assert.Equal(500, 10000 - enemy.Health);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(100, summary.CounterweightBarrierSpent);
        Assert.Equal(100, summary.CounterweightDamage, 6);
        // Only the two ordinary critical hits produce Lifesteal and hit-triggered protection.
        Assert.Equal(100, summary.HealthRestored);
        Assert.Equal(400, summary.HealingConverted);
        Assert.Equal(800, actor.Barrier);
    }

    [Fact]
    public void Legacy_counterweight_first_miss_loses_bonus_and_does_not_pass_it_to_later_hits()
    {
        var first = Damage(100); first.AttackType = AttackType.Melee;
        var second = Damage(100); second.Id = "second"; second.AttackType = AttackType.None;
        for (var seed = 0; seed < 100; seed++)
        {
            var actor = Actor([Ability("hit", first, second)], Bastion(CombatStyleIds.Counterweight),
                new Dictionary<string, Guid> { ["hit"] = Channeled }); actor.GrantBarrier(actor, 200);
            var enemy = Enemy(); enemy.AdjustAttribute(AttributeType.DodgeChance, 100);
            var result = Run(actor, enemy, seed: seed);
            if (!result.EventLog.Any(x => x.EventType == EventType.Miss)) continue;
            Assert.Equal(9900, enemy.Health);
            Assert.Equal(100, Assert.Single(result.CombatStyles).CounterweightBarrierSpent);
            Assert.Equal(0, Assert.Single(result.CombatStyles).CounterweightDamage);
            return;
        }
        Assert.Fail("The deterministic seed set should include a dodged first attempt.");
    }

    [Theory]
    [InlineData(null, 0, 160)] [InlineData(null, 1, 200)] [InlineData(null, 2, 240)] [InlineData(null, 3, 280)]
    [InlineData("short-circuit", 2, 240)]
    [InlineData("deep-reservoir", 0, 120)] [InlineData("deep-reservoir", 1, 170)]
    [InlineData("deep-reservoir", 2, 220)] [InlineData("deep-reservoir", 3, 270)] [InlineData("deep-reservoir", 4, 320)]
    [InlineData("relay", 0, 160)] [InlineData("relay", 1, 190)] [InlineData("relay", 2, 220)] [InlineData("relay", 3, 250)]
    public void Conduit_refinement_curves_use_whole_cast_charge(string? refinement, int contributors, int damage)
    {
        var (abilities, origins) = Circuit(contributors, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit(refinement), origins);
        var enemy = Enemy(); var result = Run(actor, enemy);
        Assert.Equal(damage, 10000 - enemy.Health);
        Assert.Equal(1, Assert.Single(result.CombatStyles).ChanneledCastsByCharge[contributors]);
        Assert.Equal(refinement == "relay" && contributors >= 2 ? 1 : 0, result.CombatStyles[0].Charge);
    }

    [Fact]
    public void Channeled_essence_rename_preserves_old_replay_messages_and_combat_results()
    {
        foreach (var version in Enumerable.Range(1, 6).Select(value => $"combat-styles.v{value}").Append(string.Empty))
        {
            var (abilities, origins) = Circuit(1, Ability("channeled", Damage(200)));
            var actor = Actor(abilities, Conduit() with { ContentVersion = version }, origins);
            var enemy = Enemy();
            var result = Run(actor, enemy);
            var message = Assert.Single(result.EventLog, item => item.Source == "Circuit");
            var expectedName = version is "combat-styles.v6" or "" ? "Channeled Essence" : "Focus";
            Assert.Equal($"{actor.Name}'s {expectedName} spent 1 Charge for {1d:P0} effect amounts.", message.Details);
            Assert.Equal(200, 10000 - enemy.Health);
            Assert.Equal(1, result.CombatStyles[0].ChanneledCastsByCharge[1]);
        }
    }

    [Fact]
    public void Repeated_contributors_charge_once_except_short_circuit_and_zero_channeled_resets_cycle()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var abilities = new[] { Contributor("a"), Contributor("b"), Contributor("a2"), Contributor("c"),
            Ability("f", Damage(200)), Ability("f2", Damage(200)), Contributor("b2"), Ability("f3", Damage(200)) };
        var origins = new Dictionary<string, Guid> { ["a"] = a, ["b"] = b, ["a2"] = a, ["c"] = c,
            ["f"] = Channeled, ["f2"] = Channeled, ["b2"] = b, ["f3"] = Channeled };
        var result = Run(Actor(abilities, Conduit(), origins));
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(1, summary.ChanneledCastsByCharge[3]); Assert.Equal(1, summary.ChanneledCastsByCharge[0]);
        Assert.Equal(1, summary.ChanneledCastsByCharge[1]); Assert.Equal(4, summary.ChargeGenerated);
        var repeated = new[] { Contributor("a"), Contributor("a2"), Ability("f", Damage(200)) };
        Assert.Equal(1, Run(Actor(repeated, Conduit(), origins)).CombatStyles[0].ChargeSpent);
        Assert.Equal(2, Run(Actor(repeated, Conduit(CombatStyleIds.ShortCircuit), origins)).CombatStyles[0].ChargeSpent);
    }

    [Fact]
    public void Channeled_multiplier_applies_to_each_immediate_hit_but_not_periodic_or_passive_damage()
    {
        var dot = Damage(20); dot.Id = "dot"; dot.DurationTicks = 10; dot.IntervalTicks = 1;
        var repeated = Damage(100); repeated.RepeatCount = 2;
        var (abilities, origins) = Circuit(3, Ability("channeled", repeated, dot));
        abilities.Add(Passive("proc", AbilityTriggerEvent.OnAbilityUsed, Damage(10)));
        var enemy = Enemy(); var result = Run(Actor(abilities, Conduit(), origins), enemy);
        Assert.Equal(280 + 20 + 40, 10000 - enemy.Health);
        Assert.Equal(3, result.CombatStyles[0].ChargeSpent);
        Assert.Equal(80, result.CombatStyles[0].ChanneledOutputAdded);
    }

    [Fact]
    public void Charged_damage_lifesteal_is_not_multiplied_twice()
    {
        var (abilities, origins) = Circuit(3, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit(), origins); actor.SetHealth(200);
        actor.AdjustAttribute(AttributeType.LifeSteal, 100);
        Run(actor);
        Assert.Equal(480, actor.Health);
    }

    [Theory]
    [InlineData(0, 350, 160, 160)] [InlineData(1, 350, 230, 240)]
    [InlineData(3, 350, 310, 320)] [InlineData(3, 351, 310, 310)]
    public void Conduit_mastery_level_and_upgrades_preserve_zero_charge_penalty_and_only_boost_emergency_self_recovery(
        int charge, int health, int damage, int barrier)
    {
        var style = Conduit() with
        {
            Level = 10,
            UpgradeIds = [charge == 1 ? CombatStyleIds.PartialFlow : CombatStyleIds.FullCircuit, CombatStyleIds.EmergencyChannel]
        };
        var (abilities, origins) = Circuit(charge, Ability("channeled", Damage(200), Barrier(200)));
        var actor = Actor(abilities, style, origins); actor.SetHealth(health);
        var enemy = Enemy(); Run(actor, enemy);
        Assert.Equal(damage, 10000 - enemy.Health);
        Assert.Equal(barrier, actor.Barrier);
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(1, 202)]
    [InlineData(2, 204)]
    [InlineData(9, 218)]
    [InlineData(10, 220)]
    public void Every_conduit_mastery_level_improves_a_charged_channeled(int level, int damage)
    {
        var (abilities, origins) = Circuit(1, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit() with { Level = level }, origins);
        var enemy = Enemy();
        Run(actor, enemy);
        Assert.Equal(damage, 10000 - enemy.Health);
    }

    [Theory]
    [InlineData(null, 0, 160)]
    [InlineData(null, 1, 206)]
    [InlineData(CombatStyleIds.ShortCircuit, 0, 160)]
    [InlineData(CombatStyleIds.ShortCircuit, 2, 246)]
    [InlineData(CombatStyleIds.DeepReservoir, 0, 120)]
    [InlineData(CombatStyleIds.DeepReservoir, 4, 326)]
    [InlineData(CombatStyleIds.Relay, 0, 160)]
    [InlineData(CombatStyleIds.Relay, 3, 256)]
    public void Odd_level_conduit_bonus_preserves_refinement_curves_and_zero_charge_amounts(
        string? refinement, int charge, int damage)
    {
        var (abilities, origins) = Circuit(charge, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit(refinement) with { Level = 3 }, origins);
        var enemy = Enemy();
        Run(actor, enemy);
        Assert.Equal(damage, 10000 - enemy.Health);
    }

    [Fact]
    public void Legacy_battles_use_captured_rank_bonus_instead_of_current_level_scaling()
    {
        var oldBastion = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"bastion","Kind":1,"ContentVersion":"combat-styles.v2","Level":9,"CoreRank":2,
             "Tuning":{"BarrierPerCoreRank":0.03}}
            """)!;
        var bastion = Actor([Ability("heal", Heal(200))], oldBastion);
        Run(bastion);
        Assert.Equal(159, bastion.Barrier);

        var oldConduit = JsonSerializer.Deserialize<CombatStyleSnapshot>("""
            {"CombatStyleId":"conduit","Kind":2,"ContentVersion":"combat-styles.v2","Level":9,"CoreRank":2,
             "Tuning":{"FocusPerCoreRank":0.03}}
            """)! with { ChanneledPlayerEssenceId = Channeled };
        var (abilities, origins) = Circuit(1, Ability("channeled", Damage(200)));
        var conduit = Actor(abilities, oldConduit, origins);
        var enemy = Enemy();
        Run(conduit, enemy);
        Assert.Equal(212, 10000 - enemy.Health);
    }

    [Fact]
    public void Relay_cannot_renew_itself_and_passives_and_native_actives_cannot_charge()
    {
        var (abilities, origins) = Circuit(2, Ability("channeled", Damage(200)));
        abilities.Add(Ability("channeled2", Damage(200))); origins.Add("channeled2", Channeled);
        abilities.Insert(0, Contributor("native"));
        var summary = Assert.Single(Run(Actor(abilities, Conduit(CombatStyleIds.Relay), origins)).CombatStyles);
        Assert.Equal(1, summary.ChanneledCastsByCharge[2]); Assert.Equal(1, summary.ChanneledCastsByCharge[1]);
        Assert.Equal(1, summary.RelayChargeReturned); Assert.Equal(0, summary.Charge);
    }

    [Fact]
    public void Blocked_casts_do_not_charge_but_missed_casts_do()
    {
        var (abilities, origins) = Circuit(2, Ability("channeled", Damage(200)));
        var actor = Actor(abilities, Conduit(), origins); AddCondition(actor, StandardConditionType.Silence);
        var summary = Assert.Single(Run(actor).CombatStyles);
        Assert.Equal(0, summary.ChargeGenerated); Assert.Empty(summary.ChanneledCastsByCharge);
        var miss = Damage(20); miss.AttackType = AttackType.Melee;
        for (var seed = 0; seed < 100; seed++)
        {
            var origins2 = new Dictionary<string, Guid> { ["miss"] = Guid.NewGuid(), ["channeled"] = Channeled };
            var caster = Actor([Ability("miss", miss), Ability("channeled", Damage(200))], Conduit(), origins2);
            var enemy = Enemy(); enemy.AdjustAttribute(AttributeType.DodgeChance, 100);
            var result = Run(caster, enemy, seed: seed);
            if (!result.EventLog.Any(x => x.EventType == EventType.Miss)) continue;
            Assert.Equal(1, result.CombatStyles[0].ChargeSpent); return;
        }
        Assert.Fail("The deterministic seed set should include a missed contributor.");
    }

    [Fact]
    public void Cached_compiled_ability_sharing_does_not_share_style_state_and_compact_summaries_survive()
    {
        var channeled = Ability("channeled", Damage(200));
        var origins = new Dictionary<string, Guid> { ["channeled"] = Channeled };
        var weak = Actor([channeled], Conduit(), origins, "weak");
        var ordinary = Actor([channeled], id: "ordinary");
        var enemy = Enemy(); var result = Run(weak, enemy, [ordinary], captureLog: false);
        Assert.Equal(360, 10000 - enemy.Health);
        Assert.Equal(200, channeled.TriggersByEvent[AbilityTriggerEvent.OnAbilityUsed][0].Effects[0].BaseValue);
        result.EntityStats.Clear(); Assert.Equal(40, Assert.Single(result.CombatStyles).ChanneledOutputLost);
        Assert.Empty(result.EventLog);
    }

    [Fact]
    public void Normal_cost_reactions_are_isolated_and_emergency_threshold_is_sampled_after_costs()
    {
        var channeled = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "channeled", Kind = AbilitySpecKind.Active, CooldownTicks = 1000,
            Costs = [new() { Resource = AbilityResourceType.Health, BaseValue = 50 }], Effects = [Barrier(200)]
        });
        var (abilities, origins) = Circuit(3, channeled);
        abilities.Add(Passive("cost-reaction", AbilityTriggerEvent.OnHealthChanged, Barrier(100)));
        var actor = Actor(abilities, Conduit() with { UpgradeIds = [CombatStyleIds.EmergencyChannel] }, origins);
        actor.SetHealth(400);
        Run(actor);
        Assert.Equal(350, actor.Health);
        Assert.Equal(390, actor.Barrier); // Ordinary reaction 100; the normal Channeled alone receives 145%.
    }

    [Fact]
    public void Action_prevented_by_a_cost_reaction_keeps_charge_and_never_becomes_a_channeled_cast()
    {
        var channeled = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "channeled", Kind = AbilitySpecKind.Active, CooldownTicks = 1000,
            Costs = [new() { Resource = AbilityResourceType.Health, BaseValue = 50 }], Effects = [Damage(200)]
        });
        var (abilities, origins) = Circuit(3, channeled);
        abilities.Add(Passive("cost-silence", AbilityTriggerEvent.OnHealthChanged, new()
        {
            Id = "silence", Operation = AbilityEffectOperation.ApplyCondition, Condition = StandardConditionType.Silence,
            Target = AbilityTargetSelector.Self, BaseValue = 1, DurationTicks = 100, GuaranteedConditionApplication = true
        }));
        var summary = Assert.Single(Run(Actor(abilities, Conduit(), origins)).CombatStyles);
        Assert.Equal(3, summary.Charge); Assert.Empty(summary.ChanneledCastsByCharge);
    }

    [Fact]
    public void Legacy_counterweight_spends_once_across_targets_and_obeys_post_cost_barrier_threshold()
    {
        var areaHit = Damage(100); areaHit.Target = AbilityTargetSelector.AllEnemies;
        var actor = Actor([Ability("hit", areaHit)], Bastion(CombatStyleIds.Counterweight),
            new Dictionary<string, Guid> { ["hit"] = Channeled }); actor.GrantBarrier(actor, 200);
        var first = Enemy();
        var second = new RuntimeCombatant("second", "second", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000 }, [], canBasicAttack: false);
        var result = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new(MaxTicks: 1)).Run([actor], [first, second]);
        Assert.Equal(9800, first.Health); Assert.Equal(9900, second.Health);
        Assert.Equal(100, result.CombatStyles[0].CounterweightBarrierSpent);

        var paid = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "hit", Kind = AbilitySpecKind.Active, CooldownTicks = 1000,
            Costs = [new() { Resource = AbilityResourceType.Barrier, BaseValue = 60 }], Effects = [Damage(100)]
        });
        var constrained = Actor([paid], Bastion(CombatStyleIds.Counterweight),
            new Dictionary<string, Guid> { ["hit"] = Channeled }); constrained.GrantBarrier(constrained, 250);
        var summary = Assert.Single(Run(constrained).CombatStyles);
        Assert.Equal(0, summary.CounterweightBarrierSpent); Assert.Equal(190, constrained.Barrier);
    }

    [Fact]
    public void Converted_barrier_cannot_revive_a_dead_owner_through_lifesteal_after_retaliation()
    {
        var actor = Actor([Ability("hit", Damage(200))], Bastion()); actor.SetHealth(10);
        actor.AdjustAttribute(AttributeType.LifeSteal, 100);
        var enemy = Enemy([Passive("retaliation", AbilityTriggerEvent.OnAttacked, Damage(100))]);
        var result = Run(actor, enemy);
        Assert.False(actor.IsAlive); Assert.Equal(0, actor.Barrier);
        Assert.Equal(0, result.CombatStyles[0].HealingConverted);
    }

    [Fact]
    public void Continuous_waves_keep_charge_while_playback_tracks_reinforcements()
    {
        var actor = Actor([Contributor("contributor")], Conduit(), new Dictionary<string, Guid> { ["contributor"] = Guid.NewGuid() });
        var checkpoints = new List<CombatCheckpoint>();
        var first = Enemy([Ability("suicide", new AbilityEffectSpec
        { Id = "self", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.Self, BaseValue = 10000 })]);
        var second = Enemy(id: "nextwave");
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new(MaxTicks: 2));
        var result = engine.Run([actor], [first], checkpointObserver: checkpoints.Add, checkpointIntervalTicks: 1,
            hostileReinforcementWaves: [[second]]);
        Assert.Equal(first.Id, Assert.Single(checkpoints[0].Hostile).Id);
        Assert.Contains(checkpoints[^1].Hostile, enemy => enemy.Id == second.Id);
        Assert.True(checkpoints[^1].IsFinal);
        Assert.Equal(1, Assert.Single(result.CombatStyles).Charge);
        Assert.Equal(1, Assert.Single(result.CombatStyles).ChargeGenerated);
    }

    private static CombatStyleSnapshot Bastion(string? refinement = null) => new()
    { CombatStyleId = CombatStyleIds.Bastion, Kind = CombatStyleKind.Bastion, RefinementId = refinement, Tuning = CurrentTuning };

    private static CombatStyleSnapshot Conduit(string? refinement = null) => new()
    {
        CombatStyleId = CombatStyleIds.Conduit, Kind = CombatStyleKind.Conduit, ChanneledPlayerEssenceId = Channeled,
        RefinementId = refinement, Tuning = refinement switch
        {
            CombatStyleIds.ShortCircuit => CurrentTuning with { ChargeCap = 2, DistinctContributors = false },
            CombatStyleIds.DeepReservoir => CurrentTuning with { ChargeCap = 4, ChanneledBaseMultiplier = .6, ChanneledPerCharge = .25 },
            CombatStyleIds.Relay => CurrentTuning with { ChanneledPerCharge = .15, RelayMinimumSpent = 2, RelayChargeReturn = 1 },
            _ => CurrentTuning
        }
    };

    private static RuntimeCombatant Actor(IEnumerable<CompiledAbility> abilities, CombatStyleSnapshot? style = null,
        IReadOnlyDictionary<string, Guid>? origins = null, string id = "actor", RuntimeCombatant? owner = null) =>
        new(id, id, CombatTeam.Friendly, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000 },
            abilities, canBasicAttack: false, combatStyle: style, essenceOrigins: origins,
            isSummoned: owner is not null, summonOwner: owner);

    private static RuntimeCombatant Enemy(IEnumerable<CompiledAbility>? abilities = null, string id = "enemy") =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000 },
            abilities ?? [], canBasicAttack: false);

    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant? enemy = null,
        IReadOnlyList<RuntimeCombatant>? allies = null, int ticks = 1, int seed = 1337, bool captureLog = true) =>
        new FastCombatEngine(new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: ticks, RandomSeed: seed, CaptureEventLog: captureLog))
            .Run([actor, .. allies ?? []], [enemy ?? Enemy()]);

    private static void AddCondition(RuntimeCombatant actor, StandardConditionType condition) =>
        actor.Conditions.Add(new RuntimeCondition(condition, actor, actor, 1, 1000, 0, 0, "test"));

    private static CompiledAbility Ability(string id, params AbilityEffectSpec[] effects) =>
        AbilityCompiler.CompileAbility(new AbilitySpec
        { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 1000, Effects = [.. effects] });

    private static CompiledAbility Passive(string id, AbilityTriggerEvent trigger, AbilityEffectSpec effect) =>
        AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = id, Name = id, Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = trigger }], Effects = [effect]
        });

    private static AbilityEffectSpec Heal(int value) => new()
    { Id = "heal.effect", Operation = AbilityEffectOperation.Heal, Target = AbilityTargetSelector.Self, BaseValue = value, CritEligibility = CritEligibility.Disallowed };
    private static AbilityEffectSpec Barrier(int value) => new()
    { Id = "barrier", Operation = AbilityEffectOperation.GrantBarrier, Target = AbilityTargetSelector.Self, BaseValue = value, CritEligibility = CritEligibility.Disallowed };
    private static AbilityEffectSpec Damage(int value) => new()
    { Id = "damage", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.CurrentTarget, BaseValue = value, CritEligibility = CritEligibility.Disallowed };
    private static CompiledAbility Contributor(string id) => Ability(id, new AbilityEffectSpec
    { Id = id + ".effect", Operation = AbilityEffectOperation.ModifyThreat, Target = AbilityTargetSelector.Self, BaseValue = 1 });

    private static (List<CompiledAbility> Abilities, Dictionary<string, Guid> Origins) Circuit(int contributors, CompiledAbility channeled)
    {
        var abilities = new List<CompiledAbility>(); var origins = new Dictionary<string, Guid>();
        for (var i = 0; i < contributors; i++) { var id = "c" + i; abilities.Add(Contributor(id)); origins.Add(id, Guid.NewGuid()); }
        abilities.Add(channeled); origins.Add(channeled.Id, Channeled); return (abilities, origins);
    }
}
