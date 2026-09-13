using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record BossStaggerRoute(string EvidenceKey, AbilitySpec? Ability, AbilityEffectSpec? Effect,
    IReadOnlyList<AbilityTriggerSpec> SelectedTriggers, bool ImplicitTrigger, string? Limitation)
{
    // Descriptive runtime gate only; never used to weight nominal authored power.
    public int? SeparateRuntimeControlChancePercent => Effect is { GuaranteedConditionApplication: false } ? 80 : null;
}
public sealed record BossStaggerProvider(string EssenceId, IReadOnlyList<string> EvidenceKeys,
    IReadOnlyList<BossStaggerRoute> Routes, int? StaggerPower, int? MinimumCount, string? FallbackReason);
public sealed record BossStaggerReservations(BossStaggerDefinition? Definition, int? FirstThreshold,
    IReadOnlyList<BossStaggerProvider> Providers);
public sealed record BossCoverageReservation(string Kind, string EssenceId, IReadOnlyList<string> EvidenceKeys,
    int? FirstThreshold, int? StaggerPower, int? MinimumCount, int Requested, int Satisfied, string? FallbackReason);

/// <summary>One offered direct application per requested carrier, without timing or probability predictions.</summary>
public static class TowerStaggerReservation
{
    public static BossStaggerReservations Create(BossDiscoveryInputs input, TowerBossInventoryReport inventory,
        IReadOnlyList<BossCoverageFeature> coverage)
    {
        var definition = inventory.Bosses.Single(b => b.FloorNumber == input.Floor).Definition.Stagger;
        var threshold = definition is { Enabled: true } ? definition.CalculateThreshold(input.RequiredPartySize, 0) : (int?)null;
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var essences = inventory.Essences.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var providers = coverage.Where(f => f.Kind == "recurring-control").OrderBy(f => f.EssenceId, StringComparer.Ordinal).Select(f => {
            var routes = f.EvidenceKeys.Where(k => k.StartsWith("Effect:", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).Select(key => {
                    AbilitySpec? ability = null; AbilityEffectSpec? effect = null; AbilityTriggerSpec[] triggers = []; string? limitation = null;
                    var split = key.LastIndexOf('/'); var abilityKey = split > 7 ? key[7..split] : "";
                    if (!nodes.TryGetValue(key, out var effectNode) || effectNode.Kind != TowerMechanicNodeKind.Effect
                        || !nodes.TryGetValue(abilityKey, out var abilityNode) || abilityNode.Kind != TowerMechanicNodeKind.Ability)
                        limitation = "missing-or-nested-definition";
                    else
                    {
                        ability = abilityNode.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
                        effect = effectNode.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
                        var selected = ability.Triggers.Select((t, i) => (Trigger: t, Index: i))
                            .Where(t => t.Trigger.EffectIds.Count == 0 || t.Trigger.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase)).ToArray();
                        triggers = ability.Triggers.Count == 0
                            ? [new() { Event = ability.Kind == AbilitySpecKind.Active ? AbilityTriggerEvent.OnAbilityUsed : AbilityTriggerEvent.OnCombatStart }]
                            : selected.Select(t => t.Trigger).ToArray();
                        if (!essences.TryGetValue(f.EssenceId, out var essence) || !essence.AbilityIds.Contains(ability.Id, StringComparer.Ordinal)
                            || !ability.Effects.Any(e => HarnessJson.Hash(e) == HarnessJson.Hash(effect))) limitation = "not-direct-equipped-route";
                        else if (abilityNode.Unknowns.Count > 0 || effectNode.Unknowns.Count > 0
                            || selected.Any(t => !nodes.TryGetValue("Trigger:" + abilityKey + "/" + t.Index, out var node)
                                || node.Kind != TowerMechanicNodeKind.Trigger || node.Unknowns.Count > 0)) limitation = "unknown-definition";
                        else if (effect.Operation != AbilityEffectOperation.ApplyCondition || effect.Condition is not (StandardConditionType.Stun or StandardConditionType.Freeze)
                            || effect.ChancePercent <= 0 || effect.StaggerPower <= 0) limitation = "unsupported-control-power";
                        else if (triggers.Length == 0) limitation = "no-selected-trigger";
                    }
                    return new BossStaggerRoute(key, ability, effect, triggers, ability?.Triggers.Count == 0, limitation);
                }).ToArray();
            return Derive(f.EssenceId, f.EvidenceKeys.Order(StringComparer.Ordinal).ToArray(), routes, definition, threshold, input.RequiredPartySize);
        }).ToArray();
        // Do not retain mutable authored objects owned by the caller.
        return JsonSerializer.Deserialize<BossStaggerReservations>(JsonSerializer.Serialize(new BossStaggerReservations(definition, threshold, providers), HarnessJson.Options), HarnessJson.Options)!;
    }

