using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;

namespace Services.LL.Combat.Engine;

public sealed partial class FastCombatEngine
{
    private const string FortificationEffectId = "combat-style:bastion:fortification";
    private const string EntrenchedEffectId = "combat-style:bastion:entrenched";
    private readonly Dictionary<RuntimeCombatant, CombatStyleEncounterState> _combatStyles = [];
    private bool _tickConditionsBeforeActions;

    private void InitializeCombatStyle(RuntimeCombatant combatant)
    {
        if (combatant.CombatStyle is { } style && !combatant.IsSummoned)
        {
            _combatStyles[combatant] = new CombatStyleEncounterState(combatant, style);
            _tickConditionsBeforeActions |= style.Kind == CombatStyleKind.Reaper;
        }
    }

    private void ApplyCombatStyleOpening(RuntimeCombatant combatant, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!_combatStyles.TryGetValue(combatant, out var state) || state.OpeningApplied)
            return;
        state.OpeningApplied = true;
        var style = state.Configuration;
        if (style.Level < CombatStyleProgression.OpeningTechniqueLevel || !combatant.IsAlive)
            return;

        if (style.Kind == CombatStyleKind.Bastion && style.MilestoneTuning.OpeningBarrierFraction > 0)
        {
            var requested = (float)(combatant.GetAttribute(AttributeType.MaxHealth)
                * style.MilestoneTuning.OpeningBarrierFraction);
            var accepted = combatant.GrantBarrier(combatant, requested, ++_applicationOrder, EntrenchedEffectId);
            if (accepted > 0)
                Log(combatant, combatant, "Entrenched", EventType.RestoreBarrier, (int)Math.Round(accepted),
                    $"{combatant.Name} began battle with {accepted:0.##} Barrier.", "Combat Style: Entrenched");
            // Opening protection is not converted healing and cannot trigger Barrier-gain loops.
        }
        else if (style.Kind == CombatStyleKind.Conduit && style.MilestoneTuning.OpeningCharge > 0)
        {
            var gained = Math.Min(style.MilestoneTuning.OpeningCharge, Math.Max(0, style.Tuning.ChargeCap));
            state.Charge = gained;
            state.ChargeGenerated += gained;
            if (gained > 0)
                Log(combatant, combatant, "Primed Circuit", EventType.Buff, gained,
                    $"{combatant.Name} began battle with {gained} Charge.", "Combat Style: Primed Circuit");
        }
        else if (style.Kind == CombatStyleKind.Reaper && style.Tuning.Reaper is { } reaper)
        {
            var target = combatants.Where(x => x.IsAlive && !AreAbilityAllies(combatant, x))
                .OrderByDescending(x => x.GetAttribute(AttributeType.MaxHealth)).FirstOrDefault();
            if (target is not null)
                ApplyCondition(combatant, target, StandardConditionType.Poison, reaper.OpeningPoisonStacks,
                    0, combatants, "Combat Style: Grave Seed", false, false, 0, publishApplication: false);
        }
        else if (state.Duelist is { } duelist)
            duelist.FirstImpressionAvailable = true;
    }

    private CombatStyleCastContext? BeginCombatStyleCast(RuntimeCombatant actor, RuntimeAbility ability)
    {
        if (ability.OriginPlayerEssenceId is not { } origin
            || !_combatStyles.TryGetValue(actor, out var state))
            return null;

        var context = new CombatStyleCastContext(actor, ability, state);
        context.Duelist = BeginDuelistAction(actor);
        context.ReaperApplicationCutoff = _applicationOrder;
        var style = state.Configuration;
        var tuning = style.Tuning;
        var healthFraction = actor.Health / Math.Max(1, actor.GetAttribute(AttributeType.MaxHealth));
        if (style.Kind == CombatStyleKind.Bastion)
        {
            if (style.RefinementId == CombatStyleIds.Reprisal && HasImmediateEnemyDamage(ability.Definition))
            {
                // Keep the reservation in the bank so absorption during the cast shares its cap.
                // Only this captured amount can be spent; later gains belong to a later cast.
                context.ReprisalReservedDamage = ClampReprisalStoredDamage(state);
            }
            if (style.RefinementId == CombatStyleIds.Counterweight
                && actor.Barrier >= actor.GetAttribute(AttributeType.MaxHealth) * tuning.CounterweightBarrierThreshold
                && HasImmediateEnemyDamage(ability.Definition))
            {
                var spent = actor.ConsumeBarrier((float)(actor.GetAttribute(AttributeType.MaxHealth) * tuning.CounterweightBarrierCost));
                state.CounterweightBarrierSpent += spent;
                context.CounterweightBonus = Math.Max(0, (int)Math.Round(spent));
                Log(actor, actor, "Counterweight", EventType.Buff, (int)Math.Round(spent),
                    $"{actor.Name} spent {spent:0.##} Barrier on Counterweight.", "Combat Style: Counterweight");
            }
            return context;
        }

        if (style.Kind != CombatStyleKind.Conduit)
            return context;

        if (origin != style.ChanneledPlayerEssenceId)
        {
            // A contributor at capacity is still used in this cycle; overflow is never banked.
            var firstContribution = state.Contributors.Add(origin);
            if (!tuning.DistinctContributors || firstContribution)
            {
                var gained = Math.Min(1, Math.Max(0, tuning.ChargeCap - state.Charge));
                state.Charge += gained;
                state.ChargeGenerated += gained;
            }
            return context;
        }

        context.IsChanneledEssence = true;
        context.ChargeSpent = state.Charge;
        context.ChanneledMultiplier = tuning.ChanneledBaseMultiplier + tuning.ChanneledPerCharge * state.Charge;
        if (state.Charge > 0)
        {
            context.ChanneledMultiplier += style.ChanneledMasteryBonus;
            if (style.HasUpgrade(CombatStyleIds.FullCircuit)
                && (state.Charge == tuning.ChargeCap
                    || style.HasMasteredUpgrade(CombatStyleIds.FullCircuit)
                    && style.MilestoneTuning.FullCircuitMinimumCharge > 0
                    && state.Charge >= style.MilestoneTuning.FullCircuitMinimumCharge))
                context.ChanneledMultiplier += tuning.FullCircuitBonus;
            if (style.HasUpgrade(CombatStyleIds.PartialFlow)
                && (state.Charge == 1
                    || style.HasMasteredUpgrade(CombatStyleIds.PartialFlow)
                    && state.Charge <= style.MilestoneTuning.PartialFlowMaximumCharge))
                context.ChanneledMultiplier += tuning.PartialFlowBonus;
            var recoveryThreshold = style.HasMasteredUpgrade(CombatStyleIds.EmergencyChannel)
                ? Math.Max(tuning.EmergencyChannelHealthThreshold, style.MilestoneTuning.EmergencyChannelHealthThreshold)
                : tuning.EmergencyChannelHealthThreshold;
            if (style.HasUpgrade(CombatStyleIds.EmergencyChannel)
                && healthFraction <= recoveryThreshold + 1e-7)
                context.SelfRecoveryBonus = tuning.EmergencyChannelBonus;
        }

        state.ChanneledCastsByCharge[state.Charge] = state.ChanneledCastsByCharge.GetValueOrDefault(state.Charge) + 1;
        state.ChargeSpent += state.Charge;
        state.ChanneledMultiplierTotal += context.ChanneledMultiplier;
        state.Charge = 0;
        state.Contributors.Clear();
        // These captured content versions predate the terminology change. Keep their replay log bytes unchanged.
        var channeledEssenceName = style.ContentVersion is "combat-styles.v1" or "combat-styles.v2"
            or "combat-styles.v3" or "combat-styles.v4" or "combat-styles.v5"
            ? "Focus" : "Channeled Essence";
        Log(actor, actor, "Circuit", EventType.Buff, context.ChargeSpent,
            $"{actor.Name}'s {channeledEssenceName} spent {context.ChargeSpent} Charge for {context.ChanneledMultiplier:P0} effect amounts.",
            "Combat Style: Circuit");
        return context;
    }

    private static bool HasImmediateEnemyDamage(CompiledAbility ability) =>
        ability.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers)
        && triggers.Any(trigger => trigger.Effects.Any(effect =>
            IsImmediateChanneledEssenceComponent(effect)
            && effect.Operation == AbilityEffectOperation.Damage
            && effect.Target is not (AbilityTargetSelector.Self or AbilityTargetSelector.AllAllies
                or AbilityTargetSelector.LowestHealthAlly or AbilityTargetSelector.HighestMaxHealthAlly
                or AbilityTargetSelector.Source or AbilityTargetSelector.TwoAllies
                or AbilityTargetSelector.RandomAlly or AbilityTargetSelector.SummonedAllies
                or AbilityTargetSelector.NonSummonedAllies or AbilityTargetSelector.OwnedSummons
                or AbilityTargetSelector.HighestCurrentHealthOwnedSummon)));

    public static bool HasEligibleChanneledEssenceComponent(CompiledAbility ability) =>
        ability.Kind == AbilitySpecKind.Active
        && ability.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers)
        && triggers.Any(trigger => trigger.Effects.Any(IsImmediateChanneledEssenceComponent));

    public static bool IsImmediateChanneledEssenceComponent(CompiledEffect effect) =>
        (effect.Operation is AbilityEffectOperation.Damage or AbilityEffectOperation.Heal or AbilityEffectOperation.GrantBarrier
         || effect.Operation == AbilityEffectOperation.RestoreResource
             && effect.Resource is AbilityResourceType.Health or AbilityResourceType.Barrier)
        && !IsPeriodicEffect(effect)
        && effect.AttackType != AttackType.DamageOverTime
        && !effect.Tags.Contains("Damage.Secondary")
        // Recovery derived from an already resolved amount must not receive a second multiplier.
        && effect.EventMagnitudeCoefficient == 0;

    private void CompleteCombatStyleCast(CombatStyleCastContext? context, IReadOnlyList<RuntimeCombatant> combatants)
    {
        CompleteDuelistAction(context?.Duelist, combatants);
        if (context is not { IsChanneledEssence: true })
            return;
        var tuning = context.State.Configuration.Tuning;
        if (tuning.RelayChargeReturn <= 0 || context.ChargeSpent < tuning.RelayMinimumSpent)
            return;
        var returned = Math.Min(tuning.RelayChargeReturn, Math.Max(0, tuning.ChargeCap - context.State.Charge));
        context.State.Charge += returned;
        context.State.RelayChargeReturned += returned;
    }

    private static int ApplyChanneledEssenceAmount(
        CombatStyleCastContext? context, CompiledEffect effect, RuntimeCombatant source,
        RuntimeCombatant target, int value)
    {
        if (context is not { IsChanneledEssence: true } || !ReferenceEquals(context.Actor, source)
            || !IsImmediateChanneledEssenceComponent(effect))
            return value;

        var multiplier = context.ChanneledMultiplier;
        if (ReferenceEquals(source, target)
            && (effect.Operation is AbilityEffectOperation.Heal or AbilityEffectOperation.GrantBarrier
                || effect.Operation == AbilityEffectOperation.RestoreResource))
            multiplier += context.SelfRecoveryBonus;
        var modified = Math.Max(0, (int)Math.Round(value * multiplier));
        context.State.ChanneledOutputAdded += Math.Max(0, modified - value);
        context.State.ChanneledOutputLost += Math.Max(0, value - modified);
        return modified;
    }

    private static CombatStyleDamageBonus TakeCombatStyleDamageBonus(CombatStyleCastContext? context, CompiledEffect effect,
        RuntimeCombatant source, RuntimeCombatant target)
    {
        if (context is null
            || !ReferenceEquals(context.Actor, source) || !IsImmediateChanneledEssenceComponent(effect)
            || effect.Operation != AbilityEffectOperation.Damage)
            return default;
        if (context.CounterweightBonus > 0 && source.Team != target.Team)
        {
            var bonus = context.CounterweightBonus;
            context.CounterweightBonus = 0; // Preserve historical Counterweight attempt semantics.
            return new CombatStyleDamageBonus(bonus, CombatStyleDamageBonusKind.Counterweight);
        }
        if (context.ReprisalReservedDamage <= 0 || !target.IsAlive || AreAbilityAllies(source, target))
            return default;

        var available = Math.Min(context.ReprisalReservedDamage, ClampReprisalStoredDamage(context.State));
        var reprisalBonus = (int)Math.Min(int.MaxValue, Math.Floor(available));
        context.ReprisalReservedDamage = 0; // The first attempt consumes the whole-number bonus, even on a miss.
        context.State.ReprisalStoredDamage -= reprisalBonus;
        // Fractions stay banked. If no eligible attempt occurs, the untouched reservation stays banked too.
        return new CombatStyleDamageBonus(reprisalBonus, CombatStyleDamageBonusKind.Reprisal);
    }

    private static double ClampReprisalStoredDamage(CombatStyleEncounterState state)
    {
        var cap = Math.Max(0, state.Owner.GetAttribute(AttributeType.MaxHealth))
            * Math.Max(0, state.Configuration.Tuning.ReprisalMaxHealthCapFraction.GetValueOrDefault());
        state.ReprisalStoredDamage = Math.Clamp(state.ReprisalStoredDamage, 0, cap);
        return state.ReprisalStoredDamage;
    }

    private void RecordReprisalBarrierAbsorbed(RuntimeCombatant source, RuntimeCombatant target, float absorbed)
    {
        if (absorbed <= 0 || AreAbilityAllies(source, target)
            || !_combatStyles.TryGetValue(target, out var state)
            || state.Configuration.Kind != CombatStyleKind.Bastion
            || state.Configuration.RefinementId != CombatStyleIds.Reprisal)
            return;
        var fraction = state.Configuration.Tuning.ReprisalAbsorbedDamageFraction.GetValueOrDefault();
        if (fraction <= 0)
            return;
        state.ReprisalStoredDamage += absorbed * fraction;
        ClampReprisalStoredDamage(state);
    }

    private static bool IsBastionRecoveryRecipient(RuntimeCombatant target) =>
        target.CombatStyle?.Kind == CombatStyleKind.Bastion
        && !target.IsSummoned;

    private static bool IsBastionSelfRecovery(RuntimeCombatant source, RuntimeCombatant target) =>
        IsBastionRecoveryRecipient(target)
        && (ReferenceEquals(source, target) || ReferenceEquals(source.SummonOwner, target));

    private RecoveryAllocation AllocateCombatStyleRecovery(RuntimeCombatant target,
        float healing, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!IsBastionRecoveryRecipient(target))
            return new(healing, 0, null, false);
        var style = target.CombatStyle!;
        var tuning = style.Tuning;
        var healthFraction = target.Health / Math.Max(1, target.GetAttribute(AttributeType.MaxHealth));
        var rebuild = style.RefinementId == CombatStyleIds.Rebuild
            && healthFraction <= tuning.RebuildHealthThreshold + 1e-7;

        var health = rebuild ? healing : healing * tuning.HealthFraction;
        if (!rebuild && style.HasUpgrade(CombatStyleIds.MeasuredRecovery))
            health *= 1 + tuning.MeasuredRecoveryHealthBonus;
        if (style.HasMasteredUpgrade(CombatStyleIds.HoldTheBreach) && target.Barrier <= 0)
            health *= 1 + style.MilestoneTuning.HoldTheBreachHealthBonus;
        var barrierBonus = style.BarrierMasteryBonus;
        if (style.HasUpgrade(CombatStyleIds.PreparedWall)
            && (healthFraction + 1e-7 >= tuning.PreparedWallHealthThreshold
                || style.HasMasteredUpgrade(CombatStyleIds.PreparedWall)
                && style.MilestoneTuning.PreparedWallEmptyBarrier && target.Barrier <= 0))
            barrierBonus += tuning.PreparedWallBarrierBonus;
        if (style.HasUpgrade(CombatStyleIds.HoldTheBreach) && target.Barrier <= 0)
            barrierBonus += tuning.HoldTheBreachBarrierBonus;

        RuntimeCombatant? ally = null;
        if (style.RefinementId == CombatStyleIds.Shelter)
        {
            var lowest = double.MaxValue;
            foreach (var candidate in combatants)
            {
                if (!candidate.IsAlive || ReferenceEquals(candidate, target) || !AreAbilityAllies(target, candidate))
                    continue;
                var fraction = candidate.Health / Math.Max(1, candidate.GetAttribute(AttributeType.MaxHealth));
                if (fraction < lowest)
                {
                    ally = candidate;
                    lowest = fraction;
                }
            }
        }
        return new((float)health, rebuild ? 0 : (float)(healing * tuning.BarrierFraction * (1 + barrierBonus)), ally, !rebuild);
    }

    private float ApplyCombatStyleRecovery(RuntimeCombatant target, float healing,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (IsBastionRecoveryRecipient(target) && !target.IsAlive)
            return 0;
        var allocation = AllocateCombatStyleRecovery(target, healing, combatants);
        var before = target.Health;
        target.AdjustHealth(allocation.Health);
        var restored = Math.Max(0, target.Health - before);
        if (!IsBastionRecoveryRecipient(target) || !_combatStyles.TryGetValue(target, out var state))
            return restored;
        state.HealthRestored += restored;
        var excessHealth = Math.Max(0, allocation.Health - restored);
        var recoveredExcess = state.Configuration.HasMasteredUpgrade(CombatStyleIds.MeasuredRecovery)
            ? excessHealth * state.Configuration.MilestoneTuning.MeasuredRecoveryOverhealBarrierFraction
            : 0;
        state.HealthRecoveryWasted += Math.Max(0, excessHealth - recoveredExcess);
        if (allocation.Converted)
            state.HealingConverted += healing;
        var barrier = allocation.Barrier + (float)recoveredExcess;
        if (barrier <= 0)
            return restored;

        if (allocation.Ally is { } ally)
        {
            var ownerShare = (float)(barrier * state.Configuration.Tuning.ShelterOwnerShare);
            GrantFortificationBarrier(target, target, ownerShare, state);
            var granted = GrantFortificationBarrier(target, ally, barrier - ownerShare, state);
            state.ShelterRecipients[ally.Id] = state.ShelterRecipients.GetValueOrDefault(ally.Id) + granted;
        }
        else
            GrantFortificationBarrier(target, target, barrier, state);
        return restored;
    }

    private float GrantFortificationBarrier(RuntimeCombatant owner, RuntimeCombatant target, float amount,
        CombatStyleEncounterState state)
    {
        var accepted = target.GrantBarrier(owner, amount, ++_applicationOrder, FortificationEffectId);
        state.ConvertedBarrierGranted += accepted;
        state.BarrierOverflow += Math.Max(0, amount - accepted);
        if (accepted > 0)
            Log(owner, target, "Fortification", EventType.RestoreBarrier, (int)Math.Round(accepted),
                $"{owner.Name}'s Fortification granted {accepted:0.##} Barrier to {target.Name}.", "Combat Style: Fortification");
        // Deliberately no OnBarrierApplied event: converted healing cannot feed gain/heal loops.
        return accepted;
    }

    private bool IsCombatStyleRecoveryUseful(CompiledEffect effect, RuntimeCombatant source, RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants, CombatEvent combatEvent)
    {
        var bastionBuild = source.CombatStyle?.Kind == CombatStyleKind.Bastion
            || source.SummonOwner?.CombatStyle?.Kind == CombatStyleKind.Bastion;
        if (!bastionBuild || IsPeriodicEffect(effect))
            return true;
        if (effect.Operation == AbilityEffectOperation.GrantBarrier
            || effect.Operation == AbilityEffectOperation.RestoreResource && effect.Resource == AbilityResourceType.Barrier)
            return HasBarrierCapacity(target);
        if (!(effect.Operation == AbilityEffectOperation.Heal
                || effect.Operation == AbilityEffectOperation.RestoreResource && effect.Resource == AbilityResourceType.Health))
            return true;
        var value = CalculateValue(effect, source, target, combatants, combatEvent);
        var modified = ApplyHealingReceivedModifier(target,
            (float)(value * Math.Max(0, 1 + source.GetAttribute(AttributeType.HealingPowerPercent) / 100f)));
        return IsModifiedRecoveryUseful(source, target, modified, combatants);
    }

    private bool IsModifiedRecoveryUseful(RuntimeCombatant source, RuntimeCombatant target, float healing,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        // Receiving Bastion's conversion does not change another combatant's healing decisions.
        if (!IsBastionSelfRecovery(source, target))
            return healing > 0 && target.Health < target.GetAttribute(AttributeType.MaxHealth);
        var allocation = AllocateCombatStyleRecovery(target, healing, combatants);
        var barrier = allocation.Barrier;
        if (target.CombatStyle!.HasMasteredUpgrade(CombatStyleIds.MeasuredRecovery))
        {
            var missingHealth = Math.Max(0, target.GetAttribute(AttributeType.MaxHealth) - target.Health);
            barrier += (float)(Math.Max(0, allocation.Health - missingHealth)
                * target.CombatStyle.MilestoneTuning.MeasuredRecoveryOverhealBarrierFraction);
        }
        return allocation.Health > 0 && target.Health < target.GetAttribute(AttributeType.MaxHealth)
               || barrier > 0 && (HasBarrierCapacity(target)
                   || allocation.Ally is { } ally && HasBarrierCapacity(ally));
    }

    private static bool HasBarrierCapacity(RuntimeCombatant target) =>
        target.Barrier < Math.Max(0, target.GetAttribute(AttributeType.MaxHealth) * 2.5f);

    private IReadOnlyList<CombatStyleCombatSummary> CreateCombatStyleSummaries() =>
        _combatStyles.Values.Select(state => new CombatStyleCombatSummary
        {
            EntityId = state.Owner.Id,
            CombatStyleId = state.Configuration.CombatStyleId,
            Level = state.Configuration.Level,
            RefinementId = state.Configuration.RefinementId,
            HealingConverted = state.HealingConverted,
            HealthRestored = state.HealthRestored,
            HealthRecoveryWasted = state.HealthRecoveryWasted,
            ConvertedBarrierGranted = state.ConvertedBarrierGranted,
            ConvertedBarrierAbsorbed = state.ConvertedBarrierAbsorbed,
            BarrierOverflow = state.BarrierOverflow,
            CounterweightBarrierSpent = state.CounterweightBarrierSpent,
            CounterweightDamage = state.CounterweightDamage,
            ReprisalStoredDamage = state.ReprisalStoredDamage,
            ReprisalDamage = state.ReprisalDamage,
            ShelterRecipients = new Dictionary<string, double>(state.ShelterRecipients),
            Charge = state.Charge,
            ChargeGenerated = state.ChargeGenerated,
            ChargeSpent = state.ChargeSpent,
            RelayChargeReturned = state.RelayChargeReturned,
            Contributors = state.Contributors.Order().ToArray(),
            ChanneledCastsByCharge = new Dictionary<int, int>(state.ChanneledCastsByCharge),
            ChanneledMultiplierTotal = state.ChanneledMultiplierTotal,
            ChanneledOutputAdded = state.ChanneledOutputAdded,
            ChanneledOutputLost = state.ChanneledOutputLost
        }).ToArray();

    private readonly record struct RecoveryAllocation(float Health, float Barrier, RuntimeCombatant? Ally, bool Converted);
    private enum CombatStyleDamageBonusKind { Counterweight, Reprisal }
    private readonly record struct CombatStyleDamageBonus(int Amount, CombatStyleDamageBonusKind Kind);

    private sealed class CombatStyleCastContext(RuntimeCombatant actor, RuntimeAbility ability, CombatStyleEncounterState state)
    {
        public DuelistActionContext? Duelist { get; set; }
        public long ReaperApplicationCutoff { get; set; }
        public bool ReaperHitDealtDamage { get; set; }
        public RuntimeCombatant Actor { get; } = actor;
        public RuntimeAbility Ability { get; } = ability;
        public CombatStyleEncounterState State { get; } = state;
        public int CounterweightBonus { get; set; }
        public double ReprisalReservedDamage { get; set; }
        public bool IsChanneledEssence { get; set; }
        public int ChargeSpent { get; set; }
        public double ChanneledMultiplier { get; set; } = 1;
        public double SelfRecoveryBonus { get; set; }
    }

    private sealed class CombatStyleEncounterState(RuntimeCombatant owner, CombatStyleSnapshot configuration)
    {
        public DuelistEncounterState? Duelist { get; } = configuration.Kind == CombatStyleKind.Duelist
            && configuration.Tuning.Duelist is not null ? new() : null;
        public RuntimeCombatant Owner { get; } = owner;
        public CombatStyleSnapshot Configuration { get; } = configuration;
        public bool OpeningApplied { get; set; }
        public int Charge { get; set; }
        public HashSet<Guid> Contributors { get; } = [];
        public int ChargeGenerated { get; set; }
        public int ChargeSpent { get; set; }
        public int RelayChargeReturned { get; set; }
        public Dictionary<int, int> ChanneledCastsByCharge { get; } = [];
        public double ChanneledMultiplierTotal { get; set; }
        public double ChanneledOutputAdded { get; set; }
        public double ChanneledOutputLost { get; set; }
        public double HealingConverted { get; set; }
        public double HealthRestored { get; set; }
        public double HealthRecoveryWasted { get; set; }
        public double ConvertedBarrierGranted { get; set; }
        public double ConvertedBarrierAbsorbed { get; set; }
        public double BarrierOverflow { get; set; }
        public double CounterweightBarrierSpent { get; set; }
        public double CounterweightDamage { get; set; }
        public double ReprisalStoredDamage { get; set; }
        public double ReprisalDamage { get; set; }
        public Dictionary<string, double> ShelterRecipients { get; } = new(StringComparer.Ordinal);
    }
}
