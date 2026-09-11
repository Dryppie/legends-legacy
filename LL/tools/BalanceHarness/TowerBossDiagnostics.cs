using System.Text.Json;

namespace BalanceHarness;

/// <summary>Predeclared structural replacement experiments. Selection never reads combat outcomes.</summary>
public static class TowerBossDiagnostics
{
    public const string Policy = "boss-mechanic-replacements-v2";

    private sealed record Mechanic(TowerMechanicNode Node, JsonElement Spec)
    {
        public string Operation => String(Spec, "operation");
        public string Target => String(Spec, "target");
        public string Condition => String(Spec, "condition");
        public string Signature => Operation + ":" + Target + ":" + Condition;
    }
    private sealed record Essence(TowerEssenceMechanics Definition, Mechanic[] Effects,
        string[] Recovery, Mechanic[] Protection, Mechanic[] Pressure, Mechanic[] Suppression);
    private sealed record Position(int Member, int Index, Essence Essence);
    private sealed record Swap(Position Position, Essence Insert, string Hypothesis, Mechanic[] Evidence,
        int Match, int Novelty, string Tie);

    public static BossDiagnosticPlan Create(TowerBossSearchDefinition d, TowerBossInventoryReport inventory)
    {
        if (d.SchemaVersion != 2 || d.Refinement?.DiagnosticPolicy != Policy)
            throw new InvalidDataException($"Diagnostic replacement selection requires schema 2 and policy '{Policy}'.");
        var anchor = d.Controls.SingleOrDefault(p => p.Id == d.Refinement.AnchorId)
            ?? throw new InvalidDataException("The diagnostic anchor must be an explicit retained control.");
        BossDiagnosticPlan Unsupported(string reason) => new("unsupported", null, null, [],
            $"{Policy}; anchor {anchor.Id}. {reason} No fallback enabler/consumer pair was selected.");
        if (d.Objective.TargetFloorWeights.Count != 1)
            return Unsupported("This replacement policy requires one declared target boss.");
        var boss = inventory.Bosses.Single(b => b.FloorNumber == d.Objective.TargetFloorWeights.Keys.Single());
        if (!boss.CounterIntents.Contains("focused-damage") || !boss.CounterIntents.Contains("protection"))
            return Unsupported("The selected boss does not declare both pressure and protection counter hypotheses.");

        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);
        var edges = inventory.References.Where(r => r.Activates).ToLookup(r => r.SourceKey, StringComparer.OrdinalIgnoreCase);
        TowerMechanicNode[] Active(IEnumerable<string> abilityIds)
        {
            var pending = new Stack<string>(abilityIds.Select(id => "Ability:" + id));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (pending.TryPop(out var key))
            {
                if (!seen.Add(key)) continue;
                if (!nodes.ContainsKey(key)) throw new InvalidDataException($"Missing diagnostic mechanic node '{key}'.");
                foreach (var edge in edges[key]) pending.Push(edge.TargetKey);
            }
            return seen.Order(StringComparer.Ordinal).Select(k => nodes[k]).ToArray();
        }
        var bossNodes = Active(boss.AbilityIds);
        if (bossNodes.Any(n => n.Kind == TowerMechanicNodeKind.Effect && String(n.Definition, "operation") == "Summon"))
            return Unsupported("This policy cannot establish guardian targeting in a boss profile that creates adds.");
        var bossHealing = bossNodes.Any(n => n.Kind == TowerMechanicNodeKind.Effect && String(n.Definition, "operation") == "Heal");
        var feedback = bossNodes.Where(n => n.Kind == TowerMechanicNodeKind.Trigger
                && String(n.Definition, "event") == "OnEnemyHealed")
            .Any(t => edges[t.Key].Where(r => r.Relation == TowerMechanicRelation.TriggerSelectsEffect)
                .Select(r => nodes[r.TargetKey]).Any(n => String(n.Definition, "operation") == "Heal"
                    && String(n.Definition, "target") == "Self"));

