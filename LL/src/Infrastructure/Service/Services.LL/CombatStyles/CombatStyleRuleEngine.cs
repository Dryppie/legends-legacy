using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleRuleEngine
{
    private readonly ICombatStyleDefinitionProvider _definitions;
    private readonly CombatStyleRuleSelector _selector;
    private readonly CombatStyleOperationExecutor _executor;

    public CombatStyleRuleEngine(ICombatStyleDefinitionProvider definitions)
    {
        _definitions = definitions;
        _selector = new CombatStyleRuleSelector(definitions);
        _executor = new CombatStyleOperationExecutor(definitions);
    }

    public CombatStyleRuntimeState? CreateState(CombatStyleSnapshot? snapshot, bool appliesToFriendlyTeam = true)
    {
        if (snapshot is null || _definitions.GetById(snapshot.StyleId) is not { } definition)
            return null;

        var state = new CombatStyleRuntimeState
        {
            StyleId = definition.Id,
            StyleLevel = snapshot.Level,
            FocusId = snapshot.SelectedFocusId,
            AppliesToFriendlyTeam = appliesToFriendlyTeam
        };

        state.Resources[definition.ResourceId] = 0m;
        foreach (var (nodeId, rank) in snapshot.NodeRanks ?? new Dictionary<string, int>())
        {
            if (rank > 0)
                state.NodeRanks[nodeId] = rank;
        }

        return state;
    }

    public int ModifyEffectAmount(
        CombatStyleRuntimeState? state,
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        int amount)
    {
        if (state is null || amount <= 0)
            return amount;

        var context = CombatStyleRuleContext.ForEffect(
            state,
            effect,
            source,
            target,
            effect.ProcCoefficient,
            amount);

        ApplyRules(context, CombatStyleEventType.EffectCalculation, CombatStyleRulePhase.ModifyEffect);
        ApplyPendingEmpowerments(context);

        var modified = context.Amount + context.BonusDamage;
        if (context.AdditivePercent != 0)
            modified = Math.Max(0, (int)Math.Round(modified * (1 + context.AdditivePercent)));

        context.Amount = modified;
        ApplyRules(context, CombatStyleEventType.EffectCalculation, CombatStyleRulePhase.AfterEffect);

        return context.Amount;
    }

    public void OnAbilityResolved(CombatStyleRuntimeState? state, CompiledAbility ability, RuntimeCombatant actor)
    {
        if (state is null)
            return;

        var context = CombatStyleRuleContext.ForAbility(state, ability, actor);
        ApplyRules(context, CombatStyleEventType.AbilityResolved, CombatStyleRulePhase.SideEffect);
    }

    public void OnDamageDealt(
        CombatStyleRuntimeState? state,
        CompiledEffect? effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        int healthDamage,
        decimal procCoefficient)
    {
        if (state is null || healthDamage <= 0)
            return;

        var context = CombatStyleRuleContext.ForDamageEvent(state, effect, source, target, procCoefficient);
        ApplyRules(context, CombatStyleEventType.DamageDealt, CombatStyleRulePhase.SideEffect);
    }

    public void OnDamageTaken(
        CombatStyleRuntimeState? state,
        CompiledEffect? effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        int healthDamage,
        decimal procCoefficient)
    {
        if (state is null || healthDamage <= 0)
            return;

        var context = CombatStyleRuleContext.ForDamageEvent(state, effect, source, target, procCoefficient);
        ApplyRules(context, CombatStyleEventType.DamageTaken, CombatStyleRulePhase.SideEffect);
    }

    public Dictionary<AttributeType, float> ModifySummonAttributes(
        CombatStyleRuntimeState? state,
        RuntimeCombatant owner,
        IReadOnlyDictionary<AttributeType, float> attributes)
    {
        var modified = new Dictionary<AttributeType, float>(attributes);
        if (state is null)
            return modified;

        var context = CombatStyleRuleContext.ForSummonAttributes(state, owner, modified);
        ApplyRules(context, CombatStyleEventType.SummonCreated, CombatStyleRulePhase.SummonAttributes);
        return modified;
    }

    private void ApplyRules(
        CombatStyleRuleContext context,
        CombatStyleEventType eventType,
        CombatStyleRulePhase phase)
    {
        context.EventType = eventType;
        if (phase == CombatStyleRulePhase.SummonAttributes)
        {
            ApplySummonAttributeRules(context, eventType);
            return;
        }

        foreach (var rule in _selector.GetActiveRules(context.State, eventType))
        {
            if (!CombatStylePredicateEvaluator.Matches(context, rule.Predicate)
                || !CanExecuteInPhase(rule.Operation, phase)
                || !TryConsumeRuleTrigger(context.State, rule, context.Source, context.Target))
            {
                continue;
            }

            _executor.Execute(context, rule.Operation);
        }
    }

    private void ApplySummonAttributeRules(
        CombatStyleRuleContext context,
        CombatStyleEventType eventType)
    {
        if (context.SummonAttributes is null)
            return;

        var maxHealthPercent = 0m;
        var damagePercent = 0m;

        foreach (var rule in _selector.GetActiveRules(context.State, eventType))
        {
            if (rule.Operation is not ModifySummonStatsOperation operation
                || !CombatStylePredicateEvaluator.Matches(context, rule.Predicate)
                || !TryConsumeRuleTrigger(context.State, rule, context.Source, context.Target))
            {
                continue;
            }

            if (operation.MaxHealthPercent is not null || operation.MaxHealthPercentModifiers is not null)
            {
                maxHealthPercent += CombatStyleValueEvaluator.Evaluate(
                    context.State,
                    operation.MaxHealthPercent ?? 0m,
                    operation.MaxHealthPercentModifiers);
            }

            if (operation.DamagePercent is not null || operation.DamagePercentModifiers is not null)
            {
                damagePercent += CombatStyleValueEvaluator.Evaluate(
                    context.State,
                    operation.DamagePercent ?? 0m,
                    operation.DamagePercentModifiers);
            }
        }

        MultiplySummonAttribute(context.SummonAttributes, AttributeType.MaxHealth, maxHealthPercent);
        MultiplySummonAttribute(context.SummonAttributes, AttributeType.Power, damagePercent);
    }

    private static void MultiplySummonAttribute(
        Dictionary<AttributeType, float> attributes,
        AttributeType attribute,
        decimal percent)
    {
        if (percent == 0 || !attributes.TryGetValue(attribute, out var value))
            return;

        attributes[attribute] = value * (1f + (float)percent);
    }

    private static bool CanExecuteInPhase(StyleRuleOperation operation, CombatStyleRulePhase phase) =>
        phase switch
        {
            CombatStyleRulePhase.ModifyEffect => operation is ModifyEffectAmountOperation
                or AddDamageReductionOperation
                or AddBonusDamageFromStatOperation,
            CombatStyleRulePhase.AfterEffect => operation is GainStyleResourceOperation
                or SetPendingEmpowermentOperation,
            CombatStyleRulePhase.SideEffect => operation is GainStyleResourceOperation
                or SetPendingEmpowermentOperation
                or GrantBarrierFromMaxHealthOperation,
            CombatStyleRulePhase.SummonAttributes => operation is ModifySummonStatsOperation,
            _ => false
        };

    private static void ApplyPendingEmpowerments(CombatStyleRuleContext context)
    {
        foreach (var empowerment in context.State.PendingEmpowerments.ToList())
        {
            if (!CombatStylePredicateEvaluator.Matches(context, empowerment.AppliesTo))
                continue;

            context.AdditivePercent += empowerment.AdditivePercent;
            if (empowerment.ConsumeOnUse)
                context.State.PendingEmpowerments.Remove(empowerment);
        }
    }

    private static bool TryConsumeRuleTrigger(
        CombatStyleRuntimeState state,
        CombatStyleRuleDefinition rule,
        RuntimeCombatant source,
        RuntimeCombatant target)
    {
        var keys = new List<string>(3);
        if (rule.MaxTriggersPerEncounter is not null)
            keys.Add(CreateRuleTriggerKey(rule.Id, "encounter", null));
        if (rule.MaxTriggersPerSource is not null)
            keys.Add(CreateRuleTriggerKey(rule.Id, "source", source.Id));
        if (rule.MaxTriggersPerTarget is not null)
            keys.Add(CreateRuleTriggerKey(rule.Id, "target", target.Id));

        if (keys.Count == 0)
            return true;

        foreach (var key in keys)
        {
            var max = key.Contains("|source|", StringComparison.OrdinalIgnoreCase)
                ? rule.MaxTriggersPerSource
                : key.Contains("|target|", StringComparison.OrdinalIgnoreCase)
                    ? rule.MaxTriggersPerTarget
                    : rule.MaxTriggersPerEncounter;

            if (max is not null && state.TriggerCounts.GetValueOrDefault(key) >= max.Value)
                return false;
        }

        foreach (var key in keys)
            state.TriggerCounts[key] = state.TriggerCounts.GetValueOrDefault(key) + 1;

        return true;
    }

    private static string CreateRuleTriggerKey(string ruleId, string scope, string? entityId) =>
        $"rule|{ruleId}|{scope}|{entityId ?? "all"}";
}

