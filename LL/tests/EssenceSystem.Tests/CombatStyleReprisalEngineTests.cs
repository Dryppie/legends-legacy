using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatStyleReprisalEngineTests
{
    private static readonly Guid Essence = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Any_source_barrier_on_owner_banks_actual_absorption_and_releases_without_spending_barrier(bool externalBarrier)
    {
        var actor = Actor([Ability("strike", Damage(100))]);
        var provider = Actor([], id: "provider", style: new() { Kind = CombatStyleKind.Conduit });
        actor.GrantBarrier(externalBarrier ? provider : actor, 500);
        var enemy = PrimingEnemy(Damage(200));

        var result = Run(actor, enemy);

        Assert.Equal(1000, actor.Health);
        Assert.Equal(300, actor.Barrier);
        Assert.Equal(9850, enemy.Health);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(50, summary.ReprisalDamage);
        Assert.Equal(0, summary.ReprisalStoredDamage);
        Assert.Equal(0, summary.CounterweightBarrierSpent);
    }

    [Fact]
    public void Charge_counts_only_absorbed_damage_and_accumulates_to_current_max_health_cap()
    {
        var partial = Actor([]); partial.GrantBarrier(partial, 60);
        var partialResult = Run(partial, PrimingEnemy(Damage(200)));
        Assert.Equal(860, partial.Health);
        Assert.Equal(0, partial.Barrier);
        Assert.Equal(15, Assert.Single(partialResult.CombatStyles).ReprisalStoredDamage);

        var capped = Actor([]); capped.GrantBarrier(capped, 600);
        var cappedResult = Run(capped, PrimingEnemy(Damage(100), Damage(150), Damage(250)));
        Assert.Equal(1000, capped.Health);
        Assert.Equal(100, capped.Barrier);
        Assert.Equal(100, Assert.Single(cappedResult.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Ordinary_barrier_grants_and_health_damage_do_not_prepare_reprisal()
    {
        var untouched = Actor([Ability("strike", Damage(100))]); untouched.GrantBarrier(untouched, 500);
        var enemy = Enemy();
        var untouchedResult = Run(untouched, enemy);
        Assert.Equal(9900, enemy.Health);
        Assert.Equal(500, untouched.Barrier);
        Assert.Equal(0, Assert.Single(untouchedResult.CombatStyles).ReprisalDamage);

        var exposed = Actor([]);
        var exposedResult = Run(exposed, PrimingEnemy(Damage(200)));
        Assert.Equal(800, exposed.Health);
        Assert.Equal(0, Assert.Single(exposedResult.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Fractional_absorption_gains_accumulate_without_rounding_each_hit()
    {
        var actor = Actor([]); actor.GrantBarrier(actor, 10);
        var result = Run(actor, PrimingEnemy(Damage(1), Damage(1), Damage(1)));
        Assert.Equal(7, actor.Barrier);
        Assert.Equal(.75, Assert.Single(result.CombatStyles).ReprisalStoredDamage, 6);
    }

    [Fact]
    public void Whole_damage_is_released_while_the_fractional_charge_is_retained()
    {
        var actor = Actor([Ability("strike", Damage(100))]); actor.GrantBarrier(actor, 10);
        var enemy = PrimingEnemy(Damage(5));

        var result = Run(actor, enemy);

        Assert.Equal(9899, enemy.Health);
        Assert.Equal(1, Assert.Single(result.CombatStyles).ReprisalDamage);
        Assert.Equal(.25, Assert.Single(result.CombatStyles).ReprisalStoredDamage, 6);
    }

    [Theory]
    [InlineData(200, 50, 25)]
    [InlineData(400, 100, 0)]
    public void Gains_during_a_cast_wait_for_the_next_cast_and_share_the_cap_with_reserved_charge(
        int initialAbsorption, int releasedDamage, int remainingCharge)
    {
        var preparationHit = Damage(1); preparationHit.Id = "pre-hit-ping";
        var strike = Damage(100); strike.Id = "strike-hit";
        // Listeners resolve in registration order, so the passive reacts before the active's own effects.
        var actor = Actor([Passive("pre-hit", AbilityTriggerEvent.OnAbilityUsed, preparationHit), Ability("strike", strike)]);
        actor.GrantBarrier(actor, 800);
        var retaliationHit = Damage(100); retaliationHit.Id = "retaliation-hit";
        var reaction = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "retaliation", Name = "retaliation", Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = AbilityTriggerEvent.OnDamaged, InternalCooldownTicks = 1000 }],
            Effects = [retaliationHit]
        });
        var enemy = Enemy([Passive("prime", AbilityTriggerEvent.OnCombatStart, Damage(initialAbsorption)), reaction]);

        var result = Run(actor, enemy);

        Assert.Equal(new[] { "pre-hit-ping", "retaliation-hit", "strike-hit" }, result.EventLog
            .Where(x => x.EventType == EventType.Damage && x.Source is "pre-hit-ping" or "retaliation-hit" or "strike-hit")
            .Select(x => x.Source));
        Assert.Equal(10000 - 101 - releasedDamage, enemy.Health);
        Assert.Equal(releasedDamage, Assert.Single(result.CombatStyles).ReprisalDamage);
        Assert.Equal(remainingCharge, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Owners_barrier_on_someone_else_and_inherited_summon_style_cannot_prepare_owner_reprisal()
    {
        var actor = Actor([]);
        var summon = Actor([], id: "summon", owner: actor);
        summon.GrantBarrier(actor, 400);
        var areaHit = Damage(200); areaHit.Target = AbilityTargetSelector.AllEnemies;

        var result = Engine().Run([actor, summon], [PrimingEnemy(areaHit)]);

        Assert.Null(summon.CombatStyle);
        Assert.Equal(800, actor.Health);
        Assert.Equal(1000, summon.Health);
        Assert.Equal(200, summon.Barrier);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Friendly_or_self_damage_absorption_does_not_prepare_reprisal()
    {
        var selfHit = Damage(100); selfHit.Target = AbilityTargetSelector.Self;
        var friendlyHit = Damage(100); friendlyHit.Target = AbilityTargetSelector.AllAllies;
        var actor = Actor([Ability("self-hit", selfHit)]); actor.GrantBarrier(actor, 400);
        var ally = Actor([Ability("friendly-hit", friendlyHit)], id: "ally", style: new() { Kind = CombatStyleKind.Conduit });

        var result = Engine().Run([actor, ally], [Enemy()]);

        Assert.Equal(200, actor.Barrier);
        Assert.Equal(0, result.CombatStyles.Single(x => x.EntityId == actor.Id).ReprisalStoredDamage);
    }

    [Fact]
    public void Reprisal_is_added_once_without_critical_lifesteal_or_on_hit_multiplication()
    {
        var strike = Damage(100); strike.CritEligibility = CritEligibility.Allowed; strike.RepeatCount = 2;
        var onHit = Passive("on-hit", AbilityTriggerEvent.OnHit, new AbilityEffectSpec
        {
            Id = "on-hit-barrier", Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self, EventMagnitudeCoefficient = 1
        });
        var actor = Actor([Ability("strike", strike), onHit]); actor.SetHealth(500); actor.GrantBarrier(actor, 500);
        actor.AdjustAttribute(AttributeType.CritChance, 100);
        actor.AdjustAttribute(AttributeType.CritDamage, 100);
        actor.AdjustAttribute(AttributeType.LifeSteal, 100);
        var enemy = PrimingEnemy(Damage(400));

        var result = Run(actor, enemy);

        Assert.Equal(9500, enemy.Health);
        Assert.Equal(600, actor.Health);
        Assert.Equal(800, actor.Barrier);
        var summary = Assert.Single(result.CombatStyles);
        Assert.Equal(100, summary.ReprisalDamage);
        Assert.Equal(0, summary.ReprisalStoredDamage);
        Assert.Equal(400, summary.HealingConverted);
        Assert.Equal(100, summary.HealthRestored);
    }

    [Fact]
    public void Area_attack_spends_once_on_its_first_enemy_attempt()
    {
        var area = Damage(100); area.Target = AbilityTargetSelector.AllEnemies;
        var actor = Actor([Ability("strike", area)]); actor.GrantBarrier(actor, 500);
        var first = PrimingEnemy(Damage(400));
        var second = Enemy(id: "second");

        var result = Engine().Run([actor], [first, second]);

        Assert.Equal(9800, first.Health);
        Assert.Equal(9900, second.Health);
        Assert.Equal(100, actor.Barrier);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalDamage);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Missed_first_attack_consumes_charge_without_giving_it_to_second_component()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var first = Damage(100); first.AttackType = AttackType.Melee;
            var second = Damage(100); second.Id = "second-hit";
            var actor = Actor([Ability("strike", first, second)]); actor.GrantBarrier(actor, 500);
            var enemy = PrimingEnemy(Damage(400)); enemy.AdjustAttribute(AttributeType.DodgeChance, 100);
            var result = Engine(seed: seed).Run([actor], [enemy]);
            if (!result.EventLog.Any(x => x.EventType == EventType.Miss && x.Source == first.Id)) continue;

            Assert.Equal(9900, enemy.Health);
            Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
            Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalDamage);
            return;
        }
        Assert.Fail("The deterministic seed set should include a missed Reprisal attack.");
    }

    [Theory]
    [InlineData(StandardConditionType.Silence)]
    [InlineData(StandardConditionType.Stun)]
    public void Blocked_cast_keeps_banked_charge(StandardConditionType blocked)
    {
        var actor = Actor([Ability("strike", Damage(100))]); actor.GrantBarrier(actor, 500);
        actor.Conditions.Add(new(blocked, actor, actor, 1, 1000, 0, 0, "blocked"));
        var enemy = PrimingEnemy(Damage(400));

        var result = Run(actor, enemy);

        Assert.Equal(10000, enemy.Health);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Cast_without_an_eligible_enemy_attack_keeps_its_charge()
    {
        var conditionalHit = Damage(100);
        conditionalHit.Conditions = [new()
        {
            Type = AbilityConditionType.HealthBelowPercent, Subject = AbilityConditionSubject.Source, Value = 1
        }];
        var barrier = new AbilityEffectSpec { Id = "barrier", Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self, BaseValue = 10 };
        var actor = Actor([Ability("conditional", conditionalHit, barrier)]); actor.GrantBarrier(actor, 500);
        var enemy = PrimingEnemy(Damage(400));

        var result = Run(actor, enemy);

        Assert.Equal(10000, enemy.Health);
        Assert.Equal(110, actor.Barrier);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Native_active_basic_attack_and_periodic_effect_do_not_release_charge()
    {
        var periodic = Damage(50); periodic.AttackType = AttackType.DamageOverTime;
        var actor = Actor([Ability("native", Damage(100)), Ability("periodic", periodic)],
            origins: new Dictionary<string, Guid> { ["periodic"] = Essence }, basic: true);
        actor.AdjustAttribute(AttributeType.Power, 100);
        actor.GrantBarrier(actor, 500);
        var enemy = PrimingEnemy(Damage(400));

        var result = Run(actor, enemy);

        Assert.True(enemy.Health < 10000);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalDamage);
    }

    [Fact]
    public void Passive_damage_triggered_by_a_non_damaging_essence_does_not_release_charge()
    {
        var preparation = new AbilityEffectSpec
        { Id = "preparation", Operation = AbilityEffectOperation.ModifyThreat, Target = AbilityTargetSelector.Self, BaseValue = 1 };
        var actor = Actor([Ability("prepare", preparation), Passive("reaction", AbilityTriggerEvent.OnAbilityUsed, Damage(50))]);
        actor.GrantBarrier(actor, 500);
        var enemy = PrimingEnemy(Damage(400));

        var result = Run(actor, enemy);

        Assert.Equal(9950, enemy.Health);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalDamage);
    }

    [Fact]
    public void Absorption_reaction_healing_creates_barrier_without_recharging_the_same_damage()
    {
        var heal = new AbilityEffectSpec { Id = "absorb-heal", Operation = AbilityEffectOperation.Heal,
            Target = AbilityTargetSelector.Self, BaseValue = 200, CritEligibility = CritEligibility.Disallowed };
        var actor = Actor([Passive("absorb", AbilityTriggerEvent.OnBarrierAbsorbed, heal)]);
        actor.SetHealth(500); actor.GrantBarrier(actor, 400);

        var result = Run(actor, PrimingEnemy(Damage(100)));

        Assert.Equal(550, actor.Health);
        Assert.Equal(450, actor.Barrier);
        Assert.Equal(25, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
        Assert.Equal(200, Assert.Single(result.CombatStyles).HealingConverted);
    }

    [Fact]
    public void Falling_max_health_caps_existing_charge_before_the_attack()
    {
        var actor = Actor([Ability("strike", Damage(100))]); actor.GrantBarrier(actor, 500);
        var enemy = PrimingEnemy(Damage(400));
        var changed = false;
        var result = Engine().Run([actor], [enemy], checkpointObserver: _ =>
        {
            if (changed) return;
            changed = true;
            actor.AdjustAttribute(AttributeType.MaxHealth, -500);
        }, checkpointIntervalTicks: 1);

        Assert.Equal(9850, enemy.Health);
        Assert.Equal(50, Assert.Single(result.CombatStyles).ReprisalDamage);
        Assert.Equal(0, Assert.Single(result.CombatStyles).ReprisalStoredDamage);
    }

    [Fact]
    public void Continuous_wave_keeps_charge_but_a_new_battle_starts_empty()
    {
        var actor = Actor([Ability("strike", Damage(100))]); actor.GrantBarrier(actor, 500);
        actor.Abilities[0].StartCooldown(0);
        var suicide = Damage(10000); suicide.Target = AbilityTargetSelector.Self;
        var first = Enemy([Passive("prime", AbilityTriggerEvent.OnCombatStart, Damage(400)), Ability("exit", suicide)]);
        var second = Enemy(id: "next-wave");
        var released = false;
        var result = Engine(ticks: 2).Run([actor], [first], checkpointObserver: checkpoint =>
        {
            if (released || !checkpoint.Hostile.Any(x => x.Id == second.Id)) return;
            actor.Abilities[0].ReduceCooldown(1000);
            released = true;
        }, checkpointIntervalTicks: 1, hostileReinforcementWaves: [[second]]);

        Assert.True(released);
        Assert.Equal(9800, second.Health);
        Assert.Equal(100, Assert.Single(result.CombatStyles).ReprisalDamage);

        var charged = Actor([]); charged.GrantBarrier(charged, 500);
        Assert.Equal(100, Assert.Single(Run(charged, PrimingEnemy(Damage(400))).CombatStyles).ReprisalStoredDamage);
        Assert.Equal(0, Assert.Single(Run(charged, Enemy()).CombatStyles).ReprisalStoredDamage);
    }

    private static CombatStyleSnapshot Reprisal() => new()
    {
        CombatStyleId = CombatStyleIds.Bastion, Kind = CombatStyleKind.Bastion,
        RefinementId = CombatStyleIds.Reprisal, ContentVersion = "combat-styles.v4",
        Tuning = new() { BarrierPerMasteryLevel = .01, ReprisalAbsorbedDamageFraction = .25, ReprisalMaxHealthCapFraction = .10 }
    };

    private static RuntimeCombatant Actor(IEnumerable<CompiledAbility> abilities, string id = "actor",
        CombatStyleSnapshot? style = null, RuntimeCombatant? owner = null,
        IReadOnlyDictionary<string, Guid>? origins = null, bool basic = false)
    {
        var values = abilities.ToArray();
        origins ??= values.Where(x => x.Kind == AbilitySpecKind.Active).ToDictionary(x => x.Id, _ => Essence);
        return new(id, id, CombatTeam.Friendly, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000 },
            values, canBasicAttack: basic, combatStyle: style ?? Reprisal(), essenceOrigins: origins,
            isSummoned: owner is not null, summonOwner: owner);
    }

    private static RuntimeCombatant Enemy(IEnumerable<CompiledAbility>? abilities = null, string id = "enemy") =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000 },
            abilities ?? [], canBasicAttack: false);
    private static RuntimeCombatant PrimingEnemy(params AbilityEffectSpec[] hits)
    {
        for (var index = 0; index < hits.Length; index++) hits[index].Id = $"prime-hit-{index}";
        return Enemy([Passive("prime", AbilityTriggerEvent.OnCombatStart, hits)]);
    }
    private static FastCombatEngine Engine(int ticks = 1, int seed = 1337) =>
        new(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: ticks, RandomSeed: seed));
    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant enemy) => Engine().Run([actor], [enemy]);
    private static CompiledAbility Ability(string id, params AbilityEffectSpec[] effects) => AbilityCompiler.CompileAbility(new AbilitySpec
    { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 1000, Effects = [.. effects] });
    private static CompiledAbility Passive(string id, AbilityTriggerEvent trigger, params AbilityEffectSpec[] effects) =>
        AbilityCompiler.CompileAbility(new AbilitySpec
        { Id = id, Name = id, Kind = AbilitySpecKind.Passive, Triggers = [new() { Event = trigger }], Effects = [.. effects] });
    private static AbilityEffectSpec Damage(int amount) => new()
    { Id = "hit", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.CurrentTarget,
        BaseValue = amount, CritEligibility = CritEligibility.Disallowed };
}