        var allowed = d.AllowedEssences.ToHashSet(StringComparer.Ordinal);
        var essences = inventory.Essences.Where(e => allowed.Contains(e.Id)).ToDictionary(e => e.Id, e =>
        {
            var active = Active(e.AbilityIds);
            var effects = active.Where(n => n.Kind == TowerMechanicNodeKind.Effect).Select(n => new Mechanic(n, n.Definition)).ToArray();
            bool TriggerEligible(JsonElement t) => String(t, "event") is "OnCombatStart" or "OnInterval"
                && !Items(t, "conditions").Any();
            bool Selects(JsonElement t, JsonElement effect) => !Items(t, "effectIds").Any()
                || Items(t, "effectIds").Any(id => id.GetString() == String(effect, "id"));
            // Follow only a usable activation path for proposed pressure/protection. Conditional parent summons,
            // statuses and event effects cannot turn a locally unconditional child into an unconditional route.
            var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<string>(e.AbilityIds.Select(id => "Ability:" + id));
            while (pending.TryPop(out var key))
            {
                var node = nodes[key];
                if (node.Kind == TowerMechanicNodeKind.Effect && (Items(node.Definition, "conditions").Any()
                        || Number(node.Definition, "chancePercent", 100) <= 0)
                    || node.Kind == TowerMechanicNodeKind.Trigger && !TriggerEligible(node.Definition)
                    || !eligible.Add(key)) continue;
                foreach (var edge in edges[key])
                {
                    if (edge.Relation == TowerMechanicRelation.ContainsEffect
                        && Items(node.Definition, "triggers").Any()
                        && !Items(node.Definition, "triggers").Any(t => TriggerEligible(t) && Selects(t, nodes[edge.TargetKey].Definition))) continue;
                    pending.Push(edge.TargetKey);
                }
            }
            return new Essence(e, effects, RecoveryRoutes(effects),
                effects.Where(m => IsProtection(m) && eligible.Contains(m.Node.Key)).ToArray(),
                effects.Where(m => m.Operation == "Damage" && Enemy(m.Target) && eligible.Contains(m.Node.Key)
                    && m.Target is not ("AllEnemies" or "TwoEnemies" or "ThreeEnemies" or "TwoRandomEnemies" or "ThreeRandomEnemies")
                    && String(m.Spec, "scalingCondition") == "" && String(m.Spec, "scalingStatusId") == ""
                    && Number(m.Spec, "eventMagnitudeCoefficient") == 0).ToArray(),
                effects.Where(m => Enemy(m.Target) && eligible.Contains(m.Node.Key)
                    && ((m.Operation == "ApplyCondition" && m.Condition == "Wound")
                        || (m.Operation == "ModifyHealingReceived" && Number(m.Spec, "baseValue") < 0))).ToArray());
        }, StringComparer.Ordinal);
        if (anchor.Builds.SelectMany(p => p.Value).Any(id => !essences.ContainsKey(id)))
            throw new InvalidDataException("Every anchor essence must be present in the allowed typed inventory.");
        var families = essences.ToDictionary(p => p.Key, p => p.Value.Definition.SourceMonsterId, StringComparer.Ordinal);
        if (!Legal(anchor.Builds, families)) throw new InvalidDataException("The diagnostic anchor violates essence or source-family legality.");
        var allPositions = anchor.Builds.OrderBy(p => p.Key)
            .SelectMany(p => p.Value.Select((id, i) => new Position(p.Key, i, essences[id]))).ToArray();
        var positions = allPositions.Where(p => d.MutablePartySlots.Contains(p.Member)).ToArray();
        var equipped = anchor.Builds.SelectMany(p => p.Value).ToHashSet(StringComparer.Ordinal);
        var recovery = positions.Where(p => p.Essence.Recovery.Length > 0).ToArray();
        var suppressors = positions.Where(p => p.Essence.Suppression.Length > 0).ToArray();
        var prevention = essences.Values.Where(e => e.Recovery.Length == 0 && e.Protection.Length > 0).ToArray();
        var pressure = essences.Values.Where(e => e.Recovery.Length == 0 && e.Pressure.Length > 0 && e.Suppression.Length == 0).ToArray();

