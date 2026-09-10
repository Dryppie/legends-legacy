using System.Text;
using System.Text.Json;
using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;

namespace BalanceHarness;

public sealed record EssenceMechanicEntry(string Id, string Name, string SourceMonsterId,
    IReadOnlyList<string> Signals, IReadOnlyList<AbilitySpec> Abilities, JsonElement Definition,
    string Coverage);
public sealed record EssenceMechanicsReport(int SchemaVersion, IReadOnlyDictionary<string, string> SourceHashes,
    IReadOnlyList<EssenceMechanicEntry> Essences, JsonElement Statuses, JsonElement Summons,
    IReadOnlyList<string> Limitations);

/// <summary>Auditable structured source inventory. Signals propose hypotheses; they never score power.</summary>
public static class EssenceMechanicsInventory
{
    public static EssenceMechanicsReport Create(string root, ThreatAndTankingOptions threat)
    {
        // Use the same catalog validation/compiler as combat, including reference resolution.
        new JsonAbilityCatalogProvider(new ConfigurationBuilder().Build(), root, HarnessJson.Options, threat).GetCompiledCatalog();
        var content = new OfflineContent(root, threat);
        var entries = content.Essences.GetAll().OrderBy(e => e.Id, StringComparer.Ordinal).Select(e =>
        {
            var abilities = new[] { e.ActiveAbility, e.PassiveAbility };
            if (abilities.Any(a => string.IsNullOrWhiteSpace(a.Id))) throw new InvalidDataException($"Unresolved ability on '{e.Id}'.");
            var signals = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var ability in abilities)
            {
                foreach (var tag in ability.Tags) signals.Add("tag:" + tag);
                foreach (var trigger in ability.Triggers) signals.Add("trigger:" + trigger.Event);
                foreach (var effect in ability.Effects)
                {
                    signals.Add("operation:" + effect.Operation); signals.Add("target:" + effect.Target);
                    if (effect.ScalingAttribute is { } scaling) signals.Add("scaling:" + scaling);
                    if (effect.DamageType != Domain.Models.Damages.DamageType.None) signals.Add("damage:" + effect.DamageType);
                    if (effect.Condition is { } condition) signals.Add("condition-reference:" + condition);
                    if (effect.StatusId is { } status) signals.Add("status-reference:" + status);
                    if (effect.SummonId is { } summon) signals.Add("summon-reference:" + summon);
                }
            }
            return new EssenceMechanicEntry(e.Id, e.DisplayName, e.SourceMonsterId, signals.ToArray(), abilities,
                JsonSerializer.SerializeToElement(e, HarnessJson.Options),
                "Structured direct abilities only; effective value and indirect/runtime interactions are unknown. Keep eligible for exploration.");
        }).ToArray();
        var sources = new[] { "essences/essences.json", "combat/abilities.json", "combat/statuses.json", "combat/summons.json" };
        return new(1, sources.ToDictionary(p => p, p => HarnessJson.FileHash(Path.Combine(root, "Data", p))), entries,
            HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/combat/statuses.json")),
            HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/combat/summons.json")),
            ["Authored base ability specs include coefficients, cooldown ticks, targets, triggers, conditions and references; they are not final equipment-modified combat values.",
             "Definition snapshots include attribute bonuses, ascension and evolution metadata. The foundation executes level-1 unascended builds only.",
             "Shared status/summon definitions are retained for reference inspection. Signals do not flatten transitive dependencies or infer enabler/consumer compatibility.",
             "Runtime-only rules, equipment interactions and effective healing/control/barrier attribution require separate audits. No name/description parsing or universal Essence score."]);
    }

    public static string Markdown(EssenceMechanicsReport report)
    {
        var text = new StringBuilder($"# Essence mechanics inventory\n\n{report.Essences.Count} definitions, {report.Essences.Select(e => e.SourceMonsterId).Distinct(StringComparer.OrdinalIgnoreCase).Count()} source families. Source hashes and full structured definitions are in mechanics.json.\n\n");
        foreach (var limitation in report.Limitations) text.AppendLine("- " + limitation);
        text.AppendLine("\n| Essence | Source family | Direct signals (not a score) |\n| --- | --- | --- |");
        foreach (var entry in report.Essences) text.AppendLine($"| {entry.Id} | {entry.SourceMonsterId} | {string.Join(", ", entry.Signals)} |");
        return text.ToString();
    }
}
