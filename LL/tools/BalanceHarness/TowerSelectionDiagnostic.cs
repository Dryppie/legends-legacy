using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace BalanceHarness;

public sealed record TowerDiagnosticPhaseLimit(int Seconds, long Bytes);
public sealed record TowerSelectionDiagnosticRequest(string Version, TowerPracticalRequest Operation,
    IReadOnlyDictionary<string, TowerDiagnosticPhaseLimit> Phases)
{
    // Auditor-local relocation; not part of serialized requests or their identity.
    internal string? ArchiveRoot { get; init; }
}
public sealed record TowerDiagnosticSearchBinding(string Version, string RequestHash, string TemplateHash, TowerBossDiscoveryDefinition Definition);
public sealed record TowerDiagnosticNominee(int Ordinal, string PartyId, string RecipeHash, TowerScenario Scenario,
    IReadOnlyList<string> ReferenceIds, string SelectionHash);
public sealed record TowerDiagnosticFreeze(string Version, string RequestHash, string SearchBindingHash,
    string DiscoveryHash, string SelectionHash, string AttemptsHash, int CompletedAttempts, string PrimaryId,
    IReadOnlyList<TowerDiagnosticNominee> Nominees);
public sealed record TowerDiagnosticCell(string PartyId, IReadOnlyList<TowerBalanceTrial> Trials);
public sealed record TowerDiagnosticStudy(string Version, BossGenerationResult Discovery,
    IReadOnlyList<BossDiscoveryMeasurement> Selection, TowerDiagnosticFreeze Freeze, IReadOnlyList<TowerDiagnosticCell> Evidence);
public sealed record TowerDiagnosticRate(string PartyId, int Wins, RateEstimate Estimate);
public sealed record TowerDiagnosticContrast(string OtherPartyId, int Gains, int Losses, double ObservedGain,
    double Lower, double Upper, bool Qualifies);
public sealed record TowerDiagnosticResult(string Version, string ExecutionStatus, string IntegrityStatus,
    string DiagnosticDecision, string? PrimaryId, IReadOnlyList<TowerDiagnosticRate> Rates,
    IReadOnlyList<TowerDiagnosticContrast> Contrasts, IReadOnlyList<string> RecommendedPartyIds,
    string BalanceAssessment, string SamplingAssumption, string? StudyHash, string? ArchiveHash, string StopReason);

public static partial class TowerSelectionDiagnostic
{
    public const string Version = "tower-practical-selection-diagnostic-v1";
    internal const int Samples = 1000, TotalFights = 4640, SearchFights = 640, Family = 10, EntropyBytes = 8192;
    internal static readonly string[] PhaseNames = ["admission", "search", "confirmation", "audit"];
    internal const string SamplingAssumption = "Single post-freeze cryptographic byte batch modeled as independent uniform bits; approximate Wilson family coverage; operational completion is not assumed.";
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
    internal static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;
    internal static string P(TowerSelectionDiagnosticRequest q, string name) => Path.Combine(q.ArchiveRoot ?? q.Operation.OutputRoot, name);

    internal static void ValidateRequest(TowerSelectionDiagnosticRequest q, bool paths = false)
    {
        Require(q is not null && q.Version == Version && q.Operation is not null && q.Phases is not null, "Unknown diagnostic request.");
        var o = q.Operation;
        if (paths) TowerPracticalSearch.ValidateRequest(o); else TowerPracticalSearch.ValidateRequestContract(o);
        Require(o.Version == TowerPracticalSearch.AllocationVersion && o.Allocation is { DiscoverySamples: 8, SelectionSamples: 32, ConfirmationSamples: Samples }
            && q.Phases.Keys.Order().SequenceEqual(PhaseNames.Order())
            && q.Phases.Values.All(p => p is not null && p.Seconds >= 5 && p.Bytes >= 65536)
            && q.Phases.Values.Sum(p => (long)p.Seconds) + TowerPracticalSearch.CloseoutSeconds <= o.MaximumSeconds - o.PriorSeconds
            && q.Phases.Values.Sum(p => p.Bytes) + TowerPracticalSearch.CloseoutBytes <= o.MaximumBytes - o.PriorBytes,
            "Diagnostic requires fixed counts and explicit nontransferable phase limits inside the cumulative allowance.");
    }