internal enum CombatStyleRulePhase
{
    ModifyEffect,
    AfterEffect,
    SideEffect,
    SummonAttributes
}

internal sealed class CombatStyleRuleSelector
{
    private readonly ICombatStyleDefinitionProvider _definitions;

    public CombatStyleRuleSelector(ICombatStyleDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public IEnumerable<CombatStyleRuleDefinition> GetActiveRules(
        CombatStyleRuntimeState state,
        CombatStyleEventType eventType)
    {
        var definition = _definitions.GetById(state.StyleId);
        if (definition is null)
            yield break;

        foreach (var rule in definition.Rules)
        {
            if (IsActive(state, rule, eventType))
                yield return rule;
        }

        if (state.FocusId is not null
            && _definitions.GetFocus(state.StyleId, state.FocusId) is { } focus
            && state.StyleLevel >= focus.UnlockLevel)
        {
            foreach (var rule in focus.Rules)
            {
                if (IsActive(state, rule, eventType))
                    yield return rule;
            }
        }

        foreach (var node in definition.SkillTreeNodes)
        {
            if (state.NodeRanks.GetValueOrDefault(node.Id) <= 0)
                continue;

            foreach (var rule in node.Rules)
            {
                if (IsActive(state, rule, eventType))
                    yield return rule;
            }
        }

    }

    private static bool IsActive(
        CombatStyleRuntimeState state,
        CombatStyleRuleDefinition rule,
        CombatStyleEventType eventType) =>
        rule.EventType == eventType
        && state.StyleLevel >= rule.MinStyleLevel
        && (rule.MaxStyleLevel is null || state.StyleLevel <= rule.MaxStyleLevel.Value);
}

internal sealed class CombatStyleRuleContext
{
    private CombatStyleRuleContext(
        CombatStyleRuntimeState state,
        CompiledEffect? effect,
        CompiledAbility? ability,
        RuntimeCombatant source,
        RuntimeCombatant target,
        decimal procCoefficient,
        int amount,
        Dictionary<AttributeType, float>? summonAttributes)
    {
        State = state;
        Effect = effect;
        Ability = ability;
        Source = source;
        Target = target;
        ProcCoefficient = procCoefficient;
        Amount = amount;
        SummonAttributes = summonAttributes;
    }

