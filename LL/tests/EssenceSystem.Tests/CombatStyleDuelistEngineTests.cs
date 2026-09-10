using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatStyleDuelistEngineTests
{
    [Theory]
    [InlineData(null, 0, 3, 145)]
    [InlineData(null, 3, 3, 148)]
    [InlineData(CombatStyleIds.Flurry, 3, 3, 133)]
    [InlineData(CombatStyleIds.PatientBlade, 3, 5, 183)]
    [InlineData(CombatStyleIds.GuardedThrust, 3, 3, 128)]
    public void Separate_successful_actions_prepare_then_spend_an_opening(string? form, int level, int read, int damage)
    {
        var actor = Actor(Enumerable.Range(0, read + 1).Select(i => Ability($"hit{i}", Hit())), Style(form, level));
        var enemy = Enemy();
        var result = Run(actor, enemy);
        var hits = result.EventLog.Where(x => x.EventType == EventType.Damage).ToArray();
        Assert.Equal(read + 1, hits.Length);
        Assert.All(hits.Take(read), hit => Assert.Equal(100, hit.Magnitude));
        Assert.Equal(damage, hits[^1].Magnitude);
        Assert.Single(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(form == CombatStyleIds.Flurry ? 1 : 0, Read(result));
        Assert.Equal(form == CombatStyleIds.GuardedThrust ? 1 : 0, actor.GetConditionStacks(StandardConditionType.Guard));
    }

    [Fact]
    public void Multihit_cast_builds_once_and_spending_cast_strengthens_all_its_own_hits_only()
    {
        var repeated = Hit(); repeated.RepeatCount = 4;
        var secondary = Hit(7); secondary.Id = "secondary"; secondary.Tags = ["Damage.Secondary"];
        var actor = Actor([Ability("first", repeated), Ability("second", Hit()), Ability("third", Hit()),
            Ability("opening", repeated, secondary)]);
        var enemy = Enemy();
        var result = Run(actor, enemy);
        Assert.All(result.EventLog.Where(x => x.Source == "first.hit" && x.EventType == EventType.Damage),
            hit => Assert.Equal(100, hit.Magnitude));
        var spending = result.EventLog.Where(x => x.Source == "opening.hit" && x.EventType == EventType.Damage).ToArray();
        Assert.Equal(new[] { 145, 145, 145, 145 }, spending.Select(x => x.Magnitude));
        Assert.Equal(7, result.EventLog.Single(x => x.Source == "opening.secondary" && x.EventType == EventType.Damage).Magnitude);
        Assert.Single(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(0, Read(result));
    }

    [Fact]
    public void Area_cast_keeps_existing_opponent_even_when_they_are_not_first_and_only_boosts_them()
    {
        var actor = Actor([Ability("a", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("b", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("c", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("sweep", Hit(target: AbilityTargetSelector.AllEnemies))]);
        var small = Enemy("small", 10000); var large = Enemy("large", 20000);
        var result = Engine().Run([actor], [small, large]);
        Assert.Equal(9900, small.Health);
        Assert.Equal(19555, large.Health);
        Assert.Single(result.EventLog, x => x.Source == "Duelist Opening");
    }

    [Fact]
    public void Switching_targets_discards_ready_opening_and_returning_does_not_restore_it()
    {
        var actor = Actor([Ability("a", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("b", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("c", Hit(target: AbilityTargetSelector.HighestHealthEnemy)),
            Ability("switch", Hit(target: AbilityTargetSelector.LowestHealthEnemy)),
            Ability("return", Hit(target: AbilityTargetSelector.HighestHealthEnemy))]);
        var small = Enemy("small", 10000); small.SetHealth(5000);
        var result = Engine().Run([actor], [small, Enemy("large", 20000)]);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(1, Read(result));
    }

    [Fact]
    public void First_impression_waits_for_a_surviving_target_and_does_not_boost_the_preparing_cast()
    {
        var actor = Actor([Ability("kill", Hit(100, AbilityTargetSelector.LowestHealthEnemy)),
            Ability("prepare", Hit()), Ability("spend", Hit())], Style(level: 7));
        var small = Enemy("small", 50); var large = Enemy("large", 10000);
        var result = Engine().Run([actor], [small, large]);
        Assert.Single(result.EventLog, x => x.Source == "First Impression");
        Assert.Equal(100, result.EventLog.Single(x => x.Source == "prepare.hit" && x.EventType == EventType.Damage).Magnitude);
        Assert.Equal(152, result.EventLog.Single(x => x.Source == "spend.hit" && x.EventType == EventType.Damage).Magnitude);
        Assert.Equal(9748, large.Health);
    }

    [Fact]
    public void Basic_attacks_build_read_without_spending_it_and_qualify_measured_strikes()
    {
        var style = Style(level: 10) with { UpgradeIds = [CombatStyleIds.MeasuredStrikes], MasteredUpgradeId = CombatStyleIds.MeasuredStrikes };
        var actor = Actor([Ability("opening", Hit())], style, basic: true);
        actor.Abilities[0].StartCooldown(0);
        var enemy = Enemy();
        var result = Engine(4, basicInterval: 1).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick == 3) actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        Assert.Equal(3, result.EventLog.Count(x => x.Source == "Basic Attack" && x.EventType == EventType.Damage && x.Timestamp < 3));
        Assert.Equal(175, result.EventLog.Single(x => x.Source == "opening.hit" && x.EventType == EventType.Damage).Magnitude);
        Assert.Single(result.EventLog, x => x.Source == "Duelist Opening");
    }

    [Theory]
    [InlineData(CombatStyleIds.Flurry, 1, 0)]
    [InlineData(CombatStyleIds.GuardedThrust, 0, 1)]
    public void Fully_prevented_opening_is_spent_and_still_grants_its_refinement_benefit(string form, int read, int guard)
    {
        var actor = Actor([Ability("strike", Hit())], Style(form, 7));
        var enemy = Enemy();
        var result = Engine(2).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick != 1) return;
            enemy.AdjustDamageTaken(DamageType.None, -100);
            actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        Assert.Equal(9900, enemy.Health);
        Assert.Single(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(read, Read(result));
        Assert.Equal(guard, actor.GetConditionStacks(StandardConditionType.Guard));
    }

    [Fact]
    public void Guarded_thrust_grants_normal_stacking_guard_and_two_incoming_hits_consume_two_charges()
    {
        var actor = Actor(Enumerable.Range(0, 8).Select(i => Ability($"hit{i}", Hit())), Style(CombatStyleIds.GuardedThrust, 3));
        var incoming = Hit(200); incoming.RepeatCount = 2;
        var enemy = Enemy(abilities: [Ability("answer", incoming)]);
        var result = Run(actor, enemy);
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "Duelist Opening"));
        Assert.Equal(0, actor.GetConditionStacks(StandardConditionType.Guard));
        Assert.Equal(700, actor.Health);
    }

    [Fact]
    public void Guarded_thrust_grants_guard_after_a_lethal_opening_and_guard_survives_target_changes_and_time()
    {
        var actor = Actor([Ability("strike", Hit(100, AbilityTargetSelector.LowestHealthEnemy))], Style(CombatStyleIds.GuardedThrust, 7));
        var first = Enemy("first", 200); var next = Enemy("next", 10000);
        var result = Engine(65).Run([actor], [first, next], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick is 1 or 2) actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        Assert.False(first.IsAlive);
        Assert.Equal(1, actor.GetConditionStacks(StandardConditionType.Guard));
        Assert.Equal(1, Read(result));
    }

    [Theory]
    [InlineData(3501, false, 155)]
    [InlineData(3500, false, 165)]
    [InlineData(3000, false, 165)]
    [InlineData(5000, true, 165)]
    [InlineData(5001, true, 155)]
    public void Finishing_touch_checks_health_before_the_spending_hit(int health, bool mastered, int damage)
    {
        var actor = Actor([Ability("strike", Hit())], Style(level: 10) with
        { UpgradeIds = [CombatStyleIds.FinishingTouch], MasteredUpgradeId = mastered ? CombatStyleIds.FinishingTouch : null });
        var enemy = Enemy();
        var result = Engine(2).Run([actor], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick != 1) return;
            enemy.SetHealth(health); actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        Assert.Equal(damage, result.EventLog.Last(x => x.Source == "strike.hit" && x.EventType == EventType.Damage).Magnitude);
    }

    [Theory]
    [InlineData(false, 155, 165)]
    [InlineData(true, 165, 165)]
    public void Know_your_enemy_rewards_later_openings_or_first_when_mastered(bool mastered, int first, int second)
    {
        var actor = Actor(Enumerable.Range(0, 6).Select(i => Ability($"hit{i}", Hit())), Style(level: 10) with
        { UpgradeIds = [CombatStyleIds.KnowYourEnemy], MasteredUpgradeId = mastered ? CombatStyleIds.KnowYourEnemy : null });
        var result = Run(actor, Enemy());
        Assert.Equal(new[] { first, second }, result.EventLog.Where(x => x.Source == "Duelist Opening").Select(x => x.Magnitude));
    }

    [Fact]
    public void Owner_death_clears_read_and_revival_does_not_refresh_first_impression()
    {
        var actor = Actor([Ability("strike", Hit())], Style(level: 7)); actor.SetHealth(10);
        var ally = Actor([], id: "ally");
        var enemy = Enemy(abilities: [Ability("kill", Hit(1000, AbilityTargetSelector.LowestHealthEnemy))]);
        var result = Engine(2).Run([actor, ally], [enemy], checkpointObserver: checkpoint =>
        {
            if (checkpoint.Tick != 1) return;
            Assert.False(actor.IsAlive);
            actor.SetHealth(1000); actor.Abilities[0].ReduceCooldown(1000);
        }, checkpointIntervalTicks: 1);
        Assert.Single(result.EventLog, x => x.Source == "First Impression" && x.ActorId == actor.Id);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(1, Read(result));
    }

    [Fact]
    public void Barrier_damage_builds_read_but_zero_damage_passives_and_triggered_basic_attacks_do_not()
    {
        var triggeredBasic = new AbilityEffectSpec { Id = "basic", Operation = AbilityEffectOperation.PerformBasicAttack, Target = AbilityTargetSelector.Self };
        var actor = Actor([Ability("zero", Hit(0)), Ability("strike", Hit()),
            Passive("proc", AbilityTriggerEvent.OnHit, Hit(1)), Ability("repeat", triggeredBasic)], basic: true);
        var enemy = Enemy(); enemy.GrantBarrier(enemy, 5000);
        var result = Run(actor, enemy);
        Assert.Equal(1, Read(result));
        Assert.DoesNotContain(result.EventLog, x => x.Source == "Duelist Opening");
        Assert.Equal(10000, enemy.Health);
    }

    [Fact]
    public void Logging_does_not_change_combat_outcome()
    {
        CombatResult Resolve(bool logging)
        {
            var actor = Actor(Enumerable.Range(0, 6).Select(i => Ability($"hit{i}", Hit())), Style(CombatStyleIds.Flurry, 10));
            return Engine(logging: logging).Run([actor], [Enemy()]);
        }
        var logged = Resolve(true); var logless = Resolve(false);
        Assert.Equal(logged.Outcome, logless.Outcome);
        Assert.Equal(logged.EntityStats.Sum(x => x.DamageDone), logless.EntityStats.Sum(x => x.DamageDone));
        Assert.Empty(logless.EventLog);
    }

    [Fact]
    public void Catalog_snapshots_capture_form_tuning_and_old_snapshots_omit_duelist()
    {
        var snapshot = Style(CombatStyleIds.PatientBlade, 10);
        var json = JsonSerializer.Serialize(snapshot);
        var copy = JsonSerializer.Deserialize<CombatStyleSnapshot>(json)!;
        Assert.Equal(5, copy.Tuning.Duelist!.ReadRequired);
        Assert.Equal(1.9, copy.Tuning.Duelist.Multiplier(10), 6);
        Assert.Equal(json, JsonSerializer.Serialize(copy));
        var old = new CombatStyleSnapshot { CombatStyleId = CombatStyleIds.Reaper, Kind = CombatStyleKind.Reaper };
        Assert.DoesNotContain("Duelist", JsonSerializer.Serialize(old));
    }

    private static int Read(CombatResult result) => result.EventLog.Last(x => x.Source == "Duelist Read").Magnitude;
    private static CombatStyleSnapshot Style(string? form = null, int level = 0)
    {
        var catalog = CombatStyleFoundationTests.LoadCatalog();
        var definition = catalog.Styles.Single(x => x.Id == CombatStyleIds.Duelist);
        return CombatStyleRules.Snapshot(catalog, definition, new() { CombatStyleId = definition.Id, Level = level },
            new(definition.Id, form, [], null));
    }
    private static RuntimeCombatant Actor(IEnumerable<CompiledAbility> abilities, CombatStyleSnapshot? style = null,
        bool basic = false, string id = "actor")
    {
        var list = abilities.ToArray();
        return new(id, id, CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000, [AttributeType.Power] = 1000 },
            list, canBasicAttack: basic, combatStyle: style ?? Style(),
            essenceOrigins: list.Where(x => x.Kind == AbilitySpecKind.Active).ToDictionary(x => x.Id, _ => Guid.NewGuid()));
    }
    private static RuntimeCombatant Enemy(string id = "enemy", int health = 10000, IEnumerable<CompiledAbility>? abilities = null) =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = health }, abilities ?? [], canBasicAttack: false);
    private static FastCombatEngine Engine(int ticks = 1, int basicInterval = 30, bool logging = true) =>
        new(new Dictionary<string, CompiledStatus>(), new(MaxTicks: ticks, BasicAttackIntervalTicks: basicInterval, CaptureEventLog: logging));
    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant enemy) => Engine().Run([actor], [enemy]);
    private static CompiledAbility Ability(string id, params AbilityEffectSpec[] effects) => AbilityCompiler.CompileAbility(new AbilitySpec
    { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 1000, Effects = effects.Select(x =>
        {
            var copy = JsonSerializer.Deserialize<AbilityEffectSpec>(JsonSerializer.Serialize(x))!;
            copy.Id = $"{id}.{x.Id}";
            return copy;
        }).ToList() });
    private static CompiledAbility Passive(string id, AbilityTriggerEvent trigger, params AbilityEffectSpec[] effects) =>
        AbilityCompiler.CompileAbility(new AbilitySpec { Id = id, Name = id, Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = trigger }], Effects = effects.ToList() });
    private static AbilityEffectSpec Hit(int amount = 100, AbilityTargetSelector target = AbilityTargetSelector.CurrentTarget) =>
        new() { Id = "hit", Operation = AbilityEffectOperation.Damage, Target = target, BaseValue = amount,
            CritEligibility = CritEligibility.Disallowed };
}
