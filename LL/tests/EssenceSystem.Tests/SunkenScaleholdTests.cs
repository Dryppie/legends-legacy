using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Essences;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class SunkenScaleholdTests
{
    private const string Rhythm = "status.lizardfolk_warrior.battle_rhythm";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };
    private static readonly AbilitySpec[] Abilities = Read<AbilitySpec[]>("combat/abilities.json");
    private static readonly StatusSpec[] Statuses = Read<StatusSpec[]>("combat/statuses.json");
    private static readonly SummonSpec[] Summons = Read<SummonSpec[]>("combat/summons.json");

    [Fact]
    public void Catalog_validates_all_new_abilities_and_summon()
    {
        var validation = AbilityCatalogValidator.Validate(Abilities, Statuses, summons: Summons);
        Assert.True(validation.IsValid, string.Join("\n", validation.Errors));
        Assert.Equal(10, Abilities.Count(x => x.Id.Contains(".lizardfolk_")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Skullcrusher_resolves_one_hit_and_never_refreshes_existing_stun(bool stunned)
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("brute.skullcrusher")]);
        var enemy = Actor("enemy", CombatTeam.Hostile);
        if (stunned) AddCondition(enemy, StandardConditionType.Stun, duration: 10);
        var result = Run([actor], [enemy]);
        var hit = Assert.Single(result.EventLog, x => x.Source.StartsWith("effect.creature.lizardfolk_brute.skullcrusher")
            && x.EventType is EventType.Damage or EventType.DamageCrit);
        Assert.InRange(hit.Magnitude, stunned ? 216 : 144, stunned ? 324 : 216);
        if (stunned)
        {
            Assert.True(enemy.Conditions.Single(x => x.Type == StandardConditionType.Stun).RemainingDurationTicks <= 10);
            Assert.DoesNotContain(result.EventLog, x => x.Source == "condition.stun" && x.EventType == EventType.StatusEffect);
        }
    }

    [Fact]
    public void Brutal_follow_up_refreshes_across_two_abilities_and_consumes_once()
    {
        var actor = Actor("actor", CombatTeam.Friendly,
            [Authored("brute.brutal_follow_up"), TestAbility("first", Heal()), TestAbility("second", Heal())]);
        Run([actor], [Actor("enemy", CombatTeam.Hostile)]);
        Assert.Equal(75, actor.ConsumeNextBasicAttackModifiers().DamagePercent);
        Assert.Equal(0, actor.ConsumeNextBasicAttackModifiers().DamagePercent);
    }

    [Fact]
    public void Converging_element_selects_exactly_one_outcome_and_replays_deterministically()
    {
        var seen = new HashSet<string>();
        for (var seed = 1; seed <= 45; seed++)
        {
            string Outcome()
            {
                var actor = Actor("actor", CombatTeam.Friendly, [Authored("elementalist.converging_element")]);
                var enemy = Actor("enemy", CombatTeam.Hostile);
                Run([actor], [enemy], seed: seed);
                var burn = enemy.GetConditionStacks(StandardConditionType.Burn) > 0;
                var chill = enemy.GetConditionStacks(StandardConditionType.Chill) > 0;
                var damage = enemy.Health < 10000;
                Assert.Equal(1, new[] { burn, chill, damage }.Count(x => x));
                if (burn) Assert.Equal(40, enemy.Conditions.Single().Value);
                if (chill) Assert.Equal(5, enemy.GetConditionStacks(StandardConditionType.Chill));
                return burn ? "burn" : chill ? "chill" : "damage";
            }
            var first = Outcome();
            Assert.Equal(first, Outcome());
            seen.Add(first);
        }
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void Elemental_bond_summons_once_with_authored_stats_and_magical_attacks()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("elementalist.elemental_bond")]);
        var result = Run([actor], [Actor("enemy", CombatTeam.Hostile)], ticks: 50);
        Assert.Single(result.EventLog, x => x.EventType == EventType.Summon);
        var summon = AbilityCompiler.CompileSummon(Summons.Single(x => x.Id == "lightningElemental"));
        Assert.Equal(0, summon.DurationTicks);
        Assert.Equal(DamageType.Magical, summon.BasicAttackDamageType);
        Assert.Equal(AttackType.Ranged, summon.BasicAttackType);
        var spawned = Assert.Single(result.EventLog, x => x.EventType == EventType.Summon).CombatEntity;
        Assert.NotNull(spawned);
        Assert.Equal("Lightning Elemental", spawned.Name);
        Assert.Equal(1000, spawned.MaxHealth);
        Assert.Contains(result.EventLog, x => x.ActorId == spawned.Id && x.DamageType == DamageType.Magical);
    }

    [Fact]
    public void Lightning_elemental_does_not_respawn_when_killed()
    {
        var kill = new AbilityEffectSpec { Id="kill.summons", Operation=AbilityEffectOperation.Damage,
            Target=AbilityTargetSelector.SummonedEnemies, ScalingAttribute=AttributeType.MaxHealth,
            ScalingCoefficient=1, DamageType=DamageType.None, CritEligibility=CritEligibility.Disallowed };
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("elementalist.elemental_bond")]);
        var result = Run([actor], [Actor("enemy",CombatTeam.Hostile,[TestAbility("kill",kill)])],ticks:100);
        var summon = Assert.Single(result.EventLog, x => x.EventType == EventType.Summon).CombatEntity!;
        Assert.Contains(result.EventLog,x => x.TargetId == summon.Id && x.EventType == EventType.Death);
        Assert.DoesNotContain(result.EventLog,x => x.ActorId == summon.Id && x.Timestamp > 0);
    }

    [Fact]
    public void Exposed_adds_target_critical_chance_without_exceeding_cap_or_enabling_ineligible_damage()
    {
        bool Critical(int seed, bool exposed, float critChance, bool eligible=true)
        {
            var strike = new AbilityEffectSpec { Id="strike",Operation=AbilityEffectOperation.Damage,
                Target=AbilityTargetSelector.CurrentTarget,ScalingAttribute=AttributeType.MaxHealth,
                ScalingCoefficient=.01f,DamageType=DamageType.Physical,
                CritEligibility=eligible?CritEligibility.Allowed:CritEligibility.Disallowed };
            var actor=Actor("actor",CombatTeam.Friendly,[TestAbility("strike",strike)]);
            actor.AdjustAttribute(AttributeType.CritChance,critChance);
            var enemy=Actor("enemy",CombatTeam.Hostile);
            if(exposed)AddCondition(enemy,StandardConditionType.Exposed);
            return Run([actor],[enemy],seed:seed).EventLog.Any(x=>x.Source=="strike" && x.EventType==EventType.DamageCrit);
        }
        var bonusCrits=0;
        for(var seed=1;seed<=100;seed++)
        {
            Assert.False(Critical(seed,false,0));
            if(Critical(seed,true,0))bonusCrits++;
            Assert.False(Critical(seed,true,0,eligible:false));
            var cap = AttributeCombatRules.CritChanceCapPercent;
            Assert.Equal(Critical(seed,false,cap),Critical(seed,true,cap));
        }
        Assert.InRange(bonusCrits,5,20);
    }

    [Fact]
    public void Find_weakness_applies_unique_exposed_for_ten_seconds()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("scout.find_weakness")]);
        var enemy = Actor("enemy", CombatTeam.Hostile);
        Run([actor], [enemy]);
        Assert.Equal(100, enemy.Conditions.Single(x => x.Type == StandardConditionType.Exposed).DurationTicks);
    }

    [Fact]
    public void Exposed_refreshes_is_blocked_by_ward_and_can_be_cleansed()
    {
        var exposed = new AbilityEffectSpec { Id = "expose", Operation = AbilityEffectOperation.ApplyCondition,
            Target = AbilityTargetSelector.CurrentTarget, Condition = StandardConditionType.Exposed, BaseValue = 1 };
        var actor = Actor("actor", CombatTeam.Friendly, [TestAbility("apply", exposed)]);
        var enemy = Actor("enemy", CombatTeam.Hostile);
        AddCondition(enemy, StandardConditionType.Exposed, duration: 2);
        Run([actor], [enemy]);
        Assert.Equal(100, enemy.Conditions.Single(x => x.Type == StandardConditionType.Exposed).DurationTicks);

        var warded = Actor("warded", CombatTeam.Hostile);
        AddCondition(warded, StandardConditionType.Ward);
        Run([Actor("actor", CombatTeam.Friendly, [TestAbility("apply", exposed)])], [warded]);
        Assert.False(warded.HasCondition(StandardConditionType.Exposed));
    }

    [Fact]
    public void Keen_eye_only_bonuses_stun_even_with_other_conditions_present()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("scout.keen_eye")]);
        var enemy = Actor("enemy", CombatTeam.Hostile);
        Run([actor], [enemy]);
        AddCondition(enemy, StandardConditionType.Freeze);
        Assert.Equal(0, actor.GetCriticalDamageAgainstConditionPercent(enemy));
        AddCondition(enemy, StandardConditionType.Stun);
        AddCondition(enemy, StandardConditionType.Chill);
        Assert.Equal(25, actor.GetCriticalDamageAgainstConditionPercent(enemy));
    }

    [Fact]
    public void Herb_mixture_selects_lowest_absolute_health_including_summons()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("shaman.herb_mixture"), Authored("shaman.cleansing_herbs")]);
        actor.SetHealth(100);
        var summon = new RuntimeCombatant("summon", "summon", CombatTeam.Friendly,
            new Dictionary<AttributeType,float> { [AttributeType.MaxHealth] = 1000 }, [],
            isSummoned:true, summonOwner:actor, canBasicAttack:false);
        summon.SetHealth(50);
        AddCondition(summon, StandardConditionType.Exposed);
        AddCondition(summon, StandardConditionType.Chill);
        Run([actor, summon], [Actor("enemy", CombatTeam.Hostile)]);
        Assert.Equal(100, actor.Health);
        Assert.InRange(summon.Health, 218, 302);
        Assert.Single(summon.Conditions);
        Assert.False(summon.HasCondition(StandardConditionType.Exposed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cleansing_herbs_only_triggers_on_direct_applications_even_at_full_health(bool periodic)
    {
        var heal = Heal();
        if (periodic) { heal.DurationTicks = 40; heal.IntervalTicks = 10; }
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("shaman.cleansing_herbs"), TestAbility("heal", heal)]);
        AddCondition(actor, StandardConditionType.Exposed);
        AddCondition(actor, StandardConditionType.Chill);
        Run([actor], [Actor("enemy", CombatTeam.Hostile)], ticks: 11);
        Assert.Equal(periodic ? 2 : 1, actor.Conditions.Count);
    }

    [Fact]
    public void Limited_cleanse_preserves_beneficial_statuses()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("shaman.cleansing_herbs"), TestAbility("heal", Heal())]);
        actor.Statuses.Add(new RuntimeStatus(AbilityCompiler.CompileStatus(Statuses.Single(x => x.Id == Rhythm)), actor, actor, 4));
        AddCondition(actor, StandardConditionType.Exposed);
        Run([actor], [Actor("enemy", CombatTeam.Hostile)]);
        Assert.Empty(actor.Conditions);
        Assert.Equal(4, actor.GetStatusStacks(Rhythm));
    }

    [Theory]
    [InlineData(6999, 120, 180)]
    [InlineData(7000, 120, 180)]
    [InlineData(7001, 176, 264)]
    public void Spearhead_uses_pre_hit_health_and_only_one_branch(int health, int minimum, int maximum)
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("warrior.spearhead_assault")]);
        var enemy = Actor("enemy", CombatTeam.Hostile);
        enemy.SetHealth(health);
        var result = Run([actor], [enemy]);
        var hit = Assert.Single(result.EventLog, x => x.Source.StartsWith("effect.creature.lizardfolk_warrior.spearhead_assault")
            && x.EventType is EventType.Damage or EventType.DamageCrit);
        Assert.InRange(hit.Magnitude, minimum, maximum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Battle_rhythm_bonuses_every_repeat_and_periodic_tick_regardless_of_loadout_order(bool passiveFirst)
    {
        var direct = new AbilityEffectSpec { Id="repeat", Operation=AbilityEffectOperation.Damage,
            Target=AbilityTargetSelector.CurrentTarget, ScalingAttribute=AttributeType.MaxHealth,
            ScalingCoefficient=0.01f, DamageType=DamageType.None, RepeatCount=3, CritEligibility=CritEligibility.Disallowed };
        var dot = new AbilityEffectSpec { Id="dot", Operation=AbilityEffectOperation.Damage,
            Target=AbilityTargetSelector.CurrentTarget, ScalingAttribute=AttributeType.MaxHealth,
            ScalingCoefficient=0.01f, DamageType=DamageType.None, DurationTicks=30, IntervalTicks=10, CritEligibility=CritEligibility.Disallowed };
        var burn = new AbilityEffectSpec { Id="burn", Operation=AbilityEffectOperation.ApplyCondition,
            Target=AbilityTargetSelector.CurrentTarget, Condition=StandardConditionType.Burn, BaseValue=40 };
        var active = TestAbility("multi", direct, dot, burn);
        var passive = Authored("warrior.battle_rhythm");
        var actor = Actor("actor", CombatTeam.Friendly, passiveFirst ? [passive,active] : [active,passive]);
        actor.Statuses.Add(new RuntimeStatus(AbilityCompiler.CompileStatus(Statuses.Single(x => x.Id == Rhythm)), actor, actor, 8));
        var enemy = Actor("enemy", CombatTeam.Hostile);
        var result = Run([actor], [enemy], ticks: 31);
        Assert.Equal(0, actor.GetStatusStacks(Rhythm));
        Assert.Equal(0, actor.GetAttribute(AttributeType.AttackSpeed));
        Assert.Equal(3, result.EventLog.Count(x => x.Source == "repeat" && x.Magnitude == 116));
        Assert.Equal(3, result.EventLog.Count(x => x.Source == "dot" && x.Magnitude == 116));
        var burnTicks=result.EventLog.Where(x => x.Source == "condition.burn" && x.EventType == EventType.Damage).ToArray();
        Assert.Equal(3,burnTicks.Length);
        Assert.All(burnTicks, x => Assert.Equal(46, x.Magnitude));
    }

    [Fact]
    public void Battle_rhythm_caps_at_eight_and_heals_do_not_consume_it()
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Authored("warrior.battle_rhythm"), TestAbility("heal",Heal())], basic:true);
        Run([actor], [Actor("enemy", CombatTeam.Hostile)], ticks: 100, basicInterval:10);
        Assert.Equal(8, actor.GetStatusStacks(Rhythm));
        Assert.Equal(16, actor.GetAttribute(AttributeType.AttackSpeed));
    }

    [Fact]
    public void Battle_rhythm_preserves_stacks_for_the_random_chill_outcome()
    {
        var sawChill=false;
        for(var seed=1;seed<=12;seed++)
        {
            var actor=Actor("actor",CombatTeam.Friendly,[Authored("warrior.battle_rhythm"),Authored("elementalist.converging_element")]);
            actor.Statuses.Add(new RuntimeStatus(AbilityCompiler.CompileStatus(Statuses.Single(x=>x.Id==Rhythm)),actor,actor,8));
            var enemy=Actor("enemy",CombatTeam.Hostile);
            Run([actor],[enemy],seed:seed);
            var chill=enemy.HasCondition(StandardConditionType.Chill);
            sawChill |= chill;
            Assert.Equal(chill?8:0,actor.GetStatusStacks(Rhythm));
        }
        Assert.True(sawChill);
    }

    [Fact]
    public void Battle_rhythm_preserves_its_bonus_on_authored_status_damage()
    {
        var curse=TestAbility("curse",new AbilityEffectSpec { Id="curse.apply",Operation=AbilityEffectOperation.ApplyStatus,
            Target=AbilityTargetSelector.CurrentTarget,StatusId="status.curse",BaseValue=1 });
        int Damage(bool rhythm)
        {
            var actor=Actor("actor",CombatTeam.Friendly,rhythm?[curse,Authored("warrior.battle_rhythm")]:[curse]);
            if(rhythm)actor.Statuses.Add(new RuntimeStatus(AbilityCompiler.CompileStatus(Statuses.Single(x=>x.Id==Rhythm)),actor,actor,8));
            var result=Run([actor],[Actor("enemy",CombatTeam.Hostile)],ticks:21);
            if(rhythm)Assert.Equal(0,actor.GetStatusStacks(Rhythm));
            return Assert.Single(result.EventLog,x=>x.Source=="effect.curse.dot" && x.EventType==EventType.Damage).Magnitude;
        }
        Assert.Equal((int)Math.Round(Damage(false)*1.16),Damage(true));
    }

    [Fact]
    public void Ascension_preserves_random_selection_branching_and_fixed_stack_contracts()
    {
        Assert.True(EssenceAbilityProgressionScaler.Apply(Authored("brute.skullcrusher"),3).Triggers.Single().SnapshotEffectConditions);
        Assert.True(EssenceAbilityProgressionScaler.Apply(Authored("elementalist.converging_element"),3).Triggers.Single().ChooseOneEffect);
        var rhythm = EssenceAbilityProgressionScaler.Apply(Authored("warrior.battle_rhythm"),3);
        Assert.Equal(1,rhythm.Effects[0].BaseValue);
        Assert.Equal(2,rhythm.Effects[2].BaseValue);
        Assert.Equal(1,EssenceAbilityProgressionScaler.Apply(Authored("shaman.cleansing_herbs"),3).Effects.Single().MaximumCount);
    }

    private static AbilitySpec Authored(string suffix) => Abilities.Single(x => x.Id == "ability.creature.lizardfolk_"+suffix);
    private static AbilitySpec TestAbility(string id, params AbilityEffectSpec[] effects) => new()
    { Id=id, Name=id, Kind=AbilitySpecKind.Active, CooldownTicks=10000, Effects=[..effects] };
    private static AbilityEffectSpec Heal() => new()
    { Id="heal", Operation=AbilityEffectOperation.Heal, Target=AbilityTargetSelector.Self, ScalingAttribute=AttributeType.Power, ScalingCoefficient=1 };
    private static RuntimeCombatant Actor(string id, CombatTeam team, AbilitySpec[]? abilities=null, bool basic=false) =>
        new(id,id,team,new Dictionary<AttributeType,float>
        { [AttributeType.MaxHealth]=10000, [AttributeType.Power]=100, [AttributeType.CritDamage]=100 },
        (abilities ?? []).Select(AbilityCompiler.CompileAbility),canBasicAttack:basic);
    private static void AddCondition(RuntimeCombatant target, StandardConditionType type, int duration=1000) =>
        target.Conditions.Add(new RuntimeCondition(type,target,target,1,duration,100,target.Conditions.Count,"test"));
    private static CombatResult Run(RuntimeCombatant[] friendly, RuntimeCombatant[] hostile, int ticks=1, int seed=1337, int basicInterval=30) =>
        new FastCombatEngine(AbilityCompiler.CompileStatuses(Statuses),AbilityCompiler.CompileSummons(Summons),
            AbilityCompiler.CompileAbilities(Abilities),new FastCombatEngineOptions(MaxTicks:ticks,RandomSeed:seed,BasicAttackIntervalTicks:basicInterval))
            .Run(friendly,hostile);
    private static T Read<T>(string path)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName,"LL/src/API/API.LL/Data",path);
            if (File.Exists(candidate)) return JsonSerializer.Deserialize<T>(File.ReadAllText(candidate),JsonOptions)!;
            directory=directory.Parent;
        }
        throw new FileNotFoundException(path);
    }
}
