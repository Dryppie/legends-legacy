using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;

namespace Services.LL.Combat.Engine;

public sealed partial class FastCombatEngine
{
    private void ResolveReaperHarvest(CombatStyleCastContext? context, CompiledEffect effect,
        RuntimeCombatant source, RuntimeCombatant target, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (context is not { ReaperHitDealtDamage: true }
            || context.State.Configuration.Kind != CombatStyleKind.Reaper
            || context.State.Configuration.Tuning.Reaper is not { } tuning
            || !ReferenceEquals(context.Actor, source) || !source.IsAlive || !target.IsAlive
            || AreAbilityAllies(source, target) || IsPeriodicEffect(effect)
            || effect.AttackType == AttackType.DamageOverTime || effect.Tags.Contains("Damage.Secondary")
            || effect.EventMagnitudeCoefficient != 0)
            return;

        var style = context.State.Configuration;
        var lastRites = style.RefinementId == CombatStyleIds.LastRites;
        var healthFraction = target.Health / Math.Max(1, target.GetAttribute(AttributeType.MaxHealth));
        if (lastRites && healthFraction > tuning.LastRitesHealthThreshold + 1e-7)
            return;

        var selected = target.Conditions.Where(x => ReferenceEquals(x.Source, source)
            && x.ApplicationOrder <= context.ReaperApplicationCutoff
            && x.Type is StandardConditionType.Bleed or StandardConditionType.Burn or StandardConditionType.Poison
            && x.UnpaidFutureTicks > 0).ToArray();
        if (selected.Length == 0)
            return;

        var amounts = new double[3];
        long stacks = 0;
        var retainedStacks = 0;
        // Commit every selected tick before any removal reaction or payout can run.
        foreach (var condition in selected)
        {
            var consumed = condition.ConsumeFutureTicks(lastRites ? condition.UnpaidFutureTicks : 1);
            var family = condition.Type == StandardConditionType.Bleed ? 0
                : condition.Type == StandardConditionType.Burn ? 1 : 2;
            amounts[family] += condition.PowerSnapshot * .01d * condition.Value * consumed;
            stacks += condition.Value;
            if (condition.UnpaidFutureTicks > 0)
                retainedStacks++;
        }

        var multiplier = tuning.Multiplier(style.Level, style.RefinementId);
        var closingThreshold = style.HasMasteredUpgrade(CombatStyleIds.ClosingHand)
            ? tuning.MasteredClosingHandHealthThreshold : tuning.ClosingHandHealthThreshold;
        if (style.HasUpgrade(CombatStyleIds.ClosingHand) && healthFraction <= closingThreshold + 1e-7)
            multiplier += tuning.UpgradeBonus;
        if (style.HasUpgrade(CombatStyleIds.Crosscut)
            && (selected.Select(x => x.Type).Distinct().Count() >= 2
                || style.HasMasteredUpgrade(CombatStyleIds.Crosscut) && stacks >= 3))
            multiplier += tuning.UpgradeBonus;
        if (style.HasUpgrade(CombatStyleIds.DeepRoots)
            && (retainedStacks == selected.Length
                || style.HasMasteredUpgrade(CombatStyleIds.DeepRoots) && retainedStacks > 0))
            multiplier += tuning.UpgradeBonus;

        foreach (var condition in selected)
            if (condition.UnpaidFutureTicks == 0)
                RemoveCondition(source, target, condition, ConditionRemovalReason.Consumed, combatants);

        if (!source.IsAlive || !target.IsAlive)
            return;

        var amount = amounts.Sum() * multiplier;
        if (style.RefinementId == CombatStyleIds.SoulSiphon)
        {
            var healing = ApplyHealingReceivedModifier(source, Math.Max(0, (int)Math.Round(amount
                * Math.Max(0, 1 + source.GetAttribute(AttributeType.HealingPowerPercent) / 100d))));
            var restored = ApplyCombatStyleRecovery(source, healing, combatants);
            context.State.HealthRestored += restored;
            context.State.HealthRecoveryWasted += Math.Max(0, healing - restored);
            Log(source, source, "Soul Siphon", EventType.Heal, (int)Math.Round(restored),
                $"{source.Name}'s Soul Siphon restored {restored:0.##} Health.", "Combat Style: Soul Siphon");
            if (restored > 0)
            {
                var restoredHealth = (int)Math.Round(restored);
                PublishIfObserved(AbilityTriggerEvent.OnHeal, source, source, null, combatants, restoredHealth);
                PublishIfObserved(AbilityTriggerEvent.OnHealed, source, source, null, combatants, restoredHealth);
                PublishIfObserved(AbilityTriggerEvent.OnEnemyHealed, source, source, null, combatants, restoredHealth);
                PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, source, source, null, combatants, restoredHealth);
            }
        }
        else if (style.RefinementId == CombatStyleIds.DeathSentence)
        {
            ApplyCondition(source, target, StandardConditionType.Doom, 1, 0, combatants,
                "Combat Style: Death Sentence", false, false, 0,
                publishApplication: false, storedDamage: amount);
        }
        else
        {
            var name = lastRites ? "Last Rites" : "Harvest";
            var types = new[] { DamageType.Bleed, DamageType.Burn, DamageType.Poison };
            for (var family = 0; family < amounts.Length; family++)
                ApplyDamage(source, target, Math.Max(0, (int)Math.Round(amounts[family] * multiplier)),
                    AttackType.None, types[family], null, combatants, name, $"Combat Style: {name}",
                    delivery: DamageDelivery.Stored);
        }
    }
}
