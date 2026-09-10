using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatStyleReaperEngineTests
{
    [Theory]
    [InlineData(null, StandardConditionType.Bleed, 11)]
    [InlineData(null, StandardConditionType.Burn, 11)]
    [InlineData(null, StandardConditionType.Poison, 11)]
    [InlineData(CombatStyleIds.SoulSiphon, StandardConditionType.Bleed, 11)]
    [InlineData(CombatStyleIds.SoulSiphon, StandardConditionType.Burn, 11)]
    [InlineData(CombatStyleIds.SoulSiphon, StandardConditionType.Poison, 11)]
    [InlineData(CombatStyleIds.DeathSentence, StandardConditionType.Bleed, 12)]
    [InlineData(CombatStyleIds.DeathSentence, StandardConditionType.Burn, 12)]
    [InlineData(CombatStyleIds.DeathSentence, StandardConditionType.Poison, 12)]
    public void Every_one_tick_form_accepts_each_family(string? refinement, StandardConditionType family, int amount)
    {
        var actor = Actor([Ability("strike", Damage())], Style(refinement));
        actor.SetHealth(500);
        var enemy = Enemy();
        var condition = Seed(actor, enemy, family);
        var original = condition.UnpaidFutureTicks;
        RuntimeCondition? bankedDoom = null;
        var result = Engine().Run([actor], [enemy], checkpointObserver: _ =>
            bankedDoom = enemy.Conditions.SingleOrDefault(x => x.Type == StandardConditionType.Doom), checkpointIntervalTicks: 1);
        Assert.Equal(original - 1, condition.UnpaidFutureTicks);
        Assert.Equal(refinement == CombatStyleIds.SoulSiphon ? 500 + amount : 500, actor.Health);
        Assert.Equal(refinement is null ? 9999 - amount : 9999, enemy.Health);
        if (refinement == CombatStyleIds.DeathSentence)
        {
            var doom = Assert.IsType<RuntimeCondition>(bankedDoom);
            Assert.Equal(amount, doom.StoredDamage!.Value, 6);
            Assert.Equal(150, doom.RemainingDurationTicks);
            Assert.DoesNotContain(enemy.Conditions, x => x.Type == StandardConditionType.Doom);
        }
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.DamageCrit);
    }

    [Fact]
    public void Consumed_tick_is_skipped_and_later_ticks_keep_their_original_timestamps()
    {
        var actor = Actor([Ability("strike", Damage())]);
        var enemy = Enemy();
        Seed(actor, enemy, StandardConditionType.Bleed);
        var result = Run(actor, enemy, ticks: 81);
        Assert.Equal(new[] { 40, 60, 80 }, result.EventLog.Where(x => x.Source == "condition.bleed"
            && x.EventType == EventType.Damage).Select(x => x.Timestamp));
        Assert.Equal(9958, enemy.Health); // One direct hit, 11 Harvest and three ordinary ticks.
        Assert.Empty(enemy.Conditions);
    }

    [Fact]
    public void Tick_due_at_cast_timestamp_resolves_before_harvest_and_final_tick_is_not_reused()
    {
        var actor = Actor([Ability("strike", Damage())]);
        actor.Abilities[0].StartCooldown(0);
        var enemy = Enemy();
        var condition = Seed(actor, enemy, StandardConditionType.Bleed, duration: 40);
        var result = Engine(41).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 20) actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        var damage = result.EventLog.Where(x => x.EventType == EventType.Damage).ToArray();
        Assert.Equal(new[] { "condition.bleed", "strike.hit", "Harvest" }, damage.Select(x => x.Source));
        Assert.All(damage, x => Assert.Equal(20, x.Timestamp));
        Assert.Equal(0, condition.UnpaidFutureTicks);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "condition.bleed" && x.EventType == EventType.StatusEffectExpired);
    }

    [Fact]
    public void Repeated_area_hits_harvest_every_target_without_reusing_ticks()
    {
        var hit = Damage(); hit.Target = AbilityTargetSelector.AllEnemies; hit.RepeatCount = 5;
        var actor = Actor([Ability("sweep", hit)]);
        var enemies = new[] { Enemy(id: "first"), Enemy(id: "second"), Enemy(id: "third") };
        foreach (var enemy in enemies) Seed(actor, enemy, StandardConditionType.Burn);
        var result = Engine().Run([actor], enemies);
        Assert.All(enemies, enemy => { Assert.Equal(9951, enemy.Health); Assert.Empty(enemy.Conditions); });
        Assert.Equal(12, result.EventLog.Count(x => x.Source == "Harvest" && x.EventType == EventType.Damage));
    }

    [Fact]
    public void Conditions_from_same_cast_and_hit_reactions_only_become_eligible_on_a_later_cast()
    {
        var actor = Actor([Ability("first", Condition(StandardConditionType.Bleed, 1), Damage()),
            Ability("second", Damage()), Passive("onhit", AbilityTriggerEvent.OnHit, Condition(StandardConditionType.Poison, 1))]);
        var enemy = Enemy();
        var result = Run(actor, enemy);
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "Harvest" && x.EventType == EventType.Damage));
        var bleed = Assert.Single(enemy.Conditions, x => x.Type == StandardConditionType.Bleed);
        Assert.Equal(3, bleed.UnpaidFutureTicks);
        Assert.Equal(new[] { 5, 6 }, enemy.Conditions.Where(x => x.Type == StandardConditionType.Poison).Select(x => x.UnpaidFutureTicks));
    }

    [Fact]
    public void Harvest_only_spends_the_characters_own_conditions()
    {
        var actor = Actor([Ability("strike", Damage())]);
        var ally = Actor([], id: "ally");
        var summon = Actor([], id: "summon", owner: actor);
        var enemy = Enemy();
        Seed(actor, enemy, StandardConditionType.Bleed);
        var allied = Seed(ally, enemy, StandardConditionType.Burn);
        var summoned = Seed(summon, enemy, StandardConditionType.Poison);
        Engine().Run([actor, ally, summon], [enemy]);
        Assert.Equal(9988, enemy.Health);
        Assert.Equal(4, allied.UnpaidFutureTicks);
        Assert.Equal(6, summoned.UnpaidFutureTicks);
    }

    [Fact]
    public void Barrier_only_hit_qualifies_but_zero_damage_and_dead_targets_do_not()
    {
        var actor = Actor([Ability("strike", Damage())]);
        var enemy = Enemy(); enemy.GrantBarrier(enemy, 100);
        Seed(actor, enemy, StandardConditionType.Bleed);
        Run(actor, enemy);
        Assert.Equal(88, enemy.Barrier);
        Assert.Equal(10000, enemy.Health);

        var zeroActor = Actor([Ability("zero", Damage(0))]);
        var zeroEnemy = Enemy(); var untouched = Seed(zeroActor, zeroEnemy, StandardConditionType.Bleed);
        Run(zeroActor, zeroEnemy);
        Assert.Equal(4, untouched.UnpaidFutureTicks);
        var killer = Actor([Ability("kill", Damage(10000))]);
        var victim = Enemy(); Seed(killer, victim, StandardConditionType.Bleed);
        Assert.DoesNotContain(Run(killer, victim).EventLog, x => x.Source == "Harvest");
    }

    [Fact]
    public void Cleanse_or_lethal_retaliation_before_harvest_prevents_consumption()
    {
        var cleanse = new AbilityEffectSpec { Id = "cleanse", Operation = AbilityEffectOperation.Cleanse, Target = AbilityTargetSelector.Self, BaseValue = 1 };
        var actor = Actor([Ability("strike", Damage())]);
        var enemy = Enemy([Passive("cleanse", AbilityTriggerEvent.OnAttacked, cleanse)]);
        Seed(actor, enemy, StandardConditionType.Bleed);
        Assert.DoesNotContain(Run(actor, enemy).EventLog, x => x.Source == "Harvest");

        actor = Actor([Ability("strike", Damage())]); actor.SetHealth(1);
        enemy = Enemy([Passive("retaliate", AbilityTriggerEvent.OnAttacked, Damage(10000))]);
        var condition = Seed(actor, enemy, StandardConditionType.Bleed);
        Assert.DoesNotContain(Run(actor, enemy).EventLog, x => x.Source == "Harvest");
        Assert.Equal(4, condition.UnpaidFutureTicks);
        Assert.False(actor.IsAlive);
    }

    [Fact]
    public void Stored_harvest_does_not_feed_hit_lifesteal_or_reflection_chains()
    {
        var reaction = Damage(2); reaction.Tags = ["Damage.Secondary"];
        var actor = Actor([Ability("strike", Damage()), Passive("onhit", AbilityTriggerEvent.OnHit, reaction)]);
        actor.SetHealth(500); actor.AdjustAttribute(AttributeType.LifeSteal, 100);
        var enemy = Enemy(); Seed(actor, enemy, StandardConditionType.Bleed);
        enemy.Conditions.Add(new(StandardConditionType.Thorns, enemy, enemy, 100, 0, 0, 0, "Thorns"));
        var result = Run(actor, enemy);
        Assert.Single(result.EventLog, x => x.Source == "Harvest" && x.EventType == EventType.Damage);
        Assert.Single(result.EventLog, x => x.Source == "onhit.hit" && x.EventType == EventType.Damage);
        Assert.Single(result.EventLog, x => x.EventType == EventType.ReflectedDamage);
        Assert.Equal(500, actor.Health); // One reflected damage and one direct-hit Lifesteal.
    }

    [Theory]
    [InlineData(500, false, false, 566)]
    [InlineData(500, true, false, 546)]
    [InlineData(500, false, true, 586)]
    [InlineData(500, true, true, 566)]
    [InlineData(990, false, false, 1000)]
    [InlineData(1000, false, false, 1000)]
    public void Soul_siphon_uses_healing_modifiers_and_missing_health_once(int health, bool wound, bool recovery, int expected)
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.SoulSiphon));
        actor.SetHealth(health); actor.AdjustAttribute(AttributeType.HealingPowerPercent, 100);
        actor.AdjustAttribute(AttributeType.CritChance, 75);
        if (wound) actor.Conditions.Add(new(StandardConditionType.Wound, actor, actor, 1, 60, 0, 0, "Wound"));
        if (recovery) actor.Conditions.Add(new(StandardConditionType.Recovery, actor, actor, 1, 60, 0, 0, "Recovery"));
        var enemy = Enemy();
        foreach (var family in Families) Seed(actor, enemy, family);
        Run(actor, enemy);
        Assert.Equal(expected, actor.Health);
        Assert.Equal(0, actor.Barrier);
        Assert.Equal(9999, enemy.Health);
    }

    [Theory]
    [InlineData(3502, false, 0)]
    [InlineData(3501, false, 150)]
    [InlineData(3500, false, 150)]
    [InlineData(5001, true, 0)]
    [InlineData(3501, true, 150)]
    public void Last_rites_consumes_entire_budget_only_at_its_threshold_and_deep_roots_never_qualifies(int health, bool masteredClosing, int harvest)
    {
        var style = Style(CombatStyleIds.LastRites, 10) with
        {
            UpgradeIds = [CombatStyleIds.ClosingHand, CombatStyleIds.DeepRoots],
            MasteredUpgradeId = masteredClosing ? CombatStyleIds.ClosingHand : CombatStyleIds.DeepRoots
        };
        var actor = Actor([Ability("strike", Damage())], style);
        var enemy = Enemy(); enemy.SetHealth(health);
        Seed(actor, enemy, StandardConditionType.Bleed);
        Seed(actor, enemy, StandardConditionType.Burn);
        // 8 ticks of 10 each at 125% = 100, plus 4 Poison ticks = 50.
        Seed(actor, enemy, StandardConditionType.Poison, duration: 80);
        // Ignore Grave Seed in this isolated threshold test by spending its application with Ward.
        enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        Run(actor, enemy);
        Assert.Equal(health - 1 - harvest, enemy.Health);
        Assert.Equal(harvest == 0 ? 3 : 0, enemy.Conditions.Count);
    }

    [Fact]
    public void Doom_has_independent_exact_timers_and_preserves_banked_amount_after_power_changes_and_source_death()
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.DeathSentence));
        var ally = Actor([], id: "survivor");
        var enemy = Enemy(); Seed(actor, enemy, StandardConditionType.Bleed);
        var banks = new List<double>();
        var result = Engine(152).Run([actor, ally], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 1) actor.Abilities[0].ReduceCooldown(1000);
            if (checkpoint.Tick == 2)
            {
                banks.AddRange(enemy.Conditions.Where(x => x.Type == StandardConditionType.Doom).Select(x => x.StoredDamage!.Value));
                actor.AdjustAttribute(AttributeType.Power, 9000);
                actor.SetHealth(0);
            }
        }, checkpointIntervalTicks: 1);
        Assert.Equal(2, banks.Count);
        Assert.All(banks, amount => Assert.Equal(12, amount, 6));
        var doom = result.EventLog.Where(x => x.Source == "condition.doom" && x.EventType == EventType.Damage).ToArray();
        Assert.Equal(new[] { 150, 151 }, doom.Select(x => x.Timestamp));
        Assert.All(doom, x => { Assert.Equal(12, x.Magnitude); Assert.Equal(DamageType.Magical, x.DamageType); });
    }

    [Fact]
    public void Ward_blocks_doom_without_refunding_harvested_ticks()
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.DeathSentence));
        var enemy = Enemy(); var bleed = Seed(actor, enemy, StandardConditionType.Bleed);
        enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        Run(actor, enemy);
        Assert.DoesNotContain(enemy.Conditions, x => x.Type == StandardConditionType.Doom || x.Type == StandardConditionType.Ward);
        Assert.Equal(3, bleed.UnpaidFutureTicks);
        Assert.Equal(9999, enemy.Health);
    }

    [Fact]
    public void Cleanse_removes_one_earliest_doom_without_detonating_or_refunding()
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.DeathSentence));
        var cleanse = new AbilityEffectSpec { Id = "cleanse", Operation = AbilityEffectOperation.Cleanse,
            Target = AbilityTargetSelector.Self, Condition = StandardConditionType.Doom, BaseValue = 1 };
        var enemy = Enemy([Ability("cleanse", cleanse)]);
        enemy.Abilities[0].StartCooldown(0);
        var bleed = Seed(actor, enemy, StandardConditionType.Bleed);
        var result = Engine(152).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 1) actor.Abilities[0].ReduceCooldown(1000);
            if (checkpoint.Tick == 2) enemy.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        var doom = Assert.Single(result.EventLog, x => x.Source == "condition.doom" && x.EventType == EventType.Damage);
        Assert.Equal(151, doom.Timestamp);
        Assert.Equal(12, doom.Magnitude);
        Assert.Equal(0, bleed.UnpaidFutureTicks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CombatStyleIds.SoulSiphon)]
    [InlineData(CombatStyleIds.LastRites)]
    [InlineData(CombatStyleIds.DeathSentence)]
    public void Grave_seed_is_owned_uses_highest_max_health_and_runs_once_across_waves(string? refinement)
    {
        var actor = Actor([], Style(refinement, 7));
        var first = Enemy([Ability("exit", new AbilityEffectSpec { Id = "exit", Operation = AbilityEffectOperation.Damage,
            Target = AbilityTargetSelector.Self, BaseValue = 10000 })]);
        var second = Enemy(id: "second");
        var result = Engine(2).Run([actor], [first], hostileReinforcementWaves: [[second]]);
        Assert.Single(result.EventLog, x => x.StatsSource == "Combat Style: Grave Seed" && x.EventType == EventType.StatusEffect);
        Assert.Empty(second.Conditions);

        actor = Actor([], Style(refinement, 7));
        var small = new RuntimeCombatant("small", "small", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 500 }, [], canBasicAttack: false);
        var large = Enemy(id: "large");
        Engine().Run([actor], [small, large]);
        Assert.Empty(small.Conditions);
        var poison = Assert.Single(large.Conditions);
        Assert.Same(actor, poison.Source); Assert.Equal(5, poison.Value); Assert.Equal(1000, poison.PowerSnapshot);
    }

    [Fact]
    public void Reaper_catalog_snapshot_keeps_tuning_and_validates_choices_and_mastery()
    {
        var catalog = CombatStyleFoundationTests.LoadCatalog();
        var definition = catalog.Styles.Single(x => x.Id == CombatStyleIds.Reaper);
        var progress = new CharacterCombatStyle { CombatStyleId = definition.Id, Level = 10 };
        var selection = new CombatStyleSelectionRequest(definition.Id, CombatStyleIds.SoulSiphon,
            [CombatStyleIds.Crosscut, CombatStyleIds.DeepRoots], null, MasteredUpgradeId: CombatStyleIds.Crosscut);
        Assert.Null(CombatStyleRules.ValidateSelection(progress, definition, selection));
        var snapshot = CombatStyleRules.Snapshot(catalog, definition, progress, selection);
        var roundTrip = JsonSerializer.Deserialize<CombatStyleSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(1.2, roundTrip.Tuning.Reaper!.Multiplier(10), 6);
        Assert.Equal(1.3, roundTrip.Tuning.Reaper.Multiplier(10, CombatStyleIds.DeathSentence), 6);
        Assert.Equal(.10, roundTrip.Tuning.Reaper.DeathSentenceBonus);
        Assert.Equal(5, roundTrip.Tuning.Reaper.OpeningPoisonStacks);
        Assert.Null(roundTrip.ChanneledPlayerEssenceId);
        Assert.Equal(snapshot.Tuning.Reaper, roundTrip.Tuning.Reaper);
        Assert.NotNull(CombatStyleRules.ValidateSelection(progress, definition, selection with { RefinementId = "bloodletting" }));
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
        {
            Styles = catalog.Styles.Select(x => x.Id == definition.Id
                ? x with { Tuning = x.Tuning with { Reaper = new() { PerMasteryLevel = double.NaN } } } : x).ToArray()
        }));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 12.1)]
    [InlineData(3, 12.3)]
    [InlineData(9, 12.9)]
    [InlineData(10, 13)]
    public void Every_mastery_level_is_captured_once_in_doom(int level, double expected)
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.DeathSentence, level));
        var enemy = Enemy(); Seed(actor, enemy, StandardConditionType.Bleed);
        if (level >= 7) enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        double? stored = null;
        Engine().Run([actor], [enemy], checkpointObserver: _ =>
            stored = enemy.Conditions.SingleOrDefault(x => x.Type == StandardConditionType.Doom)?.StoredDamage,
            checkpointIntervalTicks: 1);
        Assert.Equal(expected, stored!.Value, 6);
    }

    [Theory]
    [InlineData(CombatStyleIds.Crosscut, false, false, false, 39)]
    [InlineData(CombatStyleIds.Crosscut, true, false, false, 40.5)]
    [InlineData(CombatStyleIds.Crosscut, false, true, false, 40.5)]
    [InlineData(CombatStyleIds.DeepRoots, false, true, false, 40.5)]
    [InlineData(CombatStyleIds.DeepRoots, false, true, true, 39)]
    [InlineData(CombatStyleIds.DeepRoots, true, true, true, 40.5)]
    public void Upgrade_qualification_counts_source_stacks_and_remaining_budgets(string upgrade, bool mastered,
        bool mixedFamilies, bool exhaustOne, double expected)
    {
        var style = Style(CombatStyleIds.DeathSentence, 10) with
        { UpgradeIds = [upgrade], MasteredUpgradeId = mastered ? upgrade : null };
        var actor = Actor([Ability("strike", Damage())], style);
        var enemy = Enemy();
        // Ward absorbs Grave Seed, leaving precisely the authored source budgets below.
        enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        Seed(actor, enemy, StandardConditionType.Bleed, stacks: mixedFamilies ? 2 : 3);
        if (mixedFamilies) Seed(actor, enemy, StandardConditionType.Poison, duration: exhaustOne ? 20 : 120);
        double? stored = null;
        Engine().Run([actor], [enemy], checkpointObserver: _ =>
            stored = enemy.Conditions.SingleOrDefault(x => x.Type == StandardConditionType.Doom)?.StoredDamage,
            checkpointIntervalTicks: 1);
        Assert.Equal(expected, stored!.Value, 6);
    }

    [Fact]
    public void Two_upgrades_lock_their_bonus_at_banking_and_doom_uses_magical_defenses_and_barrier()
    {
        var style = Style(CombatStyleIds.DeathSentence, 10) with
        { UpgradeIds = [CombatStyleIds.ClosingHand, CombatStyleIds.Crosscut], MasteredUpgradeId = CombatStyleIds.ClosingHand };
        var actor = Actor([Ability("strike", Damage())], style);
        var enemy = Enemy(); enemy.SetHealth(4501);
        enemy.AdjustAttribute(AttributeType.Armor, 100000);
        enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        Seed(actor, enemy, StandardConditionType.Bleed);
        Seed(actor, enemy, StandardConditionType.Poison);
        double? stored = null;
        var result = Engine(151).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick != 1) return;
            stored = enemy.Conditions.Single(x => x.Type == StandardConditionType.Doom).StoredDamage;
            enemy.SetHealth(10000); enemy.GrantBarrier(enemy, 100);
            actor.AdjustAttribute(AttributeType.Power, 10000);
        }, checkpointIntervalTicks: 1);
        Assert.Equal(28, stored!.Value, 6);
        var doom = Assert.Single(result.EventLog, x => x.Source == "condition.doom" && x.EventType == EventType.Damage);
        Assert.Equal(28, doom.IncomingRawDamage);
        Assert.Equal(28, doom.BarrierAbsorbed);
        Assert.Equal(0, doom.PhysicalMitigationPrevented);
        Assert.Equal(DamageType.Magical, doom.DamageType);
    }

    [Fact]
    public void Dead_targets_lose_pending_doom_even_if_they_return_later()
    {
        var actor = Actor([Ability("strike", Damage())], Style(CombatStyleIds.DeathSentence));
        var suicide = Damage(10000); suicide.Target = AbilityTargetSelector.Self;
        var enemy = Enemy([Ability("exit", suicide)]);
        var other = Enemy(id: "other"); Seed(actor, enemy, StandardConditionType.Bleed);
        var result = Engine(151).Run([actor], [enemy, other], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 1)
            {
                Assert.DoesNotContain(enemy.Conditions, x => x.Type == StandardConditionType.Doom);
                enemy.SetHealth(10000);
            }
        }, checkpointIntervalTicks: 1);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "condition.doom" && x.EventType == EventType.Damage);
    }

    [Fact]
    public void Logless_and_logged_encounters_have_identical_outcomes()
    {
        CombatResult Resolve(bool logging)
        {
            var hit = Damage(); hit.RepeatCount = 2;
            var actor = Actor([Ability("strike", hit)], Style(CombatStyleIds.DeathSentence));
            var enemy = Enemy(); Seed(actor, enemy, StandardConditionType.Bleed);
            return new FastCombatEngine(new Dictionary<string, CompiledStatus>(),
                new(MaxTicks: 151, CaptureEventLog: logging)).Run([actor], [enemy]);
        }
        var logged = Resolve(true); var logless = Resolve(false);
        Assert.Equal(logged.Outcome, logless.Outcome);
        Assert.Equal(logged.Duration, logless.Duration);
        Assert.Equal(logged.EntityStats.Sum(x => x.DamageDone), logless.EntityStats.Sum(x => x.DamageDone));
        Assert.Empty(logless.EventLog);
    }

    [Fact]
    public void Soul_siphon_publishes_ordinary_healing_reactions_without_harvesting_recursively()
    {
        var actor = Actor([Ability("strike", Damage()), Passive("healed-hit", AbilityTriggerEvent.OnHeal, Damage(2))],
            Style(CombatStyleIds.SoulSiphon));
        actor.SetHealth(500);
        var enemy = Enemy(); var bleed = Seed(actor, enemy, StandardConditionType.Bleed);
        var result = Run(actor, enemy);
        Assert.Single(result.EventLog, x => x.Source == "Soul Siphon" && x.EventType == EventType.Heal);
        Assert.Single(result.EventLog, x => x.Source == "healed-hit.hit" && x.EventType == EventType.Damage);
        Assert.Equal(3, bleed.UnpaidFutureTicks);
        Assert.Equal(511, actor.Health);
    }

    [Fact]
    public void Direct_damage_from_condition_consumption_can_harvest_preexisting_dots()
    {
        var consume = new AbilityEffectSpec { Id = "consume", Operation = AbilityEffectOperation.ConsumeConditionStacks,
            Target = AbilityTargetSelector.CurrentTarget, Condition = StandardConditionType.Soaked, BaseValue = 1,
            ScalingAttribute = AttributeType.Power, ScalingCoefficient = .01f, CritEligibility = CritEligibility.Disallowed };
        var actor = Actor([Ability("consume", consume)]);
        var enemy = Enemy(); var bleed = Seed(actor, enemy, StandardConditionType.Bleed);
        enemy.Conditions.Add(new(StandardConditionType.Soaked, actor, enemy, 1, 0, 0, 0, "Soaked"));
        var result = Run(actor, enemy);
        Assert.Single(result.EventLog, x => x.Source == "Harvest" && x.EventType == EventType.Damage);
        Assert.Equal(3, bleed.UnpaidFutureTicks);
    }

    [Theory]
    [InlineData(3502, false, false, 620)]
    [InlineData(3501, false, false, 625)]
    [InlineData(3000, false, false, 625)]
    [InlineData(3500, true, false, 625)]
    [InlineData(3501, true, false, 620)]
    [InlineData(5000, true, true, 625)]
    [InlineData(5001, true, true, 620)]
    public void Closing_hand_checks_current_opponent_health_including_already_low_and_barrier_only_hits(
        int enemyHealth, bool barrier, bool mastered, int expectedHealth)
    {
        var style = Style(CombatStyleIds.SoulSiphon, 10) with
        { UpgradeIds = [CombatStyleIds.ClosingHand], MasteredUpgradeId = mastered ? CombatStyleIds.ClosingHand : null };
        var actor = Actor([Ability("strike", Damage())], style);
        actor.SetHealth(500);
        var enemy = Enemy(); enemy.SetHealth(enemyHealth);
        if (barrier) enemy.GrantBarrier(enemy, 100);
        enemy.Conditions.Add(new(StandardConditionType.Ward, enemy, enemy, 1, 0, 0, 0, "Ward"));
        Seed(actor, enemy, StandardConditionType.Bleed, stacks: 10);
        Run(actor, enemy);
        Assert.Equal(expectedHealth, actor.Health);
        Assert.Equal(barrier ? enemyHealth : enemyHealth - 1, enemy.Health);
    }

    [Fact]
    public void Old_committed_reaper_snapshot_keeps_its_doom_amount_opening_and_serialized_identity()
    {
        var historical = Style(CombatStyleIds.DeathSentence, 10) with
        { ContentVersion = "combat-styles.v7", Tuning = new() { Reaper = new() } };
        var json = JsonSerializer.Serialize(historical);
        Assert.DoesNotContain("DeathSentenceBonus", json);
        var snapshot = JsonSerializer.Deserialize<CombatStyleSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(snapshot));
        Assert.Equal(2, snapshot.Tuning.Reaper!.OpeningPoisonStacks);
        var actor = Actor([Ability("strike", Damage())], snapshot);
        var enemy = Enemy();
        double? banked = null;
        Engine().Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 1)
                banked = enemy.Conditions.Single(x => x.Type == StandardConditionType.Doom).StoredDamage;
        },
            checkpointIntervalTicks: 1);
        Assert.Equal(24, banked!.Value, 6); // Two old Grave Seed stacks, with no additional form bonus.
    }

    [Theory]
    [InlineData(-.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Catalog_rejects_invalid_death_sentence_bonus(double bonus)
    {
        var catalog = CombatStyleFoundationTests.LoadCatalog();
        Assert.Throws<InvalidOperationException>(() => CombatStyleRules.ValidateCatalog(catalog with
        {
            Styles = catalog.Styles.Select(x => x.Id == CombatStyleIds.Reaper
                ? x with { Tuning = x.Tuning with { Reaper = x.Tuning.Reaper! with { DeathSentenceBonus = bonus } } }
                : x).ToArray()
        }));
    }

    private static readonly ReaperTuning CurrentTuning = CombatStyleFoundationTests.LoadCatalog()
        .Styles.Single(x => x.Id == CombatStyleIds.Reaper).Tuning.Reaper!;
    private static readonly StandardConditionType[] Families = [StandardConditionType.Bleed, StandardConditionType.Burn, StandardConditionType.Poison];
    private static CombatStyleSnapshot Style(string? refinement = null, int level = 0) => new()
    {
        CombatStyleId = CombatStyleIds.Reaper, Kind = CombatStyleKind.Reaper, RefinementId = refinement,
        Level = level, Tuning = new() { Reaper = CurrentTuning }
    };
    private static RuntimeCombatant Actor(IEnumerable<CompiledAbility> abilities, CombatStyleSnapshot? style = null,
        string id = "actor", RuntimeCombatant? owner = null)
    {
        var list = abilities.ToArray();
        return new(id, id, CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000, [AttributeType.Power] = 1000 },
            list, canBasicAttack: false, combatStyle: style ?? Style(),
            essenceOrigins: list.Where(x => x.Kind == AbilitySpecKind.Active).ToDictionary(x => x.Id, _ => Guid.NewGuid()),
            isSummoned: owner is not null, summonOwner: owner);
    }
    private static RuntimeCombatant Enemy(IEnumerable<CompiledAbility>? abilities = null, string id = "enemy") =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000 }, abilities ?? [], canBasicAttack: false);
    private static RuntimeCondition Seed(RuntimeCombatant source, RuntimeCombatant target, StandardConditionType type,
        int stacks = 1, int? duration = null)
    {
        var condition = new RuntimeCondition(type, source, target, stacks,
            duration ?? (type == StandardConditionType.Bleed ? 80 : type == StandardConditionType.Burn ? 40 : 120),
            1000, 0, type.ToString(), type == StandardConditionType.Burn ? 10 : 20);
        target.Conditions.Add(condition);
        return condition;
    }
    private static FastCombatEngine Engine(int ticks = 1) => new(new Dictionary<string, CompiledStatus>(), new(MaxTicks: ticks));
    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant enemy, int ticks = 1) => Engine(ticks).Run([actor], [enemy]);
    private static CompiledAbility Ability(string id, params AbilityEffectSpec[] effects)
    {
        foreach (var effect in effects) effect.Id = $"{id}.{effect.Id}";
        return AbilityCompiler.CompileAbility(new AbilitySpec { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 1000, Effects = [.. effects] });
    }
    private static CompiledAbility Passive(string id, AbilityTriggerEvent trigger, params AbilityEffectSpec[] effects)
    {
        foreach (var effect in effects) effect.Id = $"{id}.{effect.Id}";
        return AbilityCompiler.CompileAbility(new AbilitySpec { Id = id, Name = id, Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = trigger }], Effects = [.. effects] });
    }
    private static AbilityEffectSpec Damage(int amount = 1) => new()
    { Id = "hit", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.CurrentTarget,
        BaseValue = amount, CritEligibility = CritEligibility.Disallowed };
    private static AbilityEffectSpec Condition(StandardConditionType type, int stacks) => new()
    { Id = type.ToString(), Operation = AbilityEffectOperation.ApplyCondition, Target = AbilityTargetSelector.CurrentTarget,
        Condition = type, BaseValue = stacks };
}
