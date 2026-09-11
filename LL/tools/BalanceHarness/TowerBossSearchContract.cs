using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossObjectiveContract(int SchemaVersion, IReadOnlyDictionary<int, double> TargetFloorWeights,
    string ContextPolicy, string StrategyIntent);
public sealed record BossRefinementOptions(string AnchorId,
    string DiagnosticPolicy = "boss-mechanic-replacements-v2", string ProposalPolicy = "anchor-neighborhood-v2",
    string? ReferenceSetId = null, string? ReferenceEvidenceStatus = null);
public sealed record TowerBossSearchDefinition(int SchemaVersion, string Id, BossObjectiveContract Objective,
    TowerSearchBudget Budget, IReadOnlyList<int> GenerationSeeds, int CandidatesPerArm, int DiscoverySamples,
    int ConfirmationSamples, int DiscoverySeed, int ConfirmationSeed, int DiagnosticSeed, int DiagnosticSamples,
    int MaximumBattles, IReadOnlyList<int> ExcludedCombatSeeds, IReadOnlyList<string> AllowedEssences,
    IReadOnlyList<int> MutablePartySlots, IReadOnlyList<PartyChoice> Controls, int Finalists,
    IReadOnlyList<string> Methods, IReadOnlyList<string> ContextIds, IReadOnlyDictionary<int, int> ContextCounts,
    string OwnershipAssumption = "Hypothetical ownership; acquisition is not modeled.",
    string Progression = "level-1-unascended", string OrderPolicy = "preserve-slots",
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossRefinementOptions? Refinement = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ValidationReferenceHash = null);
public sealed record BossContextAlias(int Floor, string Context, string EvaluatedContext);
public sealed record BossDiagnosticPlan(string Status, string? Enabler, string? Consumer,
    IReadOnlyList<PartyChoice> Parties, string Note);
public sealed record TowerBossSearchReport(string Status, int PlannedMaximum, int ActualBattles, int CacheHits,
    IReadOnlyList<BossSearchResult> Arms, IReadOnlyList<BossMeasurement> Discovery,
    IReadOnlyList<PartyChoice> Selection, IReadOnlyList<BossMeasurement> Confirmation,
    IReadOnlyList<BossStrategy> StrategyArchive, IReadOnlyList<BossContextAlias> ContextAliases,
    IReadOnlyList<BossMeasurement> Diagnostics);