    internal static BossStaggerProvider Derive(string essenceId, IReadOnlyList<string> evidence, IReadOnlyList<BossStaggerRoute> routes,
        BossStaggerDefinition? definition, int? threshold, int partySize)
    {
        var reason = routes.Count != 1 ? "requires-one-direct-route" : routes[0].Limitation;
        var power = reason is null ? routes[0].Effect?.StaggerPower : null;
        if (reason is null && power is not > 0) reason = "unsupported-control-power";
        var minimum = power is > 0 && threshold is > 0 ? (int)(((long)threshold.Value + power.Value - 1) / power.Value) : (int?)null;
        reason = definition is null ? "absent-stagger" : !definition.Enabled ? "disabled-stagger"
            : definition.MaximumBreaks is <= 0 ? "no-first-break" : reason ?? (minimum > partySize ? "unattainable-minimum" : null);
        return new(essenceId, evidence, routes, power, minimum, reason);
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private void ValidateStaggerReservations()
    {
        var m = mechanics.StaggerReservations;
        if (input.Generation.PolicyVersion is not (TowerBossGeneration.StaggerReservationVersion or TowerBossGeneration.LoadoutDiversityVersion))
        {
            if (m is not null) throw new InvalidDataException("Stagger reservations require the opt-in policy.");
            return;
        }
        var expected = mechanics.CompatibleDefense!.Coverage.Where(f => f.Kind == "recurring-control").OrderBy(f => f.EssenceId, StringComparer.Ordinal).ToArray();
        if (m is null || m.Providers is null || m.FirstThreshold != (m.Definition is { Enabled: true } ? m.Definition.CalculateThreshold(input.RequiredPartySize, 0) : (int?)null)
            || !m.Providers.Select(p => p?.EssenceId).SequenceEqual(expected.Select(f => f.EssenceId)))
            throw new InvalidDataException("Stagger reservations must cover the unchanged control providers and first threshold.");
        foreach (var p in m.Providers)
        {
            var f = expected.Single(f => f.EssenceId == p.EssenceId);
            if (p.EvidenceKeys is null || p.Routes is null || !p.EvidenceKeys.SequenceEqual(f.EvidenceKeys.Order(StringComparer.Ordinal))
                || !p.Routes.Select(r => r?.EvidenceKey).SequenceEqual(p.EvidenceKeys.Where(k => k.StartsWith("Effect:", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal))
                || p.Routes.Any(r => r.SelectedTriggers is null || r.Limitation is null && (r.Ability is null || r.Effect is null
                    || r.SelectedTriggers.Count == 0 || r.Effect.Operation != AbilityEffectOperation.ApplyCondition
                    || r.Effect.Condition is not (StandardConditionType.Stun or StandardConditionType.Freeze) || r.Effect.ChancePercent <= 0 || r.Effect.StaggerPower <= 0))
                || HarnessJson.Hash(p) != HarnessJson.Hash(TowerStaggerReservation.Derive(p.EssenceId, p.EvidenceKeys, p.Routes, m.Definition, m.FirstThreshold, input.RequiredPartySize)))
                throw new InvalidDataException("Invalid source-derived stagger reservation metadata.");
        }
    }
}