        Swap[] Swaps(IEnumerable<Position> sources, IEnumerable<Essence> replacements, string hypothesis, bool protective)
        {
            return (from position in sources
                    from insert in replacements
                    where position.Essence.Definition.Id != insert.Definition.Id
                    let evidence = protective ? insert.Protection : insert.Pressure
                    let match = protective
                        ? position.Essence.Protection.Select(m => m.Signature).Intersect(insert.Protection.Select(m => m.Signature)).Count()
                        : position.Essence.Pressure.Select(m => String(m.Spec, "damageType")).Intersect(insert.Pressure.Select(m => String(m.Spec, "damageType"))).Count()
                    let tie = HarnessJson.Hash(new { Policy, anchor.Id, boss.FloorNumber, position.Member, position.Index, Essence = insert.Definition.Id, hypothesis })
                    let swap = new Swap(position, insert, hypothesis, evidence, match, equipped.Contains(insert.Definition.Id) ? 0 : 1, tie)
                    where Legal(Apply(anchor, swap), families)
                    select swap).OrderByDescending(s => s.Match).ThenByDescending(s => s.Novelty)
                .ThenBy(s => s.Tie, StringComparer.Ordinal).ToArray();
        }
        Swap[] first;
        Swap[] second;
        string question;
        if (feedback)
        {
            first = Swaps(recovery.Where(p => p.Essence.Recovery.Any(r => r is "direct-heal" or "periodic-heal" or "lifesteal")),
                prevention, "direct-recovery-to-prevention", true);
            // Regeneration is a distinct enemy-healed emitter; a HealingDone-only removal is insufficient.
            second = Swaps(recovery.Where(p => p.Essence.Recovery.Contains("regeneration")), pressure,
                "regeneration-to-pressure", false);
            question = "Enemy-healed feedback: independently replace a direct/periodic/lifesteal supplier with nonrecovery prevention and a regeneration supplier with nonrecovery pressure.";
        }
        else if (bossHealing && boss.CounterIntents.Contains("denial") && allPositions.Any(p => p.Essence.Suppression.Length > 0))
        {
            first = Swaps(suppressors, pressure, "existing-healing-denial-withdrawal-to-pressure", false);
            second = Swaps(recovery, prevention, "recovery-to-prevention", true);
            question = "Boss recovery already has an authored healing-denial supplier in the anchor: compare its withdrawal for pressure and a separate recovery-to-prevention substitution. Withdrawal may leave other denial suppliers active.";
        }
        else if (bossHealing && boss.CounterIntents.Contains("denial"))
        {
            first = Swaps(positions.Where(p => p.Essence.Pressure.Length > 0), essences.Values.Where(e => e.Suppression.Length > 0),
                "pressure-to-healing-denial", false);
            // Evidence for this factor is the denial effect, not incidental damage in the same essence.
            first = first.Select(s => s with { Evidence = s.Insert.Suppression }).ToArray();
            second = Swaps(recovery, prevention, "recovery-to-prevention", true);
            question = "Compare introduced boss-healing denial and a separate recovery-to-prevention substitution.";
        }
        else
            return Unsupported("Neither a typed enemy-healed feedback route nor a supported boss-healing-denial experiment was found.");

