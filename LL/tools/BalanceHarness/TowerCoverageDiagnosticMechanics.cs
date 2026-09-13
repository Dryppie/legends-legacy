using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record CoverageTrigger(bool Implicit, AbilityTriggerSpec Definition);
public sealed record CoverageRoute(string EssenceId, string Category, string EvidenceKey, bool Compatible,
    string AbilityId, string AbilityName, AbilitySpecKind AbilityKind, int CooldownTicks,
    AbilityEffectSpec Effect, IReadOnlyList<CoverageTrigger> SelectedTriggers, IReadOnlyList<string> Unknowns)
{
    public bool SelfOnly => Effect.Target == AbilityTargetSelector.Self;
    public bool DeathEventOnly => SelectedTriggers.Count > 0 && SelectedTriggers.All(t => t.Definition.Event is
        AbilityTriggerEvent.OnDeath or AbilityTriggerEvent.OnEnemyDeath or AbilityTriggerEvent.OnKill);
    public int AuthoredChancePercent => Effect.ChancePercent;
    public int? SeparateRuntimeControlChancePercent => Effect.Operation == AbilityEffectOperation.ApplyCondition
        && Effect.Condition is StandardConditionType.Stun or StandardConditionType.Freeze
        && !Effect.GuaranteedConditionApplication ? 80 : null;
}
public sealed record CoverageLogRoute(string Key, string OwnerId, string AbilityId, string AbilityName, AbilityEffectSpec Effect);
public sealed record CoverageLogMatch(int EventIndex, string Status, string? Mechanism, IReadOnlyList<string> CandidateKeys,
    string ActorId, string TargetId, string Source, string StatsSource, EventType EventType, int Tick, int Magnitude);

/// <summary>Descriptive inventory and log attribution only. Never called by candidate generation or fitness.</summary>
public static class TowerCoverageDiagnosticMechanics
{
    public static IReadOnlyList<CoverageRoute> Routes(TowerBossInventoryReport inventory,
        IReadOnlyList<BossCoverageFeature> baseline, IReadOnlyList<BossCoverageFeature> compatible)
    {
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var eligible = compatible.Select(f => (f.EssenceId, f.Kind)).ToHashSet();
        return baseline.OrderBy(f => f.Kind, StringComparer.Ordinal).ThenBy(f => f.EssenceId, StringComparer.Ordinal)
            .SelectMany(f => f.EvidenceKeys.Where(k => k.StartsWith("Effect:Ability:", StringComparison.Ordinal)).Order(StringComparer.Ordinal)
                .Select(key => {
                    var split = key.LastIndexOf('/');
                    var ability = nodes[key[7..split]].Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
                    var effect = nodes[key].Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
                    return new CoverageRoute(f.EssenceId, f.Kind, key, eligible.Contains((f.EssenceId, f.Kind)), ability.Id,
                        ability.Name, ability.Kind, ability.CooldownTicks, effect, Triggers(ability, effect.Id),
                        nodes[key].Unknowns.Concat(nodes[key[7..split]].Unknowns).Distinct().Order(StringComparer.Ordinal).ToArray());
                })).ToArray();
    }

    public static IReadOnlyList<CoverageTrigger> Triggers(AbilitySpec ability, string effectId)
        => ability.Triggers.Count == 0
            ? [new(true, new() { Event = ability.Kind == AbilitySpecKind.Active ? AbilityTriggerEvent.OnAbilityUsed : AbilityTriggerEvent.OnCombatStart })]
            : ability.Triggers.Where(t => t.EffectIds.Count == 0 || t.EffectIds.Contains(effectId, StringComparer.OrdinalIgnoreCase))
                .Select(t => new CoverageTrigger(false, t)).ToArray();

    // All direct equipped ability effects compete, including effects outside the five coverage categories.
    // Otherwise an unclassified effect with the same log name could be incorrectly credited to coverage.
    public static IReadOnlyList<CoverageLogRoute> LogRoutes(JsonElement participants, TowerBossInventoryReport inventory)
    {
        var nodes = inventory.Nodes.Where(n => n.Kind == TowerMechanicNodeKind.Ability).ToDictionary(n => n.Id, StringComparer.Ordinal);
        var essences = inventory.Essences.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var routes = new List<CoverageLogRoute>();
        foreach (var p in participants.EnumerateArray())
        {
            var owner = p.GetProperty("slot").GetProperty("slotId").GetString()!;
            var ids = p.GetProperty("nativeAbilityIds").EnumerateArray().Select(a => a.GetString()!)
                .Concat(p.GetProperty("essences").EnumerateArray().SelectMany(e =>
                    essences[e.GetProperty("essenceDefinitionId").GetString()!].AbilityIds)).Distinct(StringComparer.Ordinal);
            foreach (var id in ids.Order(StringComparer.Ordinal))
            {
                if (!nodes.TryGetValue(id, out var node)) throw new InvalidDataException("Prepared ability absent from frozen inventory: " + id);
                var ability = node.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
                foreach (var effect in ability.Effects.OrderBy(e => e.Id, StringComparer.Ordinal))
                    routes.Add(new(owner + "/Effect:Ability:" + id + "/" + effect.Id, owner, id, ability.Name, effect));
            }
        }
        return routes.OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
    }

