using System.Text;
using System.Text.Json;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.WorldTower;

namespace BalanceHarness;

public enum TowerMechanicNodeKind { Ability, Status, Summon, StandardCondition, Effect, Trigger, EventIdentity, SummonGroup }
public enum TowerMechanicRelation
{
    ContainsEffect, ContainsTrigger, TriggerSelectsEffect, Applies, Consumes, Removes, Reads,
    ScalesFrom, Alternative, ResetsCooldown, SummonAbility, LinkedEffect, EventFilter, SummonGroup, Forbids, Emits, ListensTo
}

public sealed record TowerMechanicReference(string SourceKey, string TargetKey,
    TowerMechanicRelation Relation, string Field, bool Activates);
public sealed record TowerMechanicNode(string Key, TowerMechanicNodeKind Kind, string Id,
    JsonElement Definition, IReadOnlyList<string> Signals, IReadOnlyList<string> Unknowns);
public sealed record TowerEssenceMechanics(string Id, string Name, string SourceMonsterId,
    IReadOnlyList<string> AbilityIds, IReadOnlyList<string> Dependencies, IReadOnlyList<string> Signals);
public sealed record TowerBossProfile(int FloorNumber, string GuardianName, Guid GuardianCreatureId,
    string AbilityProfileId, int RequiredSlots, IReadOnlyList<string> AbilityIds,
    TowerFloorDefinition Definition, JsonElement Creature, IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Signals, IReadOnlyList<string> AuthoredFacts,
    IReadOnlyList<string> CounterHypotheses, IReadOnlyList<string> Unknowns,
    IReadOnlyList<string> CounterIntents);
public sealed record TowerEnablerConsumerPair(string EnablerEssenceId, string ConsumerEssenceId,
    string Mechanism, string ProducerNodeKey, string ConsumerNodeKey, string Compatibility,
    IReadOnlyList<string> Unknowns);
public sealed record TowerTelemetryAudit(string Metric, string EvidenceLevel, string Source,
    string Semantics, IReadOnlyList<string> Unknowns);
public sealed record TowerBossInventoryReport(int SchemaVersion,
    IReadOnlyDictionary<string, string> SourceHashes, IReadOnlyList<TowerBossProfile> Bosses,
    IReadOnlyList<TowerEssenceMechanics> Essences, IReadOnlyList<TowerMechanicNode> Nodes,
    IReadOnlyList<TowerMechanicReference> References, IReadOnlyList<TowerEnablerConsumerPair> EnablerConsumerPairs,
    IReadOnlyList<TowerTelemetryAudit> TelemetryAudit, IReadOnlyList<string> Limitations);

/// <summary>Source-derived hypotheses for offline search, never an Essence power ranking.</summary>
public static class TowerBossInventory
{
    public static readonly IReadOnlyList<string> SourceFiles =
    ["world-tower/tower-floors.json", "world/creatures.json", "combat/creature-abilities.json",
        "combat/abilities.json", "combat/statuses.json", "combat/summons.json", "essences/essences.json"];

