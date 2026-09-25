using System.Text.Json;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record TowerAlliedActionProvider(string EssenceId, string AbilityNodeKey, string EffectNodeKey,
    string Operation, string Target, string EffectDefinitionHash);
public sealed record TowerAlliedActionProtectionReport(string Version, string InventoryHash,
    IReadOnlyList<TowerAlliedActionProvider> Providers, string Interpretation);

/// <summary>Retains equipped providers of directly authored allied Basic Attacks.
/// Conditional effects still qualify; this is structural protection, not an uptime claim.</summary>
public static class TowerAlliedActionProtection
{
    public const string Version = "tower-allied-basic-attack-protection-v1";

    public static TowerAlliedActionProtectionReport Create(TowerBossInventoryReport inventory)
    {
        // Reuse the source-bound inventory checks, including typed effect consistency.
        _ = TowerDamageSourceAffinities.Create(inventory);
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var providers = new List<TowerAlliedActionProvider>();
        foreach (var essence in inventory.Essences.OrderBy(e => e.Id, StringComparer.Ordinal))
        foreach (var abilityId in essence.AbilityIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var key = "Ability:" + abilityId;
            var node = nodes[key];
            var ability = node.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
            if (node.Id != abilityId) throw new InvalidDataException("Changed allied-action root identity.");
            // AbilityIds declares the equipped roots. Variants may share a base
            // passive, so OwningEssenceId is not used as combatant ownership.
            foreach (var effect in ability.Effects)
            {
                var effectKey = "Effect:" + key + "/" + effect.Id;
                if (!nodes.TryGetValue(effectKey, out var effectNode)
                    || effectNode.Kind != TowerMechanicNodeKind.Effect || effectNode.Unknowns.Count != 0
                    || HarnessJson.Hash(effectNode.Definition) != HarnessJson.Hash(effect))
                    throw new InvalidDataException("Missing, unresolved or inconsistent allied-action effect: " + effectKey);
                if (effect.Operation == AbilityEffectOperation.PerformBasicAttack
                    && effect.Target == AbilityTargetSelector.NonSummonedAllies)
                    providers.Add(new(essence.Id, key, effectKey, effect.Operation.ToString(),
                        effect.Target.ToString(), HarnessJson.Hash(effect)));
            }
        }
        return new(Version, HarnessJson.Hash(inventory), providers.Distinct().OrderBy(p => p.EssenceId, StringComparer.Ordinal)
            .ThenBy(p => p.AbilityNodeKey, StringComparer.Ordinal).ThenBy(p => p.EffectNodeKey, StringComparer.Ordinal).ToArray(),
            "DirectEquippedAbilityEffectsOnlyConditionalAuthoredCapabilityNotMeasuredCombatValue");
    }
}