    public static CoverageLogMatch Match(int index, CombatLogItem e, IReadOnlyList<CoverageLogRoute> routes)
    {
        var candidates = routes.Where(r => r.OwnerId == e.ActorId).Select(r => (Route: r, Mode: Mode(r, e)))
            .Where(r => r.Mode != null).OrderBy(r => r.Route.Key, StringComparer.Ordinal).ToArray();
        return new(index, candidates.Length switch { 0 => "Unmatched", 1 => "Unique", _ => "Ambiguous" },
            candidates.Length == 1 ? candidates[0].Mode : null, candidates.Select(r => r.Route.Key).ToArray(),
            e.ActorId, e.TargetId, e.Source, e.StatsSource, e.EventType, e.Timestamp, e.Magnitude);
    }

    private static string? Mode(CoverageLogRoute route, CombatLogItem e)
    {
        var effect = route.Effect;
        if (e.EventType is EventType.StaggerApplied or EventType.StaggerBroken)
            return effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Stun or StandardConditionType.Freeze
                && e.Source == route.AbilityName && e.StatsSource == route.AbilityName ? "OwnerAbilityStagger" : null;
        if (e.EventType == EventType.StaggerRecovered) return null;
        if (e.Source == effect.Id) return "OwnerEffectId";
        if (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is not null
            && e.Source == ConditionLogId(effect.Condition.Value) && e.StatsSource == route.AbilityName)
            return "OwnerAbilityCondition";
        return null;
    }

    // FastCombatEngine.GetConditionId has one authored-enum/log-identity spelling difference.
    internal static string ConditionLogId(StandardConditionType condition) => condition == StandardConditionType.Vulnerable
        ? "condition.vulnerability" : "condition." + condition.ToString().ToLowerInvariant();

    public static bool Application(EventType type) => type is EventType.Buff or EventType.Debuff or EventType.StatusEffect
        or EventType.StaggerApplied or EventType.StaggerBroken or EventType.RestoreBarrier or EventType.Heal or EventType.HealCrit or EventType.HealOverTime;
    public static bool Healing(EventType type) => type is EventType.Heal or EventType.HealCrit or EventType.HealOverTime;

    public static object Categories(IReadOnlyList<CoverageRoute> routes, bool compatibleOnly) => routes.Where(r => !compatibleOnly || r.Compatible)
        .GroupBy(r => r.Category).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new {
            Category = g.Key, Providers = g.Select(r => r.EssenceId).Distinct().Count(), EffectRoutes = g.Count(),
            SelfOnlyProviders = g.GroupBy(r => r.EssenceId).Count(p => p.All(r => r.SelfOnly)),
            DeathEventOnlyProviders = g.GroupBy(r => r.EssenceId).Count(p => p.All(r => r.DeathEventOnly))
        }).ToArray();

    public static object Exposure(TowerScenario scenario, IReadOnlyList<CoverageRoute> routes, bool compatibleOnly)
        => routes.Where(r => !compatibleOnly || r.Compatible).GroupBy(r => r.Category).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => {
            var providers = g.Select(r => r.EssenceId).ToHashSet(StringComparer.Ordinal);
            return new { Category = g.Key, CharactersWithProvider = scenario.Party.Count(p => p.Build.EssenceIds.Any(providers.Contains)),
                Instances = scenario.Party.OrderBy(p => p.PartySlot).SelectMany(p => p.Build.EssenceIds.Where(providers.Contains)
                    .Select(id => new { p.PartySlot, EssenceId = id, SelfOnly = g.Where(r => r.EssenceId == id).All(r => r.SelfOnly),
                        DeathEventOnly = g.Where(r => r.EssenceId == id).All(r => r.DeathEventOnly) })).ToArray() };
        }).ToArray();
}