    public static TowerBossInventoryReport Create(string root, ThreatAndTankingOptions threat)
    {
        var provider = new JsonAbilityCatalogProvider(new ConfigurationBuilder().Build(), root, HarnessJson.Options, threat);
        _ = provider.GetCompiledCatalog(); // Production validation/compiler, including cooldown/trigger and reference rules.
        var catalog = provider.GetCatalog();
        var graph = new Graph(catalog);
        var content = new OfflineContent(root, threat);
        var essences = content.Essences.GetAll().OrderBy(e => e.Id, StringComparer.Ordinal).Select(e =>
        {
            var ids = new[] { e.ActiveAbility.Id, e.PassiveAbility.Id };
            var roots = ids.Select(id => Key(TowerMechanicNodeKind.Ability, id)).ToArray();
            return new TowerEssenceMechanics(e.Id, e.DisplayName, e.SourceMonsterId, ids,
                graph.Closure(roots), graph.Signals(roots));
        }).ToArray();
        var floors = new JsonWorldTowerDefinitionProvider(Path.Combine(root, "Data", SourceFiles[0]), HarnessJson.Options).GetFloors();
        // This version promises all fifteen released floors; a release expansion needs an explicit contract update.
        if (!floors.Select(f => f.FloorNumber).SequenceEqual(Enumerable.Range(1, 15)))
            throw new InvalidDataException("Boss inventory schema 1 requires all 15 released Tower floors.");
        var creatures = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data", "world/creatures.json"))
            .GetProperty("creatures").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetGuid());
        var profiles = new JsonCreatureAbilityDefinitionProvider(new ConfigurationBuilder().Build(), root, HarnessJson.Options);
        var bosses = floors.Select(floor =>
        {
            if (!creatures.TryGetValue(floor.GuardianCreatureId, out var creature))
                throw new InvalidDataException($"Floor {floor.FloorNumber} references missing creature '{floor.GuardianCreatureId}'.");
            var runtimeProfile = CreatureEssenceSource.GetMonsterDefinitionId(creature.GetProperty("name").GetString()!);
            if (!runtimeProfile.Equals(floor.GuardianAbilityProfileId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Floor {floor.FloorNumber} declares profile '{floor.GuardianAbilityProfileId}', but production creature preparation selects '{runtimeProfile}'.");
            var ids = profiles.GetAbilityIds(runtimeProfile).ToArray();
            if (ids.Length == 0 || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Length)
                throw new InvalidDataException($"Floor {floor.FloorNumber} has an empty or duplicate ability profile.");
            var roots = ids.Select(id => Key(TowerMechanicNodeKind.Ability, id)).ToArray();
            var signals = graph.Signals(roots);
            var dependencies = graph.Closure(roots);
            var facts = new List<string>
            {
                $"Starts with one guardian; required party members: {floor.RequiredSlots}. Summons are created only by production abilities.",
                $"Guardian creature {floor.GuardianCreatureId}; declared ability profile {floor.GuardianAbilityProfileId}.",
                "The floor definition preserves guardian scaling and stagger thresholds, durations, growth and maximum breaks."
            };
            foreach (var id in ids)
            {
                var ability = catalog.AbilitiesById[id];
                facts.Add($"{id}: {ability.Kind}, cooldown {ability.CooldownTicks} ticks; " +
                    $"triggers [{string.Join(", ", ability.Triggers.Select(t => t.Event))}]; " +
                    $"effects [{string.Join(", ", ability.Effects.Select(e => $"{e.Operation} -> {e.Target}"))}].");
            }
            return new TowerBossProfile(floor.FloorNumber, floor.GuardianName, floor.GuardianCreatureId,
                floor.GuardianAbilityProfileId, floor.RequiredSlots, ids, floor, creature, dependencies,
                signals, facts, CounterHypotheses(signals),
                ["These are authored mechanics and source-reviewed runtime rules; no counter has been confirmed by this inventory.",
                 "Actual targets depend on taunt, locked targets, summon eligibility, effect conditions and runtime tie breaking; threat alone does not establish protection.",
                 "Phase timing, barrier waste, boss healing caused by each recovery path and deaths prevented require separate diagnostic trials."],
                CounterIntents(signals));
        }).ToArray();
        return new(1, SourceFiles.ToDictionary(p => p, p => HarnessJson.FileHash(Path.Combine(root, "Data", p))),
            bosses, essences, graph.Nodes.Values.OrderBy(n => n.Key, StringComparer.Ordinal).ToArray(),
            graph.References.OrderBy(r => r.SourceKey, StringComparer.Ordinal).ThenBy(r => r.Field, StringComparer.Ordinal)
                .ThenBy(r => r.TargetKey, StringComparer.Ordinal).ToArray(),
            graph.Pairs(essences), Audit(),
            ["Dependencies are cycle-safe typed references. Signals traverse activation edges only: merely reading a status does not claim to apply its effects.",
             "All ability/effect/trigger/status/summon definitions retain targets, subject filters, chance, delays, cooldowns, scaling, caps, refresh/stack policies and consumption parameters. Runtime subject binding is not inferred from a matching identifier.",
             "Enabler/consumer pairs are untested structural hypotheses, with same-owner versus cross-character restrictions retained. Unexplained Essences remain eligible for uniform exploration.",
             "Status sharing can refresh, replace, saturate, consume or remove another contribution. Standard condition rules live in the engine; their numeric caps are not duplicated as authored JSON facts.",
             "Source hashes cover frozen content. Source-reviewed telemetry statements describe this implementation; replay execution identity must additionally match the saved executable.",
             "This inventory changes no loadout legality, slot budget, Combat Style, gear, ownership, encounter fidelity or combat outcome."]);
    }

    public static string Markdown(TowerBossInventoryReport report)
    {
        var text = new StringBuilder("# Tower boss and Essence mechanics inventory\n\n");
        text.AppendLine($"{report.Bosses.Count} bosses; {report.Essences.Count} Essences; {report.Nodes.Count} typed nodes; {report.EnablerConsumerPairs.Count} untested interaction hypotheses. Full definitions and references are in boss-profiles.json.\n");
        foreach (var limitation in report.Limitations) text.AppendLine("- " + limitation);
        foreach (var boss in report.Bosses)
        {
            text.AppendLine($"\n## Floor {boss.FloorNumber}: {boss.GuardianName}\n");
            text.AppendLine("Authored facts:\n");
            foreach (var fact in boss.AuthoredFacts) text.AppendLine("- " + fact);
            text.AppendLine("\nCounter hypotheses (unconfirmed):\n");
            foreach (var hypothesis in boss.CounterHypotheses) text.AppendLine("- " + hypothesis);
            text.AppendLine("\nUnknowns:\n");
            foreach (var unknown in boss.Unknowns) text.AppendLine("- " + unknown);
        }
        text.AppendLine("\n## Telemetry source audit\n");
        foreach (var audit in report.TelemetryAudit)
        {
            text.AppendLine($"- **{audit.Metric}** ({audit.EvidenceLevel}; `{audit.Source}`): {audit.Semantics}");
            foreach (var unknown in audit.Unknowns) text.AppendLine("  - Unknown: " + unknown);
        }
        text.AppendLine("\n## Source SHA-256\n\n| File | SHA-256 |\n| --- | --- |");
        foreach (var source in report.SourceHashes) text.AppendLine($"| {source.Key} | {source.Value} |");
        return text.ToString();
    }

    private static IReadOnlyList<string> CounterHypotheses(IReadOnlyList<string> signals)
    {
        var result = new List<string> { "Compare focused guardian damage with enough protection to finish the encounter at the declared budget." };
        if (signals.Contains("operation:Summon")) result.Add("Compare add clearing with guardian focus; measure hostile summon active ticks and guardian progress. Add removal order may alter boss effects.");
        if (signals.Contains("operation:Heal")) result.Add("Compare sustained damage, recovery suppression where eligible, and burst against the boss's recovery.");
        if (signals.Contains("operation:ModifyHealingReceived") || signals.Contains("operation:ModifyRegenerationRate"))
            result.Add("Compare barriers/prevention with healing throughput; verify which recovery paths are suppressed.");
        if (signals.Contains("trigger:OnEnemyHealed")) result.Add("Compare necessary recovery with prevention; measure enemy healing feedback on reserved diagnostic seeds.");
        if (signals.Contains("target:AllEnemies")) result.Add("Compare group protection and distributed recovery against party-wide effects; this does not imply a need for party AoE damage.");
        if (signals.Contains("operation:Dispel")) result.Add("Compare reliance on dispellable buffs with resilient damage/support and effective stagger.");
        if (signals.Any(s => s.Contains("HealthSpread", StringComparison.Ordinal))) result.Add("Compare distributed rescue/recovery and health equalization at the actual health-spread check ticks.");
        return result;
    }

    private static IReadOnlyList<string> CounterIntents(IReadOnlyList<string> signals)
    {
        var intents = new SortedSet<string>(StringComparer.Ordinal) { "focused-damage", "protection" };
        if (signals.Contains("operation:Summon")) intents.Add("add-clearing");
        if (signals.Contains("operation:ApplyCondition") || signals.Contains("operation:ApplyStatus")) intents.Add("denial");
        if (signals.Contains("target:AllEnemies") && !signals.Contains("trigger:OnEnemyHealed")) intents.Add("sustain");
        return intents.ToArray();
    }

    private static string Key(TowerMechanicNodeKind kind, string id) => kind + ":" + id;

    private sealed class Graph
    {
        public Dictionary<string, TowerMechanicNode> Nodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<TowerMechanicReference> References { get; } = [];
        private readonly Dictionary<string, AbilityEffectSpec> _effects = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AbilityTriggerSpec> _triggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _eventIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<TowerMechanicReference>> _outgoing;

        public Graph(AbilityCatalog catalog)
        {
            foreach (var condition in Enum.GetValues<StandardConditionType>())
                Add(TowerMechanicNodeKind.StandardCondition, condition.ToString(), new { condition }, ["condition:" + condition],
                    ["Engine-owned condition: effective duration, stack cap, cleanse/ward/stagger behavior must be read with ApplyCondition and the runtime. A condition reference does not establish successful application."]);
            foreach (var ability in catalog.Abilities)
                AddOwner(TowerMechanicNodeKind.Ability, ability.Id, ability, ability.Effects, ability.Triggers,
                    ability.Tags.Select(t => "tag:" + t).Concat(["cooldown-ticks:" + ability.CooldownTicks]).ToArray());
            foreach (var status in catalog.Statuses)
                AddOwner(TowerMechanicNodeKind.Status, status.Id, status, status.Effects, status.Triggers,
                    [$"stacking:{status.StackingPolicy}", $"max-stacks:{status.MaxStacks}", $"locked-at-max:{status.LockAtMaxStacks}"]);
            foreach (var summon in catalog.Summons)
            {
                var key = Add(TowerMechanicNodeKind.Summon, summon.Id, summon,
                    [$"summon-max-active:{summon.MaxActive}", $"summon-duration-ticks:{summon.DurationTicks}"]);
                foreach (var id in summon.AbilityIds)
                    Link(key, Key(TowerMechanicNodeKind.Ability, id), TowerMechanicRelation.SummonAbility, "abilityIds", true);
            }
            // Index all runtime event emitters before resolving event-id predicates (including forward references).
            foreach (var node in Nodes.Values.ToArray())
            {
                var eventId = node.Kind == TowerMechanicNodeKind.StandardCondition ? "condition." + node.Id.ToLowerInvariant()
                    : node.Kind == TowerMechanicNodeKind.Effect ? _effects[node.Key].Id : node.Id;
                if (!_eventIds.TryGetValue(eventId, out var keys)) _eventIds[eventId] = keys = [];
                keys.Add(node.Key);
            }
            foreach (var (key, effect) in _effects)
            {
                if (effect.SummonGroupId is { } group && !Nodes.ContainsKey(Key(TowerMechanicNodeKind.SummonGroup, group)))
                    Add(TowerMechanicNodeKind.SummonGroup, group, new { id = group }, ["summon-group:" + group]);
            }
            foreach (var (key, effect) in _effects) EffectReferences(key, effect);
            foreach (var (key, trigger) in _triggers)
                for (var i = 0; i < trigger.Conditions.Count; i++) ConditionReferences(key, trigger.Conditions[i], $"conditions[{i}]");
            foreach (var reference in References)
                if (!Nodes.ContainsKey(reference.TargetKey))
                    throw new InvalidDataException($"Unresolved mechanic reference {reference.SourceKey}.{reference.Field} -> {reference.TargetKey}.");
            _outgoing = References.GroupBy(r => r.SourceKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        private string Add<T>(TowerMechanicNodeKind kind, string id, T definition, IReadOnlyList<string> signals,
            IReadOnlyList<string>? unknowns = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidDataException($"Missing {kind} identity.");
            var key = Key(kind, id);
            if (!Nodes.TryAdd(key, new(key, kind, id, JsonSerializer.SerializeToElement(definition, HarnessJson.Options), signals, unknowns ?? [])))
                throw new InvalidDataException($"Duplicate mechanic node '{key}'.");
            return key;
        }

        private void Link(string source, string target, TowerMechanicRelation relation, string field, bool activates = false) =>
            References.Add(new(source, target, relation, field, activates));

        private void AddOwner<T>(TowerMechanicNodeKind kind, string id, T definition,
            IReadOnlyList<AbilityEffectSpec> effects, IReadOnlyList<AbilityTriggerSpec> triggers, IReadOnlyList<string> signals)
        {
            var owner = Add(kind, id, definition, signals);
            foreach (var effect in effects)
            {
                var key = Add(TowerMechanicNodeKind.Effect, owner + "/" + effect.Id, effect, EffectSignals(effect));
                _effects.Add(key, effect);
                Link(owner, key, TowerMechanicRelation.ContainsEffect, "effects", true);
            }
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                var key = Add(TowerMechanicNodeKind.Trigger, owner + "/" + i, trigger,
                    new[] { "trigger:" + trigger.Event, $"trigger-cooldown-ticks:{trigger.InternalCooldownTicks}" }
                        .Concat(trigger.Conditions.Select(c => "predicate:" + c.Type)).ToArray());
                _triggers.Add(key, trigger);
                Link(owner, key, TowerMechanicRelation.ContainsTrigger, "triggers", true);
                Link(key, EventNode(trigger.Event), TowerMechanicRelation.ListensTo, "event");
                foreach (var effect in trigger.EffectIds.Count == 0 ? effects.Select(e => e.Id) : trigger.EffectIds)
                    Link(key, Key(TowerMechanicNodeKind.Effect, owner + "/" + effect), TowerMechanicRelation.TriggerSelectsEffect, "effectIds", true);
            }
        }

        private void EffectReferences(string key, AbilityEffectSpec effect)
        {
            var operation = effect.Operation;
            if (operation == AbilityEffectOperation.Heal)
            {
                Link(key, EventNode(AbilityTriggerEvent.OnHealed), TowerMechanicRelation.Emits, "restored-health-event");
                if (!(effect.DurationTicks > 0 && effect.IntervalTicks > 0))
                    Link(key, EventNode(AbilityTriggerEvent.OnHeal), TowerMechanicRelation.Emits, "nonperiodic-restored-health-event");
                Link(key, EventNode(AbilityTriggerEvent.OnEnemyHealed), TowerMechanicRelation.Emits, "enemy-restored-health-event");
            }
            if (operation == AbilityEffectOperation.GrantBarrier)
                Link(key, EventNode(AbilityTriggerEvent.OnBarrierApplied), TowerMechanicRelation.Emits, "barrier-application-event");
            var relation = operation switch
            {
                AbilityEffectOperation.ApplyStatus or AbilityEffectOperation.ApplyCondition or AbilityEffectOperation.ApplyRandomCondition or AbilityEffectOperation.Summon => TowerMechanicRelation.Applies,
                AbilityEffectOperation.ConsumeConditionStacks or AbilityEffectOperation.ConsumeOwnedSummon => TowerMechanicRelation.Consumes,
                AbilityEffectOperation.RemoveStatus or AbilityEffectOperation.RemoveCondition => TowerMechanicRelation.Removes,
                _ => TowerMechanicRelation.Reads
            };
            void Reference(TowerMechanicNodeKind kind, string? id, TowerMechanicRelation rel, string field, bool activates = false)
            { if (id is not null) Link(key, Key(kind, id), rel, field, activates); }
            Reference(TowerMechanicNodeKind.Status, effect.StatusId, relation, "statusId", operation is AbilityEffectOperation.ApplyStatus or AbilityEffectOperation.ToggleStatus);
            Reference(TowerMechanicNodeKind.Status, effect.AlternativeStatusId, TowerMechanicRelation.Alternative, "alternativeStatusId", operation == AbilityEffectOperation.ToggleStatus);
            Reference(TowerMechanicNodeKind.Status, effect.ScalingStatusId, TowerMechanicRelation.ScalesFrom, "scalingStatusId");
            Reference(TowerMechanicNodeKind.StandardCondition, effect.Condition?.ToString(), relation, "condition");
            Reference(TowerMechanicNodeKind.StandardCondition, effect.AlternativeCondition?.ToString(), TowerMechanicRelation.Alternative, "alternativeCondition");
            Reference(TowerMechanicNodeKind.StandardCondition, effect.ScalingCondition?.ToString(), TowerMechanicRelation.ScalesFrom, "scalingCondition");
            Reference(TowerMechanicNodeKind.StandardCondition, effect.TargetCondition?.ToString(), TowerMechanicRelation.Reads, "targetCondition");
            Reference(TowerMechanicNodeKind.StandardCondition, effect.LifeStealTargetCondition?.ToString(), TowerMechanicRelation.Reads, "lifeStealTargetCondition");
            Reference(TowerMechanicNodeKind.Summon, effect.SummonId, relation, "summonId", operation == AbilityEffectOperation.Summon);
            Reference(TowerMechanicNodeKind.Summon, effect.RepeatPerOwnedSummonId, TowerMechanicRelation.ScalesFrom, "repeatPerOwnedSummonId");
            Reference(TowerMechanicNodeKind.Summon, effect.ScalingOwnedSummonId, TowerMechanicRelation.ScalesFrom, "scalingOwnedSummonId");
            Reference(TowerMechanicNodeKind.Ability, effect.AbilityId, TowerMechanicRelation.ResetsCooldown, "abilityId");
            Reference(TowerMechanicNodeKind.SummonGroup, effect.SummonGroupId, TowerMechanicRelation.SummonGroup, "summonGroupId");
            if (effect.LinkedEffectId is { } linked)
            {
                var owner = Nodes[key].Id[..Nodes[key].Id.LastIndexOf('/')];
                Reference(TowerMechanicNodeKind.Effect, owner + "/" + linked, TowerMechanicRelation.LinkedEffect, "linkedEffectId");
            }
            for (var i = 0; i < effect.Conditions.Count; i++) ConditionReferences(key, effect.Conditions[i], $"conditions[{i}]");
        }

        private string EventNode(AbilityTriggerEvent triggerEvent)
        {
            var id = "trigger:" + triggerEvent;
            var key = Key(TowerMechanicNodeKind.EventIdentity, id);
            if (!Nodes.ContainsKey(key))
                Add(TowerMechanicNodeKind.EventIdentity, id, new { triggerEvent }, ["event:" + triggerEvent],
                    ["Source-reviewed event possibility, conditional on successful effect execution. Trigger source/recipient scope and effect conditions still apply."]);
            return key;
        }

        private void ConditionReferences(string key, AbilityConditionSpec condition, string field)
        {
            if (condition.Condition is { } standard)
                Link(key, Key(TowerMechanicNodeKind.StandardCondition, standard.ToString()),
                    condition.Type == AbilityConditionType.NoEnemyHasCondition ? TowerMechanicRelation.Forbids : TowerMechanicRelation.Reads,
                    field + ".condition");
            if (condition.StatusId is not { } id) return;
            if (condition.Type is not (AbilityConditionType.EventIdIs or AbilityConditionType.EventIdIsNot))
            {
                Link(key, Key(TowerMechanicNodeKind.Status, id), TowerMechanicRelation.Reads, field + ".statusId");
                return;
            }
            var targets = _eventIds.GetValueOrDefault(id);
            if (targets is not null)
                foreach (var target in targets) Link(key, target, TowerMechanicRelation.EventFilter, field + ".statusId");
            else if (Nodes.ContainsKey(Key(TowerMechanicNodeKind.SummonGroup, id)))
                Link(key, Key(TowerMechanicNodeKind.SummonGroup, id), TowerMechanicRelation.EventFilter, field + ".statusId");
            else
            {
                // Engine event identities such as basic_attack are not catalog references. Keep them visible.
                if (id.StartsWith("status.", StringComparison.OrdinalIgnoreCase) || id.StartsWith("ability.", StringComparison.OrdinalIgnoreCase)
                    || id.StartsWith("condition.", StringComparison.OrdinalIgnoreCase) || id.StartsWith("effect.", StringComparison.OrdinalIgnoreCase)
                    || id.StartsWith("summon-group.", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Unresolved event reference {key}.{field} -> {id}.");
                var eventKey = Key(TowerMechanicNodeKind.EventIdentity, id);
                if (!Nodes.ContainsKey(eventKey)) Add(TowerMechanicNodeKind.EventIdentity, id, new { id }, ["event-identity:" + id],
                    ["Runtime event identity; emitter and effective source/target scope require runtime source review."]);
                Link(key, eventKey, TowerMechanicRelation.EventFilter, field + ".statusId");
            }
        }

        public IReadOnlyList<string> Closure(IEnumerable<string> roots, bool activationOnly = false)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var remaining = new Stack<string>(roots);
            while (remaining.TryPop(out var key))
            {
                if (!Nodes.ContainsKey(key)) throw new InvalidDataException($"Unresolved mechanic root '{key}'.");
                if (!seen.Add(key)) continue;
                foreach (var reference in _outgoing.GetValueOrDefault(key) ?? [])
                    if (!activationOnly || reference.Activates) remaining.Push(reference.TargetKey);
            }
            return seen.Order(StringComparer.Ordinal).ToArray();
        }

        public IReadOnlyList<string> Signals(IEnumerable<string> roots) => Closure(roots, true)
            .SelectMany(k => Nodes[k].Signals).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        public IReadOnlyList<TowerEnablerConsumerPair> Pairs(IReadOnlyList<TowerEssenceMechanics> essences)
        {
            var result = new List<TowerEnablerConsumerPair>();
            var active = essences.ToDictionary(e => e.Id, e => Closure(e.AbilityIds.Select(id => Key(TowerMechanicNodeKind.Ability, id)), true).ToHashSet(StringComparer.OrdinalIgnoreCase));
            var producers = References.Where(r => r.Relation == TowerMechanicRelation.Applies && _effects.ContainsKey(r.SourceKey)).ToArray();
            var consumers = References.Where(r => r.Relation is TowerMechanicRelation.Reads or TowerMechanicRelation.Consumes or TowerMechanicRelation.ScalesFrom)
                .Where(r => Nodes[r.TargetKey].Kind is TowerMechanicNodeKind.Status or TowerMechanicNodeKind.StandardCondition or TowerMechanicNodeKind.Summon).ToLookup(r => r.TargetKey, StringComparer.OrdinalIgnoreCase);
            foreach (var producer in producers)
            foreach (var consumer in consumers[producer.TargetKey])
            foreach (var enabler in essences.Where(e => active[e.Id].Contains(producer.SourceKey)))
            foreach (var dependent in essences.Where(e => e.Id != enabler.Id && active[e.Id].Contains(consumer.SourceKey)))
            {
                var effect = _effects[producer.SourceKey];
                var self = effect.Target is AbilityTargetSelector.Self or AbilityTargetSelector.Source;
                var ownedSummon = Nodes[producer.TargetKey].Kind == TowerMechanicNodeKind.Summon;
                var compatibility = self || ownedSummon ? "same-owner-or-explicit-recipient-required" : "recipient-and-trigger-scope-unverified";
                if ((self || ownedSummon) && enabler.SourceMonsterId.Equals(dependent.SourceMonsterId, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(new(enabler.Id, dependent.Id, producer.TargetKey, producer.SourceKey, consumer.SourceKey,
                    compatibility,
                    ["Matching identifiers are a proposal, not proof of synergy. Resolve source/target/event subjects, trigger restrictions and legal family placement in the full party.",
                     "Shared status caps, refresh/replace rules, competing consumers and application timing may cancel the benefit. Compare each ingredient and the pair on reserved diagnostic seeds."]));
            }
            // Source-reviewed event paths add support hypotheses without treating enemy-healing feedback as ally synergy.
            var listeners = References.Where(r => r.Relation == TowerMechanicRelation.ListensTo)
                .ToLookup(r => r.TargetKey, StringComparer.OrdinalIgnoreCase);
            foreach (var producer in References.Where(r => r.Relation == TowerMechanicRelation.Emits
                         && r.TargetKey != Key(TowerMechanicNodeKind.EventIdentity, "trigger:OnEnemyHealed")))
            foreach (var listener in listeners[producer.TargetKey])
            foreach (var enabler in essences.Where(e => active[e.Id].Contains(producer.SourceKey)))
            foreach (var dependent in essences.Where(e => e.Id != enabler.Id && active[e.Id].Contains(listener.SourceKey)))
            {
                var sameOwner = _triggers[listener.SourceKey].Event == AbilityTriggerEvent.OnHeal
                    || _effects[producer.SourceKey].Target is AbilityTargetSelector.Self or AbilityTargetSelector.Source;
                if (sameOwner && enabler.SourceMonsterId.Equals(dependent.SourceMonsterId, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(new(enabler.Id, dependent.Id, producer.TargetKey, producer.SourceKey, listener.SourceKey,
                    sameOwner ? "same-owner-or-explicit-recipient-required" : "recipient-and-trigger-scope-unverified",
                    ["A successful heal/barrier can emit this event, but actual recipients, restored magnitude, trigger conditions and internal cooldowns must match. Overheal does not establish a healing event.",
                     "This is a source-derived proposal; compare ingredients and combination on reserved diagnostic seeds before claiming synergy."]));
            }
            return result.DistinctBy(p => (p.EnablerEssenceId, p.ConsumerEssenceId, p.Mechanism, p.ProducerNodeKey, p.ConsumerNodeKey))
                .OrderBy(p => p.EnablerEssenceId, StringComparer.Ordinal).ThenBy(p => p.ConsumerEssenceId, StringComparer.Ordinal)
                .ThenBy(p => p.Mechanism, StringComparer.Ordinal).ThenBy(p => p.ProducerNodeKey, StringComparer.Ordinal)
                .ThenBy(p => p.ConsumerNodeKey, StringComparer.Ordinal).ToArray();
        }
    }

    private static IReadOnlyList<string> EffectSignals(AbilityEffectSpec effect)
    {
        var signals = new SortedSet<string>(StringComparer.Ordinal)
        { "operation:" + effect.Operation, "target:" + effect.Target, "damage:" + effect.DamageType };
        if (effect.Condition is { } condition) signals.Add("condition:" + condition);
        if (effect.ScalingAttribute is { } scaling) signals.Add("scaling:" + scaling);
        foreach (var conditionSpec in effect.Conditions) signals.Add("predicate:" + conditionSpec.Type);
        if (effect.IgnoreTaunt) signals.Add("targeting:ignore-taunt");
        if (effect.ExcludeSummons) signals.Add("targeting:exclude-summons");
        if (effect.Operation == AbilityEffectOperation.Damage)
        {
            signals.Add("intent:focused-damage");
            if (effect.Target is AbilityTargetSelector.AllEnemies or AbilityTargetSelector.SummonedEnemies
                or AbilityTargetSelector.TwoEnemies or AbilityTargetSelector.ThreeEnemies
                or AbilityTargetSelector.TwoRandomEnemies or AbilityTargetSelector.ThreeRandomEnemies)
                signals.Add("intent:add-clearing");
        }
        if (effect.Operation == AbilityEffectOperation.Heal || effect.LifeStealPercentage > 0
            || (effect.Operation is AbilityEffectOperation.ModifyHealingReceived or AbilityEffectOperation.ModifyRegenerationRate && effect.BaseValue > 0))
            signals.Add("intent:sustain");
        if (effect.Operation is AbilityEffectOperation.GrantBarrier or AbilityEffectOperation.GrantCover
            || (effect.Operation == AbilityEffectOperation.ModifyDamageTaken && effect.BaseValue < 0)
            || (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Guard or StandardConditionType.Ward))
            signals.Add("intent:protection");
        if (effect.Operation is AbilityEffectOperation.Cleanse or AbilityEffectOperation.Dispel
            || (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Stun or StandardConditionType.Freeze or StandardConditionType.Silence))
            signals.Add("intent:denial");
        return signals.ToArray();
    }

    private static IReadOnlyList<TowerTelemetryAudit> Audit() =>
    [
        new("Effective healing and feedback", "source-reviewed", "FastCombatEngine.RestoreHealth; TickHealthRegeneration; FastCombatEngine.CombatStyles.ApplyCombatStyleRecovery",
            "Direct and periodic Heal and lifesteal restoration log restored health after the healing-received modifier and health cap. Periodic effects skip OnHeal, but successful restoration publishes OnHealed and OnEnemyHealed. Regeneration separately tracks potential/overheal/pulses and publishes OnEnemyHealed using restored health.",
            ["No causal deaths-prevented claim. Boss recovery attributable to each party heal or health swap is not a dedicated summary field; verify detailed event sequence. Combat Style-specific recovery paths are outside this search contract."]),
        new("Damage, barriers and ownership", "source-reviewed", "EntityStats; AbilityStats; CombatStatsAggregator; FastCombatEngine.ConsumeBarrier",
            "Entity TargetInteractions separate damage/healing/barrier generation by recipient; guardian versus summon recipients can be separated using entity IDs and IsSummonedEntity. Entity DamageBlocked records barrier absorption, distinct from BarrierGenerated; ability TotalBarrier records generation. Detailed barrier contribution events exist.",
            ["Summary ability barrier generation cannot establish absorption, expiry or waste per producer. Summon entity damage must not be blindly added to owner totals without checking attribution. DamageDone is not an independent measure of useful guardian progress."]),
        new("Summon windows", "source-reviewed", "CompactCombatTelemetry; FastCombatEngine.TickSummons",
            "Compact telemetry records active hostile summon ticks, first additional-hostile/clear ticks and wave/window counts. Entity telemetry includes summon expiry and summon end ticks.",
            ["A cleared hostile window can follow death, expiry or boss consumption. Window completion alone does not prove successful add killing; timed phase attribution needs event logs."]),
        new("Control and target selection", "source-reviewed", "FastCombatEngine.ApplyCondition; FillTargets; IsSourceScopedTriggerRelevant; EntityStats",
            "Stun/Freeze can fail through application chance, Unstoppable or Ward; when the target has stagger they apply stagger instead. Target selectors preserve IgnoreTaunt/ExcludeSummons/health-percentage flags. Health-extremum selectors can honor forced taunt unless IgnoreTaunt; CurrentTarget uses locked or attention targeting. Source-scoped healing triggers and enemy-healed listeners have different recipient rules.",
            ["Do not count a control tag as a denied action. ActionDeniedTicks, StaggeredTicks, StaggerBreaks and actual target interactions require trials; health-spread check timing is not a compact aggregate."]),
        new("Status and condition conflicts", "source-reviewed", "FastCombatEngine.ApplyStatus; ApplyCondition; AbilityRuntime.RuntimeStatus",
            "Statuses retain authored Refresh/Stack/Replace, MaxStacks and LockAtMaxStacks. RuntimeStatus clamps stacks and can lock removal after maximum. Standard conditions have engine-specific shared, unique or independent application policies; Stun/Freeze use the guardian's stagger rules.",
            ["Matching status/condition names do not prove cooperating owners or compatible timing. Consumption, removal, refresh and saturation may conflict; the complete effect and trigger specs must be inspected."])
    ];
}