/// <summary>A separately versioned offline objective. Historical search schemas keep their original meaning.</summary>
public static partial class TowerBossSearch
{
    public const string Version = "tower-boss-search-v1";
    public const string RefinementVersion = "tower-boss-search-v2";
    public static string Algorithm(TowerBossSearchDefinition d) => d.SchemaVersion == 1 ? Version : RefinementVersion;
    public static readonly string[] Intents = ["any", "focused-damage", "add-clearing", "protection", "sustain", "denial"];
    public static readonly string[] Methods = ["random", "legacy", "joint", "graph"];
    public static TowerBossSearchDefinition Read(string path) => JsonSerializer.Deserialize<TowerBossSearchDefinition>(
        File.ReadAllText(path), new JsonSerializerOptions(HarnessJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
        ?? throw new InvalidDataException("Empty boss search definition.");

    public static TowerBossSearchDefinition Definition(string root, string catalogs, int floor, int slots, int seed,
        string intent, string effort, IReadOnlyList<int>? excludedSeeds = null, bool refinement = true)
    {
        if (floor is < 1 or > 15 || !Intents.Contains(intent) || effort is not ("coverage" or "thorough"))
            throw new InvalidDataException("Choose a released boss, supported strategy intent and coverage or thorough sampling.");
        var historical = TowerWholeParty.Definition(root, catalogs, slots, seed, excludedSeeds);
        var budget = historical.Budget! with { PriorityFloor = floor };
        var scenarios = TowerPartyProgression.Scenarios(root, catalogs, budget);
        var mutable = scenarios.Single(s => s.FloorNumber == floor).Party.Select(p => p.PartySlot).Order().ToArray();
        var controls = historical.WholeParty!.Controls.Select(p => TowerPartySelection.Choice(p.Source,
            TowerWholeParty.Deploy(p.Builds, p.Builds, "repeat", mutable.Length))).DistinctBy(p => p.Id).ToArray();
        int Seed(string label) => StableRandom.Seed(Version, seed.ToString(CultureInfo.InvariantCulture), label);
        var content = new OfflineContent(root, TowerBundle.ReadSettings(root).Threat);
        var contexts = BuildContexts(root, catalogs, budget, mutable);
        var aliases = Aliases(contexts, controls[0]);
        var counts = aliases.GroupBy(a => a.Floor).ToDictionary(g => g.Key, g => g.Select(a => a.EvaluatedContext).Distinct().Count());
        var d = new TowerBossSearchDefinition(1, $"boss-{floor}-slots-{slots}", new(1, new Dictionary<int, double> { [floor] = 1 },
            "worst-context-paired-gain", intent), budget, [Seed("generation-a"), Seed("generation-b")],
            controls.Length + (effort == "thorough" ? 12 : 4), effort == "thorough" ? 4 : 2,
            effort == "thorough" ? 20 : 5, Seed("discovery"), Seed("confirmation"), Seed("diagnostic"),
            effort == "thorough" ? 4 : 1, 100000, historical.ExcludedCombatSeeds,
            content.Essences.GetAll().Select(e => e.Id).Order(StringComparer.Ordinal).ToArray(), mutable, controls,
            controls.Length + Methods.Length * 2 + 5, Methods, contexts.Keys.ToArray(), counts);
        if (refinement)
        {
            var validation = TowerBossValidationReferences.Read(catalogs);
            if (validation is not null)
                d = d with { ValidationReferenceHash = HarnessJson.FileHash(Path.Combine(catalogs, TowerBossValidationReferences.FileName)),
                    ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(validation.ExcludedCombatSeeds).Distinct().Order().ToArray() };
            var references = TowerBossReferences.Load(root, catalogs, floor, slots);
            if (references is not null)
                d = d with { Controls = references.Controls,
                    ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(references.ExcludedCombatSeeds).Distinct().Order().ToArray() };
            d = d with { SchemaVersion = 2, Refinement = new(references?.AnchorId ?? d.Controls[0].Id,
                ReferenceSetId: references?.ReferenceSetId, ReferenceEvidenceStatus: references?.EvidenceStatus),
                CandidatesPerArm = d.Controls.Count + (effort == "thorough" ? 24 : 8),
                Finalists = d.Controls.Count + d.Methods.Count * d.GenerationSeeds.Count + 5 };
        }
        d = d with { MaximumBattles = Cost(d) };
        Validate(d); return d;
    }

    public static int[] CombatSeeds(TowerBossSearchDefinition d) =>
        Schedule(d.DiscoverySeed, d.DiscoverySamples).Where(p => d.Objective.TargetFloorWeights.ContainsKey(p.Key)).SelectMany(p => p.Value)
        .Concat(Schedule(d.ConfirmationSeed, d.ConfirmationSamples).Values.SelectMany(p => p))
        .Concat(Schedule(d.DiagnosticSeed, d.DiagnosticSamples).Where(p => d.Objective.TargetFloorWeights.ContainsKey(p.Key)).SelectMany(p => p.Value)).ToArray();

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> Schedule(int seed, int count) => TowerLoadoutPilot.Schedule(seed, count);
    private static int Cost(TowerBossSearchDefinition d) => checked(
        d.Methods.Count * d.GenerationSeeds.Count * d.CandidatesPerArm * d.DiscoverySamples
            * d.Objective.TargetFloorWeights.Keys.Sum(f => d.ContextCounts[f])
        + d.Finalists * d.ConfirmationSamples * d.ContextCounts.Values.Sum()
        + 4 * d.DiagnosticSamples * d.Objective.TargetFloorWeights.Keys.Sum(f => d.ContextCounts[f]));

    public static int Validate(TowerBossSearchDefinition d)
    {
        var b = d.Budget;
        if (d.SchemaVersion is not (1 or 2) || !TowerBenchmark.SafeId(d.Id) || d.Objective is null || d.Objective.SchemaVersion != 1
            || d.Objective.ContextPolicy != "worst-context-paired-gain" || !Intents.Contains(d.Objective.StrategyIntent)
            || d.Objective.TargetFloorWeights is not { Count: > 0 and <= 15 }
            || d.Objective.TargetFloorWeights.Any(p => p.Key is < 1 or > 15 || !double.IsFinite(p.Value) || p.Value <= 0)
            || !double.IsFinite(d.Objective.TargetFloorWeights.Values.Sum())
            || b is null || b.EssenceSlots is < 4 or > 10 || b != (TowerPartyProgression.Budget(b.EssenceSlots) with { PriorityFloor = b.PriorityFloor })
            || !d.Objective.TargetFloorWeights.ContainsKey(b.PriorityFloor)
            || d.GenerationSeeds is not { Count: > 0 and <= 4 } || d.GenerationSeeds.Distinct().Count() != d.GenerationSeeds.Count
            || d.Methods is null || !d.Methods.SequenceEqual(Methods)
            || d.CandidatesPerArm is < 2 or > 100 || d.DiscoverySamples is < 1 or > 20
            || d.ConfirmationSamples is < 1 or > 100 || d.DiagnosticSamples is < 1 or > 20
            || d.MaximumBattles is < 1 or > 100000 || d.ExcludedCombatSeeds is null || d.ExcludedCombatSeeds.Count > 100000
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count
            || d.AllowedEssences is not { Count: >= 4 and <= 1000 } || d.AllowedEssences.Any(string.IsNullOrWhiteSpace)
            || d.AllowedEssences.Distinct(StringComparer.Ordinal).Count() != d.AllowedEssences.Count
            || d.MutablePartySlots is not { Count: > 0 and <= 50 } || d.MutablePartySlots.Any(s => s is < 1 or > 50)
            || !d.MutablePartySlots.SequenceEqual(d.MutablePartySlots.Distinct().Order())
            || d.ContextIds is not { Count: 2 } || d.ContextIds.Any(string.IsNullOrWhiteSpace) || d.ContextIds.Distinct().Count() != 2
            || d.ContextCounts is null || !d.ContextCounts.Keys.Order().SequenceEqual(Enumerable.Range(1, 15))
            || d.ContextCounts.Values.Any(n => n is < 1 or > 2)
            || d.Controls is not { Count: > 0 } || d.Controls.Count > (d.SchemaVersion == 1 ? 32 : 64) || d.Controls[0].Source != "control"
            || d.Controls.Select(p => p.Id).Distinct().Count() != d.Controls.Count
            || d.Controls.Any(p => p.Id != HarnessJson.Hash(p.Builds) || !p.Builds.Keys.Order().SequenceEqual(d.MutablePartySlots)
                || p.Builds.Values.Any(ids => ids.Count != b.EssenceSlots || ids.Distinct().Count() != ids.Count || ids.Except(d.AllowedEssences).Any()))
            || d.CandidatesPerArm <= d.Controls.Count || d.Finalists < d.Controls.Count + d.Methods.Count * d.GenerationSeeds.Count + 5
            || d.Finalists > (d.SchemaVersion == 1 ? 64 : 96)
            || (d.SchemaVersion == 1 && d.Refinement is not null)
            || (d.ValidationReferenceHash is not null && (d.SchemaVersion != 2 || d.ValidationReferenceHash.Length != 64
                || d.ValidationReferenceHash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))))
            || (d.SchemaVersion == 2 && (d.Refinement is null || !d.Controls.Any(c => c.Id == d.Refinement.AnchorId)
                || d.Refinement.DiagnosticPolicy != "boss-mechanic-replacements-v2" || d.Refinement.ProposalPolicy != "anchor-neighborhood-v2"
                || (d.Refinement.ReferenceSetId is null) != (d.Refinement.ReferenceEvidenceStatus is null)
                || (d.Refinement.ReferenceSetId is not null && (!TowerBenchmark.SafeId(d.Refinement.ReferenceSetId)
                    || string.IsNullOrWhiteSpace(d.Refinement.ReferenceEvidenceStatus)))))
            || string.IsNullOrWhiteSpace(d.OwnershipAssumption) || d.Progression != "level-1-unascended" || d.OrderPolicy != "preserve-slots")
            throw new InvalidDataException("Invalid boss objective, fixed budget, legal pool, mutable party, control/strategy allocation or bounded search contract.");
        var seeds = CombatSeeds(d);
        if (seeds.Distinct().Count() != seeds.Length || seeds.Intersect(d.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Discovery, confirmation, diagnostic and excluded combat seeds must be disjoint.");
        var cost = Cost(d);
        if (cost > d.MaximumBattles) throw new InvalidDataException($"Planned {cost} actual combats exceed hard cap {d.MaximumBattles}.");
        return cost;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(string root, string catalogs, TowerBossSearchDefinition d)
    {
        var contexts = BuildContexts(root, catalogs, d.Budget, d.MutablePartySlots);
        if (!contexts.Keys.SequenceEqual(d.ContextIds) || d.MutablePartySlots.Except(contexts.Values.First()
            .Where(s => d.Objective.TargetFloorWeights.ContainsKey(s.FloorNumber)).SelectMany(s => s.Party.Select(p => p.PartySlot))).Any())
            throw new InvalidDataException("Declared contexts or mutable party slots differ from released Tower content.");
        var aliases = Aliases(contexts, d.Controls[0]);
        if (aliases.GroupBy(a => a.Floor).Any(g => g.Select(a => a.EvaluatedContext).Distinct().Count() != d.ContextCounts[g.Key]))
            throw new InvalidDataException("Declared distinct context costs differ from actual parties.");
        return contexts;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> BuildContexts(string root, string catalogs,
        TowerSearchBudget budget, IReadOnlyList<int> mutable)
    {
        var contexts = TowerPartyProgression.Contexts(root, catalogs, 0, budget);
        var baseline = contexts.Values.First();
        return contexts.ToDictionary(c => c.Key, c => (IReadOnlyList<TowerScenario>)c.Value.Select(s => s with {
            Id = $"boss-search-floor-{s.FloorNumber}",
            Assumptions = ["Boss search: fixed identities, gear, training and required Tower party size; mutable ordered Essences declared in definition.json.",
                "Hypothetical ownership; level-1 unascended/unevolved Essences; Combat Styles disabled."],
            Party = s.Party.Select(p => mutable.Contains(p.PartySlot)
                ? baseline.Single(o => o.FloorNumber == s.FloorNumber).Party.Single(o => o.PartySlot == p.PartySlot) : p).ToArray()
        }).ToArray());
    }

    public static IReadOnlyList<BossContextAlias> Aliases(IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> contexts, PartyChoice control)
    {
        var aliases = new List<BossContextAlias>();
        foreach (var floor in Enumerable.Range(1, 15))
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var context in contexts)
            {
                var scenario = TowerPartySelection.Apply(context.Value.Single(s => s.FloorNumber == floor), control.Builds, [1]);
                var hash = HarnessJson.Hash(scenario);
                if (!seen.TryGetValue(hash, out var canonical)) seen.Add(hash, canonical = context.Key);
                aliases.Add(new(floor, context.Key, canonical));
            }
        }
        return aliases;
    }
}
