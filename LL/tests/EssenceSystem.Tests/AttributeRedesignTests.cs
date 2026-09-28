using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Combat.Engine;
using Services.LL.Items;
using Domain.Models.Damages;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

public sealed class AttributeRedesignTests
{
    [Fact]
    public async Task Competitive_execution_rejects_mixed_snapshot_rules_before_loading_or_running_combat()
    {
        CombatRuntimeParticipant Participant(CombatSide side, int version)
        {
            var entity = new Domain.Models.Entities.Characters.Character { AttributeRulesVersion = version };
            return new(new(side.ToString(), entity.Id, side), entity, new CombatEntity(entity));
        }
        var current = Participant(CombatSide.Friendly, 18);
        var legacy = Participant(CombatSide.Hostile, 17);
        var id = Guid.NewGuid();
        var plan = new CombatEncounterPlan(id, CombatMode.Pvp, 1, DateTimeOffset.UtcNow, [current.Slot, legacy.Slot],
            new PvpEncounterSourceContext(id, current.SourceEntity.Id, legacy.SourceEntity.Id)) { ContentType = CombatContentType.Arena };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new CombatEngineExecutor(null!)
            .ExecuteSimulationAsync(new(plan, [current], [legacy]), new CombatRuleset(1, 10), default));
        Assert.Contains("one attribute rules version", error.Message);
    }

    [Fact]
    public void Authored_defense_survives_zero_rating_defaults_and_temporary_alias_removal()
    {
        var creature = new Domain.Models.Entities.Characters.Character();
        creature.BaseAttributes = Domain.Helpers.EntityBaseAttributeHelper.CreateEntityAttributes(creature.Id);
        creature.BaseAttributes.Single(x => x.AttributeType == AttributeType.Armor).Value = 40;
        var actor = new CombatEntity(creature);
        AttributeCalculator.CalculateBaseCombatAttributes(actor);
        Assert.Equal(165, actor.CombatAttributes[AttributeType.ArmorRating], 4);
        Assert.Equal(40, actor.CombatAttributes[AttributeType.Armor], 4);
        var buff = new Domain.Models.Attributes.Modifiers.InstanceAttributeModifier(AttributeType.Armor, 165,
            Domain.Models.Attributes.Modifiers.ModifierType.Flat);
        actor.ModifyAttribute(buff);
        Assert.Equal(330, actor.CombatAttributes[AttributeType.ArmorRating], 4);
        Assert.Equal(100 * AttributeRules.Mitigation(330), actor.CombatAttributes[AttributeType.Armor], 4);
        actor.ModifyAttribute(buff, remove: true);
        Assert.Equal(165, actor.CombatAttributes[AttributeType.ArmorRating], 4);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(165, .4)]
    [InlineData(330, .533333333)]
    [InlineData(660, .64)]
    public void Finite_defense_curve_has_expected_mitigation(double rating, double mitigation) =>
        Assert.Equal(mitigation, AttributeRules.Mitigation(rating), 6);

    [Fact]
    public void Penetration_works_at_high_finite_defense_and_corrosion_opposes_rating()
    {
        Assert.Equal(AttributeRules.Mitigation(100000) - .4f, AttributeRules.Mitigation(100000, 60), 6);
        Assert.Equal(.08f, AttributeRules.Mitigation(495, 40, 50), 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeRules.Mitigation(double.PositiveInfinity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(40)]
    public void Whole_loadout_legacy_cooldown_conversion_preserves_cadence(float reduction)
    {
        var haste = AttributeRules.HasteFromCooldownReduction(reduction);
        Assert.InRange(Math.Abs(AttributeRules.CooldownTicks(300, haste)
            - AttributeCombatRules.CalculateCooldownTicks(300, reduction)), 0, 1);
        Assert.Equal(0, AttributeRules.CooldownTicks(0, haste));
    }

    [Fact]
    public void Effective_caps_preserve_overcap_contributions_when_buffs_are_removed()
    {
        var actor = new RuntimeCombatant("a", "a", CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 100, [AttributeType.CritDamage] = 490 },
            [], attributeRulesVersion: AttributeRules.CurrentVersion);
        actor.AdjustAttribute(AttributeType.CritDamage, 50);
        Assert.Equal(500, actor.GetAttribute(AttributeType.CritDamage));
        Assert.Equal(540, actor.Attributes[AttributeType.CritDamage]);
        actor.AdjustAttribute(AttributeType.CritDamage, -50);
        Assert.Equal(490, actor.GetAttribute(AttributeType.CritDamage));
    }

    [Fact]
    public void Tenacity_policy_distinguishes_harmful_timers_expiry_damage_and_charges()
    {
        Assert.Equal(ConditionResistancePolicy.Duration, ConditionResistanceRules.For(StandardConditionType.Stun));
        Assert.Equal(ConditionResistancePolicy.ExpiryDamage, ConditionResistanceRules.For(StandardConditionType.Doom));
        Assert.Equal(ConditionResistancePolicy.Unaffected, ConditionResistanceRules.For(StandardConditionType.Guard));
        Assert.Equal(ConditionResistancePolicy.Unaffected, ConditionResistanceRules.For(StandardConditionType.Empower));
        Assert.Equal(24, AttributeRules.HarmfulDurationTicks(30, 20));
        Assert.Equal(1, AttributeRules.HarmfulDurationTicks(1, 80));
        Assert.True(ConditionResistanceRules.IsHarmfulStatus(["Status.Debuff"]));
        Assert.False(ConditionResistanceRules.IsHarmfulStatus(["Status.Buff"]));
    }

    [Fact]
    public void Catalog_allocations_preserve_budget_slot_rules_and_historical_prices()
    {
        var path = Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json");
        var catalog = JsonStarterEquipmentCatalog.Load(path);
        var historical = JsonStarterEquipmentCatalog.Load(path, balanceVersion: 1);
        Assert.Equal(30, historical.Evaluator.Evaluate("plain.dagger", 1, 0, null).Stats[AttributeType.AttackSpeed]);
        Assert.Equal(15, catalog.Evaluator.Evaluate("plain.dagger", 1, 0, null).Stats[AttributeType.AttackSpeed]);
        foreach (var definition in catalog.Evaluator.Definitions)
        {
            var result = catalog.Evaluator.Evaluate(definition.Id, 1, 0, definition.NativeStyleId);
            Assert.NotNull(result.Allocation);
            Assert.Equal(result.TargetBudget, result.Allocation.Total, 5);
            Assert.All(result.Stats.Keys, attribute => Assert.True(AttributeRules.IsOrdinaryEquipmentAttribute(attribute)));
            EquipmentSpecializationRules.ValidateCombination(result.Stats.Keys);
        }
    }

    [Fact]
    public void Current_gear_defense_is_stable_across_character_level_boundaries()
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));
        var state = EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.heavy_helm", 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "test", "test"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));
        var data = EquipmentData.Create(state, catalog.Evaluator);
        var item = new EquipmentInstance { Id = state.Id, ItemBaseId = data.ItemBaseId, ItemBase = catalog.GetEquipmentBase(data.ItemBaseId) };
        item.ApplyProgressionData(data);
        var before = AttributeCalculator.CalculateProjectedEquipmentAttributes(new Dictionary<AttributeType, float>(), [item], 50);
        var after = AttributeCalculator.CalculateProjectedEquipmentAttributes(new Dictionary<AttributeType, float>(), [item], 51);
        Assert.Equal(before[AttributeType.Armor], after[AttributeType.Armor]);
        Assert.True(before[AttributeType.ArmorRating] > 0);
    }

    [Theory]
    [InlineData(StandardConditionType.Stun, 3, 24)]
    [InlineData(StandardConditionType.Poison, 1, 96)]
    [InlineData(StandardConditionType.Empower, 1, 100)]
    [InlineData(StandardConditionType.Doom, 1, 150)]
    public void Production_engine_applies_condition_specific_tenacity(StandardConditionType condition, int value, int ticks)
    {
        var ability = Active("condition", new AbilityEffectSpec { Id = "apply", Operation = AbilityEffectOperation.ApplyCondition,
            Condition = condition, Target = AbilityTargetSelector.CurrentTarget, BaseValue = value, GuaranteedConditionApplication = true });
        var actor = Actor("actor", CombatTeam.Friendly, [ability]);
        var target = Actor("target", CombatTeam.Hostile, [], new() { [AttributeType.Tenacity] = 20 });
        Run(actor, target);
        var applied = Assert.Single(target.Conditions);
        Assert.Equal(ticks, applied.DurationTicks);
        if (condition == StandardConditionType.Doom) Assert.Equal(.8, applied.DamageMultiplier, 6);
    }

    [Fact]
    public void Restoration_scales_barriers_but_not_life_steal()
    {
        var barrier = Active("barrier", new AbilityEffectSpec { Id = "barrier", Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self, BaseValue = 100 });
        var actor = Actor("actor", CombatTeam.Friendly, [barrier], new() { [AttributeType.Restoration] = 50 });
        Run(actor, Actor("enemy", CombatTeam.Hostile, []));
        Assert.Equal(150, actor.Barrier);
        var strike = Active("strike", new AbilityEffectSpec { Id = "strike", Operation = AbilityEffectOperation.Damage,
            Target = AbilityTargetSelector.CurrentTarget, BaseValue = 100, DamageType = DamageType.None,
            CritEligibility = CritEligibility.Disallowed });
        var leech = Actor("leech", CombatTeam.Friendly, [strike], new() { [AttributeType.LifeSteal] = 50, [AttributeType.Restoration] = 100 });
        leech.SetHealth(100);
        Run(leech, Actor("enemy", CombatTeam.Hostile, []));
        Assert.Equal(150, leech.Health);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 10)]
    public void Proc_coefficient_zero_disables_downstream_listener_without_reducing_hit_damage(int coefficient, int expectedPower)
    {
        var strike = Active("strike", new AbilityEffectSpec { Id = "strike", Operation = AbilityEffectOperation.Damage,
            Target = AbilityTargetSelector.CurrentTarget, BaseValue = 100, DamageType = DamageType.None,
            CritEligibility = CritEligibility.Disallowed, ProcCoefficient = coefficient });
        var proc = new AbilitySpec { Id = "proc", Name = "proc", Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = AbilityTriggerEvent.OnDamageDealt }],
            Effects = [new() { Id = "proc.power", Operation = AbilityEffectOperation.ModifyAttribute, Target = AbilityTargetSelector.Self,
                Attribute = AttributeType.Power, BaseValue = 10 }] };
        var actor = Actor("actor", CombatTeam.Friendly, [strike, proc]);
        var target = Actor("target", CombatTeam.Hostile, []);
        Run(actor, target);
        Assert.Equal(900, target.Health);
        Assert.Equal(expectedPower, actor.GetAttribute(AttributeType.Power));
    }

    [Theory]
    [InlineData(ProcScope.RootAction, 1, 10)]
    [InlineData(ProcScope.PerTarget, 1, 20)]
    [InlineData(ProcScope.RootAction, 2, 0)]
    public void Multi_hit_multi_target_actions_only_open_declared_listener_opportunities(ProcScope scope, int everyNth, int expected)
    {
        var strike = Active("sweep", new AbilityEffectSpec { Id = "sweep", Operation = AbilityEffectOperation.Damage,
            Target = AbilityTargetSelector.AllEnemies, RepeatCount = 3, BaseValue = 10,
            DamageType = DamageType.None, CritEligibility = CritEligibility.Disallowed });
        var listener = new AbilitySpec { Id = "listener", Name = "listener", Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = AbilityTriggerEvent.OnDamageDealt, ProcScope = scope, EveryNthOccurrence = everyNth }],
            Effects = [new() { Id = "power", Operation = AbilityEffectOperation.ModifyAttribute, Target = AbilityTargetSelector.Self,
                Attribute = AttributeType.Power, BaseValue = 10 }] };
        var actor = Actor("actor", CombatTeam.Friendly, [strike, listener]);
        var enemies = new[] { Actor("a", CombatTeam.Hostile, []), Actor("b", CombatTeam.Hostile, []) };
        new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1)).Run([actor], enemies);
        Assert.All(enemies, enemy => Assert.Equal(970, enemy.Health));
        Assert.Equal(expected, actor.GetAttribute(AttributeType.Power));
    }

    [Fact]
    public void Haste_applies_to_initial_repeat_and_reset_cooldowns_but_not_passive_internal_cooldowns()
    {
        var ability = Active("active", new AbilityEffectSpec { Id = "noop", Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self, BaseValue = 1 });
        ability.CooldownTicks = 100;
        var actor = Actor("actor", CombatTeam.Friendly, [ability], new() { [AttributeType.AbilityHaste] = 50 });
        var runtime = Assert.Single(actor.Abilities);
        runtime.StartInitialCooldown(50, true); Assert.Equal(67, runtime.RemainingCooldownTicks);
        runtime.StartCooldown(50, isHaste: true); Assert.Equal(67, runtime.RemainingCooldownTicks);
        Assert.Equal(67, actor.ResetAbilityCooldown("active"));
        var passive = AbilityCompiler.CompileAbility(new AbilitySpec { Id = "passive", Name = "passive", Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = AbilityTriggerEvent.OnHit, InternalCooldownTicks = 10 }] });
        var trigger = Assert.Single(passive.TriggersByEvent[AbilityTriggerEvent.OnHit]);
        var proc = new RuntimeAbility(passive); proc.StartTriggerCooldown(trigger);
        for (var i = 0; i < 9; i++) { proc.Tick(); Assert.False(proc.CanUseTrigger(trigger, 100)); }
        proc.Tick(); Assert.True(proc.CanUseTrigger(trigger, 100));
    }

    private static AbilitySpec Active(string id, AbilityEffectSpec effect) => new()
        { Id = id, Name = id, Kind = AbilitySpecKind.Active, CooldownTicks = 100, Effects = [effect] };

    private static RuntimeCombatant Actor(string id, CombatTeam team, AbilitySpec[] abilities, Dictionary<AttributeType, float>? stats = null)
    {
        stats ??= [];
        stats.TryAdd(AttributeType.MaxHealth, 1000);
        return new RuntimeCombatant(id, id, team, stats, abilities.Select(AbilityCompiler.CompileAbility),
            canBasicAttack: false, attributeRulesVersion: AttributeRules.CurrentVersion);
    }

    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant target) =>
        new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1,
            BasicAttackIntervalTicks: 1000)).Run([actor], [target]);
}