    public CombatStyleRuntimeState State { get; }
    public CompiledEffect? Effect { get; }
    public CompiledAbility? Ability { get; }
    public RuntimeCombatant Source { get; }
    public RuntimeCombatant Target { get; }
    public decimal ProcCoefficient { get; }
    public int Amount { get; set; }
    public decimal AdditivePercent { get; set; }
    public int BonusDamage { get; set; }
    public CombatStyleEventType EventType { get; set; }
    public Dictionary<AttributeType, float>? SummonAttributes { get; }

    public static CombatStyleRuleContext ForEffect(
        CombatStyleRuntimeState state,
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        decimal procCoefficient,
        int amount) =>
        new(state, effect, null, source, target, procCoefficient, amount, null);

    public static CombatStyleRuleContext ForAbility(
        CombatStyleRuntimeState state,
        CompiledAbility ability,
        RuntimeCombatant actor) =>
        new(state, null, ability, actor, actor, 1m, 0, null);

    public static CombatStyleRuleContext ForDamageEvent(
        CombatStyleRuntimeState state,
        CompiledEffect? effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        decimal procCoefficient) =>
        new(state, effect, null, source, target, procCoefficient, 0, null);

    public static CombatStyleRuleContext ForSummonAttributes(
        CombatStyleRuntimeState state,
        RuntimeCombatant owner,
        Dictionary<AttributeType, float> summonAttributes) =>
        new(state, null, null, owner, owner, 1m, 0, summonAttributes);
}

internal static class CombatStylePredicateEvaluator
{
    public static bool Matches(CombatStyleRuleContext context, EffectPredicate predicate)
    {
        if (predicate.SourceMustBePlayer == true && !IsPlayer(context.State, context.Source))
            return false;
        if (predicate.TargetMustBePlayer == true && !IsPlayer(context.State, context.Target))
            return false;
        if (predicate.SourceMustBeOwnedSummon == true && !IsOwnedSummon(context.State, context.Source))
            return false;
        if (predicate.TargetMustBeOwnedSummon == true && !IsOwnedSummon(context.State, context.Target))
            return false;
        if ((predicate.SourceHealthPercentAtOrBelow is not null
                || predicate.SourceHealthPercentAtOrBelowModifiers.Count > 0)
            && HealthPercent(context.Source) > CombatStyleValueEvaluator.Evaluate(
                context.State,
                predicate.SourceHealthPercentAtOrBelow ?? 0m,
                predicate.SourceHealthPercentAtOrBelowModifiers))
            return false;
        if ((predicate.TargetHealthPercentAtOrBelow is not null
                || predicate.TargetHealthPercentAtOrBelowModifiers.Count > 0)
            && HealthPercent(context.Target) > CombatStyleValueEvaluator.Evaluate(
                context.State,
                predicate.TargetHealthPercentAtOrBelow ?? 0m,
                predicate.TargetHealthPercentAtOrBelowModifiers))
            return false;

        if (context.Effect is not null)
            return MatchesEffect(predicate, context.Effect);

        if (context.Ability is not null)
            return MatchesAbility(predicate, context.Ability);

        return AllowsNonEffectPredicate(predicate);
    }