        foreach (var a in first)
        foreach (var b in second)
        {
            if ((a.Position.Member, a.Position.Index) == (b.Position.Member, b.Position.Index)
                || a.Insert.Definition.Id == b.Insert.Definition.Id) continue;
            var combined = Apply(anchor, a, b);
            if (!Legal(combined, families)) continue;
            var parties = new[] { TowerPartySelection.Choice("baseline", anchor.Builds),
                TowerPartySelection.Choice("enabler-alone", Apply(anchor, a)),
                TowerPartySelection.Choice("consumer-alone", Apply(anchor, b)),
                TowerPartySelection.Choice("combination", combined) };
            if (parties.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != 4) continue;
            string Describe(Swap s) => $"member {s.Position.Member}, essence slot {s.Position.Index + 1}: {s.Position.Essence.Definition.Id} -> {s.Insert.Definition.Id}; hypothesis {s.Hypothesis}; inserted evidence [{string.Join(", ", s.Evidence.Select(m => m.Node.Key + " (" + m.Signature + ")"))}]; removed effects [{string.Join(", ", s.Position.Essence.Effects.Select(m => m.Node.Key + " (" + m.Signature + ")"))}]; removed recovery [{string.Join(", ", s.Position.Essence.Recovery)}]";
            string Remaining(IReadOnlyDictionary<int, IReadOnlyList<string>> builds, Func<Essence, bool> predicate) => string.Join(", ",
                builds.OrderBy(p => p.Key).SelectMany(p => p.Value.Select((id, i) => (p.Key, Index: i, Essence: essences[id])))
                    .Where(p => predicate(p.Essence)).Select(p => $"{p.Key}:{p.Index + 1}={p.Essence.Definition.Id}[{string.Join("/", p.Essence.Recovery)}]"));
            var note = $"{Policy}; anchor {anchor.Id}; counter intents [{string.Join(", ", boss.CounterIntents.Order(StringComparer.Ordinal))}]. {question} "
                + $"A: {Describe(a)}. B: {Describe(b)}. "
                + $"Anchor healing-denial suppliers [{Remaining(anchor.Builds, e => e.Suppression.Length > 0)}]. "
                + $"Anchor recovery routes [{Remaining(anchor.Builds, e => e.Recovery.Length > 0)}]. "
                + $"Combination remaining recovery routes [{Remaining(combined, e => e.Recovery.Length > 0)}]. "
                + "Selection uses typed activation references, local trigger/target/condition eligibility, matched protection operation/recipient or direct damage type, then absent-in-anchor preference and a stable policy/anchor/slot/essence hash; no numeric power ranking or combat outcomes. "
                + "The legacy enabler/consumer fields name inserted A/B essences; these are whole-essence matched substitutions, not proof of enabler/consumer synergy. Other effects, cooldowns, targeting, existing suppliers and equipment/base regeneration remain confounders. "
                + "Recovery screening includes Heal (direct, periodic and event-triggered), damage lifesteal, positive healing-received modifiers/Recovery, regeneration rate/interval/attribute/Renewal, and uncertain health-restoration routes; mere status reads are excluded. "
                + "Measure victories and paired guardian progress alongside guardian healing, party direct/periodic/lifesteal healing and regeneration separately. Barriers, raw healing or reduced boss healing do not establish deaths prevented; detailed event attribution remains required.";
            return new("planned", a.Insert.Definition.Id, b.Insert.Definition.Id, parties, note);
        }
        return Unsupported("No two independent, informative and source-family-legal substitutions satisfy the declared mechanism cohorts (feedback requires both a direct recovery and a regeneration source).");
    }

    private static string[] RecoveryRoutes(IEnumerable<Mechanic> effects)
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var m in effects)
        {
            if (m.Operation == "Heal") result.Add(Number(m.Spec, "durationTicks") > 0 && Number(m.Spec, "intervalTicks") > 0 ? "periodic-heal" : "direct-heal");
            if (Number(m.Spec, "lifeStealPercentage") > 0 || Number(m.Spec, "healingScalingCoefficient") > 0) result.Add("lifesteal");
            if (m.Operation == "ModifyHealingReceived" && (Number(m.Spec, "baseValue") > 0 || VariableMagnitude(m.Spec))
                || m.Operation is "ApplyCondition" or "ApplyRandomCondition" && (m.Condition == "Recovery" || String(m.Spec, "alternativeCondition") == "Recovery")) result.Add("healing-amplification");
            if (m.Operation == "ModifyRegenerationRate" && (Number(m.Spec, "baseValue") > 0 || VariableMagnitude(m.Spec))
                || m.Operation == "ModifyRegenerationInterval" && (Number(m.Spec, "baseValue") < 0 || VariableMagnitude(m.Spec))
                || m.Operation is "ApplyCondition" or "ApplyRandomCondition" && (m.Condition == "Renewal" || String(m.Spec, "alternativeCondition") == "Renewal")
                || String(m.Spec, "attribute") == "HealthRegeneration") result.Add("regeneration");
            if (m.Operation == "SwapHealth" || m.Operation == "RestoreResource" && String(m.Spec, "resource") == "Health") result.Add("unknown-health-route");
        }
        return result.ToArray();
    }

    private static bool IsProtection(Mechanic m) => !Enemy(m.Target) &&
        (m.Operation is "GrantBarrier" or "GrantCover"
        || m.Operation == "ModifyDamageTaken" && Number(m.Spec, "baseValue") < 0
        || m.Operation == "ApplyCondition" && m.Condition is "Guard" or "Ward");
    private static bool Enemy(string target) => target.Contains("Enemy", StringComparison.Ordinal)
        || target.Contains("Enemies", StringComparison.Ordinal) || target == "CurrentTarget";
    private static IReadOnlyDictionary<int, IReadOnlyList<string>> Apply(PartyChoice anchor, params Swap[] swaps)
    {
        var builds = anchor.Builds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value.ToArray());
        foreach (var swap in swaps) builds[swap.Position.Member][swap.Position.Index] = swap.Insert.Definition.Id;
        return builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value);
    }
    private static bool Legal(IReadOnlyDictionary<int, IReadOnlyList<string>> builds, IReadOnlyDictionary<string, string> families) =>
        builds.Values.All(ids => ids.Distinct(StringComparer.Ordinal).Count() == ids.Count
            && ids.All(families.ContainsKey) && ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Count);
    private static IEnumerable<JsonElement> Items(JsonElement value, string property) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray() : [];
    private static string String(JsonElement value, string property) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString()! : "";
    private static double Number(JsonElement value, string property, double fallback = 0) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.Number ? item.GetDouble() : fallback;
    private static bool VariableMagnitude(JsonElement value) => new[] { "scalingCoefficient", "eventMagnitudeCoefficient", "conditionScalingCoefficient", "statusScalingCoefficient", "ownedSummonScalingCoefficient" }
        .Any(property => Number(value, property) != 0);
}