    internal static void ValidateDefinition(TowerBossDiscoveryDefinition d, bool template)
    {
        TowerBossDiscovery.ValidateDiagnosticPhase(d, template);
        var s = d.Stages.Schedules.Single().Value;
        Require(d.RequiredPartySize == 10 && d.Budget is { CharacterLevel: 40, EssenceSlots: 5 } && d.OwnedCopies is null
            && d.Generation is { CandidatesPerArm: 64, MaximumAttemptsPerArm: 256 }
            && d.MaximumBattles == TotalFights && d.ExcludedCombatSeeds.Count <= TowerStudyLimits.HistoricalSeeds - 2089
            && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order())
            && s.Discovery.Count == (template ? 0 : 8) && s.Selection.Count == (template ? 0 : 32)
            && s.Confirmation.Count == 0 && s.Diagnostics.Count == 0 && s.Feedback is null,
            "Diagnostic requires the frozen ten-character, five-Essence cohort and exact phase shape.");
        var reserved = d.Generation.Seeds.Concat(s.Discovery).Concat(s.Selection).ToArray();
        Require(reserved.Distinct().Count() == reserved.Length && !reserved.Intersect(d.ExcludedCombatSeeds).Any(), "Overlapping search reservations.");
    }

    internal static TowerDiagnosticFreeze Freeze(TowerDiagnosticSearchBinding binding, BossGenerationResult discovery,
        IReadOnlyList<BossDiscoveryMeasurement> selection, BossGenerationMechanics mechanics, string attemptsHash)
    {
        var d = binding.Definition; ValidateDefinition(d, false);
        Require(binding.Version == Version && TowerContractJson.Hash(binding.RequestHash) && TowerContractJson.Hash(binding.TemplateHash)
            && TowerContractJson.Hash(attemptsHash) && discovery.Status == "Complete"
            && discovery.Arms.Count == 1 && discovery.Arms[0].Evaluations.Count == 64
            && discovery.DiscoveryShortlist.Count == 4 && selection.Count == 4, "Incomplete search cannot freeze a diagnostic.");
        var input = TowerBossDiscovery.CopyGenerationInputs(d);
        var panel = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection);
        var primary = TowerBossStudyPolicy.Select(input, mechanics, discovery.DiscoveryShortlist, selection, panel, 1, d.Stages.SelectionPolicyVersion).Single();
        var nominees = discovery.DiscoveryShortlist.Select((party, ordinal) => {
            var scenario = TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, party, []);
            return new TowerDiagnosticNominee(ordinal, party.Id, TowerBossDiscovery.RecipeHash(scenario.Party), scenario,
                d.Starts.Where(s => s.Party.Id == party.Id).Select(s => s.ReferenceId).Order(StringComparer.Ordinal).ToArray(),
                HarnessJson.Hash(selection.Single(s => s.Id == party.Id)));
        }).ToArray();
        Require(nominees.Select(n => n.RecipeHash).Distinct().Count() == 4
            && nominees.SelectMany(n => n.ReferenceIds).Order().SequenceEqual(d.References.Select(r => r.Id).Order()), "Changed nominees or missing anchors.");
        return new(Version, binding.RequestHash, HarnessJson.Hash(binding), HarnessJson.Hash(discovery), HarnessJson.Hash(selection),
            attemptsHash, SearchFights, primary.Party.Id, nominees);
    }

    // Shared live/recorded execution. The entropy callback is called only after the complete nominee freeze.
    internal static async Task<TowerDiagnosticStudy> Execute(TowerDiagnosticSearchBinding binding, BossGenerationMechanics mechanics,
        TowerBossDiscoveryRun.Battle battle, Action<string, object> freeze, Func<TowerDiagnosticFreeze, int[]> confirmation,
        Func<string> attemptsHash, Action<bool> attempt, Action<string> phase, CancellationToken ct)
    {
        var d = binding.Definition; ValidateDefinition(d, false);
        var input = TowerBossDiscovery.CopyGenerationInputs(d); var completed = 0; var started = 0;
        async Task<(LoadoutTrial Trial, TowerBattleReport Report)> Fight(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Require(started == completed && started < TotalFights, "Attempt cap or unmatched attempt.");
            attempt(false); started++;
            var value = await battle(arm, stage, scenario, seed, token);
            Require(value.Trial.Stage == stage && value.Trial.Seed == seed && value.Trial.Recipe == HarnessJson.Hash(scenario)
                && value.Report.Battle.Seed == seed && value.Report.Battle.ScenarioId == scenario.Id
                && Enum.IsDefined(value.Report.Battle.Summary.ContentOutcome)
                && value.Report.Succeeded == (value.Report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Inconsistent trial evidence.");
            attempt(true); completed++; return value;
        }
        phase("search");
        var discovery = await TowerSuppliedCompositionSearch.RunDiagnosticAsync(binding, mechanics,
            (party, arm, token) => TowerBossDiscoveryRun.Measure(d, input, party, arm, Fight, token), ct);
        freeze("discovery.json", discovery);
        Require(discovery.Status == "Complete" && completed == 512, "Incomplete discovery; no diagnostic refill.");
        freeze("discovery-shortlist.json", discovery.DiscoveryShortlist);
        var selection = new List<BossDiscoveryMeasurement>();
        var selectionInput = input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection) };
        foreach (var party in discovery.DiscoveryShortlist)
            selection.Add(await TowerBossDiscoveryRun.Measure(d, selectionInput, party, "selection", Fight, ct, "selection"));
        Require(completed == SearchFights, "Incomplete selection."); freeze("selection-results.json", selection);
        var frozen = Freeze(binding, discovery, selection, mechanics, attemptsHash());
        freeze("nominees-freeze.json", frozen);
        phase("confirmation");
        var seeds = confirmation(frozen);
        var search = d.Generation.Seeds.Concat(d.Stages.Schedules.Single().Value.Discovery).Concat(d.Stages.Schedules.Single().Value.Selection);
        Require(seeds.Length == Samples && seeds.Distinct().Count() == Samples && !seeds.Intersect(d.ExcludedCombatSeeds.Concat(search)).Any(), "Invalid independent panel.");
        var evidence = new List<TowerDiagnosticCell>();
        foreach (var nominee in frozen.Nominees)
        {
            var scenario = nominee.Scenario with { Seeds = seeds };
            var trials = new List<TowerBalanceTrial>();
            foreach (var seed in seeds)
            {
                var result = await Fight("confirmation/" + nominee.PartyId, "confirmation", scenario, seed, ct);
                trials.Add(new(seed, result.Report.Battle.Summary.ContentOutcome));
            }
            evidence.Add(new(nominee.PartyId, trials));
        }
        Require(started == TotalFights && completed == TotalFights, "Incomplete diagnostic.");
        var study = new TowerDiagnosticStudy(Version, discovery, selection, frozen, evidence);
        freeze("study.json", study); return study;
    }

    internal static TowerDiagnosticResult Assess(TowerDiagnosticStudy study, string archiveHash)
    {
        var f = study.Freeze;
        Require(study.Version == Version && f.Version == Version && f.CompletedAttempts == SearchFights
            && TowerContractJson.Hash(archiveHash) && f.Nominees.Count == 4 && study.Evidence.Count == 4
            && f.Nominees.Select(n => n.Ordinal).SequenceEqual(Enumerable.Range(0, 4))
            && f.Nominees.Select(n => n.PartyId).Distinct().Count() == 4
            && f.Nominees.Select(n => n.RecipeHash).Distinct().Count() == 4
            && f.Nominees.Select(n => n.PartyId).SequenceEqual(study.Evidence.Select(e => e.PartyId))
            && f.Nominees.Count(n => n.PartyId == f.PrimaryId) == 1, "Changed diagnostic family.");
        var panel = study.Evidence[0].Trials.Select(t => t.Seed).ToArray();
        Require(panel.Length == Samples && panel.Distinct().Count() == Samples
            && study.Evidence.All(e => e.Trials.Select(t => t.Seed).SequenceEqual(panel) && e.Trials.All(t => Enum.IsDefined(t.Outcome))), "Incomplete paired panel.");
        var wins = study.Evidence.ToDictionary(e => e.PartyId, e => e.Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray());
        var rates = study.Evidence.Select(e => new TowerDiagnosticRate(e.PartyId, wins[e.PartyId].Count(w => w),
            TowerBalanceEvaluator.Wilson(wins[e.PartyId].Count(w => w), Samples, Family)!)).ToArray();
        var contrasts = f.Nominees.Where(n => n.PartyId != f.PrimaryId).Select(n => {
            var pairs = wins[n.PartyId].Zip(wins[f.PrimaryId]).ToArray();
            var gains = pairs.Count(p => p.First && !p.Second); var losses = pairs.Count(p => !p.First && p.Second);
            var g = TowerBalanceEvaluator.Wilson(gains, Samples, Family)!; var l = TowerBalanceEvaluator.Wilson(losses, Samples, Family)!;
            return new TowerDiagnosticContrast(n.PartyId, gains, losses, (gains-losses)/(double)Samples, g.Lower-l.Upper, g.Upper-l.Lower,
                rates.Single(r => r.PartyId == n.PartyId).Estimate.Lower >= .10 && 20*(gains-losses) >= Samples && g.Lower-l.Upper > 0);
        }).ToArray();
        return new(Version, "Complete", "Verified", contrasts.Any(c => c.Qualifies) ? "SelectionMissDemonstrated" : "NoSelectionMissDemonstrated",
            f.PrimaryId, rates, contrasts, f.Nominees.Where(n => n.ReferenceIds.Count > 0).Select(n => n.PartyId).ToArray(),
            "NotAssessed", SamplingAssumption, HarnessJson.Hash(study), archiveHash, "Frozen diagnostic completed; no promotion, retry or extension.");
    }

    internal static object Export(TowerDiagnosticStudy study) => new { version = Version, primaryId = study.Freeze.PrimaryId, adoption = "Hold",
        teams = study.Freeze.Nominees.Select(n => new { n.PartyId, primary = n.PartyId == study.Freeze.PrimaryId, n.ReferenceIds,
            recommended = n.ReferenceIds.Count > 0, scenario = n.Scenario with { Seeds = [] },
            subgroups = n.Scenario.Party.ToDictionary(p => p.PartySlot, p => WorldTowerPartyRules.GetPartyNumber(p.PartySlot)),
            requiredCopies = n.Scenario.Party.SelectMany(p => p.Build.EssenceIds).GroupBy(e => e).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()) }).ToArray() };

    internal static string Markdown(TowerDiagnosticResult result, TowerDiagnosticStudy study)
    {
        var text = new StringBuilder("# Practical Tower selection diagnostic\n\n");
        text.AppendLine($"Decision: **{result.DiagnosticDecision}**. Frozen primary: `{result.PrimaryId}`. Adoption: **Hold**.");
        text.AppendLine("\nFour nominees; 1,000 paired trials each; approximate family-ten Wilson intervals. Negative evidence does not establish equivalence. Both anchors remain recommended. This diagnostic does not establish method reliability or encounter balance.");
        text.AppendLine("\n| Other nominee | Gains / losses | Observed gain | Adjusted paired interval |\n| --- | ---: | ---: | ---: |");
        foreach (var c in result.Contrasts) text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {c.OtherPartyId} | {c.Gains} / {c.Losses} | {100*c.ObservedGain:F2} pp | {100*c.Lower:F2} to {100*c.Upper:F2} pp |"));
        text.AppendLine("\n[All rates and decisions](result.json) · [Seed-free equipment and team exports](teams.json).\n\nSampling assumption: " + SamplingAssumption);
        foreach (var n in study.Freeze.Nominees)
        {
            text.AppendLine($"\n## {n.PartyId}\n\n| Character | Subgroup | Essences in fixed order |\n| ---: | ---: | --- |");
            foreach (var p in n.Scenario.Party) text.AppendLine($"| {p.PartySlot} | {WorldTowerPartyRules.GetPartyNumber(p.PartySlot)} | {string.Join(", ", p.Build.EssenceIds)} |");
        }
        return text.ToString();
    }
}