    private static bool MatchesEffect(EffectPredicate predicate, CompiledEffect effect)
    {
        if (predicate.ActiveAbilityOnly && effect.AbilityKind != AbilitySpecKind.Active)
            return false;
        if (predicate.PassiveAbilityOnly && effect.AbilityKind != AbilitySpecKind.Passive)
            return false;
        if (predicate.EffectOperations.Count > 0 && !predicate.EffectOperations.Contains(effect.Operation))
            return false;
        if (predicate.DamageTypes.Count > 0 && !predicate.DamageTypes.Contains(effect.DamageType))
            return false;
        if (predicate.AttackTypes.Count > 0 && !predicate.AttackTypes.Contains(effect.AttackType))
            return false;
        if (predicate.TargetSelectors.Count > 0 && !predicate.TargetSelectors.Contains(effect.Target))
            return false;
        if (predicate.RequiredTags.Count > 0 && predicate.RequiredTags.Any(tag => !HasAnyTag(effect, tag)))
            return false;
        if (predicate.AnyTags.Count > 0 && predicate.AnyTags.All(tag => !HasAnyTag(effect, tag)))
            return false;
        if (predicate.MultiTargetOnly && !IsMultiTargetEffect(effect))
            return false;
        if (predicate.AmplifiableEffectOnly && !IsAmplifiableEffect(effect))
            return false;
        if (predicate.HealOrBarrierOnly && !IsHealOrBarrierEffect(effect))
            return false;
        if (predicate.StatusOrDebuffOnly && !IsStatusOrDebuffEffect(effect))
            return false;
        if (predicate.RangedOnly && !IsRangedEffect(effect))
            return false;

        return true;
    }

    private static bool MatchesAbility(EffectPredicate predicate, CompiledAbility ability)
    {
        if (predicate.ActiveAbilityOnly && ability.Kind != AbilitySpecKind.Active)
            return false;
        if (predicate.PassiveAbilityOnly && ability.Kind != AbilitySpecKind.Passive)
            return false;
        if (predicate.EffectOperations.Count > 0 || predicate.DamageTypes.Count > 0 || predicate.AttackTypes.Count > 0)
            return false;
        if (predicate.TargetSelectors.Count > 0)
            return false;
        if (predicate.RequiredTags.Count > 0 && predicate.RequiredTags.Any(tag => !HasAnyTag(ability, tag)))
            return false;
        if (predicate.AnyTags.Count > 0 && predicate.AnyTags.All(tag => !HasAnyTag(ability, tag)))
            return false;
        if (predicate.MultiTargetOnly
            || predicate.AmplifiableEffectOnly
            || predicate.HealOrBarrierOnly
            || predicate.StatusOrDebuffOnly
            || predicate.RangedOnly)
        {
            return false;
        }

        return true;
    }

    private static bool AllowsNonEffectPredicate(EffectPredicate predicate) =>
        !predicate.ActiveAbilityOnly
        && !predicate.PassiveAbilityOnly
        && predicate.EffectOperations.Count == 0
        && predicate.DamageTypes.Count == 0
        && predicate.AttackTypes.Count == 0
        && predicate.TargetSelectors.Count == 0
        && predicate.RequiredTags.Count == 0
        && predicate.AnyTags.Count == 0
        && !predicate.MultiTargetOnly
        && !predicate.AmplifiableEffectOnly
        && !predicate.HealOrBarrierOnly
        && !predicate.StatusOrDebuffOnly
        && !predicate.RangedOnly;

    public static bool HasAnyTag(CompiledAbility ability, string tag) =>
        ability.Tags.Any(candidate => TagMatches(candidate, tag));

    public static bool HasAnyTag(CompiledEffect effect, string tag) =>
        effect.Tags.Any(candidate => TagMatches(candidate, tag)) ||
        effect.AbilityTags.Any(candidate => TagMatches(candidate, tag));

    public static bool TagMatches(string candidate, string tag) =>
        candidate.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
        candidate.EndsWith("." + tag, StringComparison.OrdinalIgnoreCase);

    public static bool IsPlayer(CombatStyleRuntimeState state, RuntimeCombatant combatant) =>
        combatant.Team == (state.AppliesToFriendlyTeam ? CombatTeam.Friendly : CombatTeam.Hostile)
        && !combatant.IsSummoned;

    public static bool IsOwnedSummon(CombatStyleRuntimeState state, RuntimeCombatant combatant) =>
        combatant.IsSummoned && combatant.SummonOwner is not null && IsPlayer(state, combatant.SummonOwner);

    public static bool IsAmplifiableEffect(CompiledEffect effect) =>
        effect.Operation is AbilityEffectOperation.Damage
            or AbilityEffectOperation.Heal
            or AbilityEffectOperation.GrantBarrier
            or AbilityEffectOperation.ModifyAttribute;

    public static bool IsRangedEffect(CompiledEffect effect) =>
        effect.AttackType == AttackType.Ranged || HasAnyTag(effect, "Ranged");

    public static bool IsMultiTargetEffect(CompiledEffect effect) =>
        effect.Target is AbilityTargetSelector.AllEnemies
            or AbilityTargetSelector.TwoEnemies
            or AbilityTargetSelector.EveryoneButSelf;

    public static bool IsHealOrBarrierEffect(CompiledEffect effect) =>
        effect.Operation is AbilityEffectOperation.Heal or AbilityEffectOperation.GrantBarrier;

    public static bool IsStatusOrDebuffEffect(CompiledEffect effect) =>
        effect.Operation is AbilityEffectOperation.ApplyStatus or AbilityEffectOperation.ModifyAttribute ||
        HasAnyTag(effect, "Debuff") ||
        HasAnyTag(effect, "Control") ||
        HasAnyTag(effect, "Curse");

    public static decimal HealthPercent(RuntimeCombatant combatant)
    {
        var maxHealth = combatant.GetAttribute(AttributeType.MaxHealth);
        return maxHealth <= 0 ? 0m : (decimal)(combatant.Health / maxHealth * 100f);
    }
}

internal sealed class CombatStyleOperationExecutor
{
    private readonly ICombatStyleDefinitionProvider _definitions;

    public CombatStyleOperationExecutor(ICombatStyleDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public void Execute(CombatStyleRuleContext context, StyleRuleOperation operation)
    {
        switch (operation)
        {
            case ModifyEffectAmountOperation op:
                context.AdditivePercent += ApplyProc(
                    CombatStyleValueEvaluator.Evaluate(context.State, op.AdditivePercent, op.AdditivePercentModifiers),
                    op.UsesProcCoefficient,
                    context.ProcCoefficient);
                break;
            case AddDamageReductionOperation op:
                context.AdditivePercent -= ApplyProc(
                    CombatStyleValueEvaluator.Evaluate(context.State, op.Percent, op.PercentModifiers),
                    op.UsesProcCoefficient,
                    context.ProcCoefficient);
                break;
            case AddBonusDamageFromStatOperation op:
                context.BonusDamage += CalculateBonusDamage(context, op);
                break;
            case GainStyleResourceOperation op:
                var resourceActor = context.EventType == CombatStyleEventType.DamageTaken
                    ? context.Target
                    : context.Source;
                AddResource(
                    context.State,
                    op.ResourceId,
                    ApplyProc(
                        CombatStyleValueEvaluator.Evaluate(context.State, op.Amount, op.AmountModifiers),
                        op.UsesProcCoefficient,
                        context.ProcCoefficient),
                    resourceActor);
                break;
            case SetPendingEmpowermentOperation op:
                AddPendingEmpowerment(context.State, op);
                break;
            case GrantBarrierFromMaxHealthOperation op:
                GrantBarrierFromMaxHealth(context, op);
                break;
            case ModifySummonStatsOperation op:
                ModifySummonAttributes(context, op);
                break;
        }
    }

    private static decimal ApplyProc(decimal value, bool usesProcCoefficient, decimal procCoefficient) =>
        usesProcCoefficient ? value * procCoefficient : value;

    private static int CalculateBonusDamage(CombatStyleRuleContext context, AddBonusDamageFromStatOperation operation)
    {
        if (context.Effect is null)
            return 0;

        var coefficient = CombatStyleValueEvaluator.Evaluate(
            context.State,
            operation.Coefficient,
            operation.CoefficientModifiers);

        if (operation.UsesProcCoefficient)
            coefficient *= context.Effect.ProcCoefficient;

        return (int)Math.Round((decimal)context.Source.GetAttribute(operation.Stat) * coefficient);
    }

    private static void AddPendingEmpowerment(
        CombatStyleRuntimeState state,
        SetPendingEmpowermentOperation operation)
    {
        var additivePercent = CombatStyleValueEvaluator.Evaluate(state, operation.AdditivePercent, operation.AdditivePercentModifiers);
        if (additivePercent <= 0)
            return;

        if (state.PendingEmpowerments.Any(x => x.Id.Equals(operation.EmpowermentId, StringComparison.OrdinalIgnoreCase)))
            return;

        state.PendingEmpowerments.Add(new PendingStyleEmpowerment(
            operation.EmpowermentId,
            operation.AppliesTo,
            additivePercent,
            operation.ConsumeOnUse));
    }

    private void AddResource(CombatStyleRuntimeState state, string resourceId, decimal amount, RuntimeCombatant actor)
    {
        if (amount <= 0)
            return;

        var definition = _definitions.GetById(state.StyleId);
        var current = state.Resources.GetValueOrDefault(resourceId);
        var max = definition?.ResourceMaxAmount > 0
            ? definition.ResourceMaxAmount
            : resourceId.Equals("arcaneCharge", StringComparison.OrdinalIgnoreCase) ? 5m : 100m;
        current = Math.Min(max, current + amount);
        state.Resources[resourceId] = current;

        if (current < max || definition is null)
            return;

        state.Resources[resourceId] = 0m;
        foreach (var operation in definition.ResourceOverflowOperations)
            Execute(CombatStyleRuleContext.ForDamageEvent(state, null, actor, actor, 1m), operation);
    }

    private static void GrantBarrierFromMaxHealth(
        CombatStyleRuleContext context,
        GrantBarrierFromMaxHealthOperation operation)
    {
        var percent = CombatStyleValueEvaluator.Evaluate(context.State, operation.Percent, operation.PercentModifiers);
        if (percent <= 0)
            return;

        if (operation.MaxTriggersPerEncounter is not null)
        {
            var maxTriggers = operation.MaxTriggersPerEncounter.Value
                + (int)Math.Round(CombatStyleValueEvaluator.Evaluate(context.State, 0m, operation.MaxTriggerModifiers));
            var triggerCount = context.State.TriggerCounts.GetValueOrDefault(operation.TriggerKey);
            if (triggerCount >= maxTriggers)
                return;

            context.State.TriggerCounts[operation.TriggerKey] = triggerCount + 1;
        }

        context.Target.AdjustBarrier(Math.Max(1, context.Target.GetAttribute(AttributeType.MaxHealth) * (float)percent));
    }

    private static void ModifySummonAttributes(
        CombatStyleRuleContext context,
        ModifySummonStatsOperation operation)
    {
        if (context.SummonAttributes is null)
            return;

        if (operation.MaxHealthPercent is not null || operation.MaxHealthPercentModifiers is not null)
        {
            var percent = CombatStyleValueEvaluator.Evaluate(
                context.State,
                operation.MaxHealthPercent ?? 0m,
                operation.MaxHealthPercentModifiers);
            Multiply(context.SummonAttributes, AttributeType.MaxHealth, percent);
        }

        if (operation.DamagePercent is not null || operation.DamagePercentModifiers is not null)
        {
            var percent = CombatStyleValueEvaluator.Evaluate(
                context.State,
                operation.DamagePercent ?? 0m,
                operation.DamagePercentModifiers);
            Multiply(context.SummonAttributes, AttributeType.Power, percent);
        }
    }

    private static void Multiply(Dictionary<AttributeType, float> attributes, AttributeType attribute, decimal percent)
    {
        if (percent == 0 || !attributes.TryGetValue(attribute, out var value))
            return;

        attributes[attribute] = value * (1f + (float)percent);
    }
}

internal static class CombatStyleValueEvaluator
{
    public static decimal Evaluate(
        CombatStyleRuntimeState state,
        decimal baseValue,
        IReadOnlyList<StyleValueModifier>? modifiers)
    {
        var total = baseValue;
        if (modifiers is null)
            return total;

        foreach (var modifier in modifiers)
        {
            if (state.StyleLevel < modifier.MinStyleLevel
                || (modifier.MaxStyleLevel is not null && state.StyleLevel > modifier.MaxStyleLevel.Value))
            {
                continue;
            }

            total += modifier.Type switch
            {
                "nodeRank" when modifier.NodeId is not null =>
                    state.NodeRanks.GetValueOrDefault(modifier.NodeId) * modifier.Value,
                "focusLevel" when modifier.FocusId is not null
                    && state.FocusId?.Equals(modifier.FocusId, StringComparison.OrdinalIgnoreCase) == true =>
                    modifier.Value,
                _ => 0m
            };
        }

        return total;
    }
}
