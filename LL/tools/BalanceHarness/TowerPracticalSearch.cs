using System.Globalization;
using System.Text;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace BalanceHarness;

public sealed record TowerPracticalContrast(string ReferenceId, int Gains, int Losses,
    double ObservedGain, double Lower, double Upper);
public sealed record TowerPracticalResult(string Version, string ExecutionStatus, string IntegrityStatus,
    string StrengthDecision, string? SelectedPartyId, IReadOnlyList<string> SelectedReferenceAncestry,
    IReadOnlyList<string> RecommendedPartyIds, RateEstimate? SelectedRate,
    IReadOnlyList<TowerPracticalContrast> Contrasts, GoalOutcome? BalanceAssessment,
    string StopReason, string? StudyHash, string? ArchiveHash);
public sealed record TowerPracticalTeam(string PartyId, string Role, bool Recommended, TowerScenario Scenario,
    IReadOnlyDictionary<int, int> Subgroups, IReadOnlyDictionary<string, int> RequiredCopies,
    IReadOnlyList<string> ReferenceIds, IReadOnlyList<string> SuppliedAncestry);
public sealed record TowerPracticalTeams(string Version, string StrengthDecision, string IntegrityStatus,
    string StudyHash, string ArchiveHash, IReadOnlyDictionary<string, string> ContentHashes,
    string SettingsHash, string ExecutionHash, IReadOnlyList<TowerPracticalTeam> Teams);

/// <summary>A practical result contract over the unchanged incumbent kernel. Never infers authentication from JSON alone.</summary>
public static partial class TowerPracticalSearch
{
    public const string Version = "tower-practical-search-v1";
    internal const int IntervalFamily = 7; // Frozen pilot gate: three rates and two gain/loss contrasts.
    internal static void Require(bool valid, string message)
    { if (!valid) throw new InvalidDataException(message); }

    internal static TowerBossDiscoveryDefinition Prepare(TowerBossDiscoveryDefinition source)
    {
        var d = source.Mode == TowerBossDiscovery.Independent
            ? TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray(),
                TowerSuppliedCompositionSearch.IncumbentVersion) : source;
        TowerBossDiscovery.Validate(d);
        Require(d.Mode == TowerBossDiscovery.Improve && TowerSuppliedCompositionSearch.PreservesIncumbents(d.Generation.PolicyVersion),
            "Practical search requires an incumbent, racing or anchored-neighborhood policy, or a compatible independent source definition.");
        var scheduled = Reserved(d);
        Require(scheduled.Distinct().Count() == scheduled.Length && !scheduled.Intersect(d.ExcludedCombatSeeds).Any(),
            "Construction and combat schedules must be mutually distinct and absent from the complete historical union.");
        Require(d.Stages.Schedules.Single().Value.Confirmation.Count >= 256,
            "Practical improvement requires at least 256 prospectively declared confirmation trials; no automatic sample extension.");
        return d;
    }

    internal static int[] Reserved(TowerBossDiscoveryDefinition d) => d.Generation.Seeds.Concat(
        d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation))).ToArray();

    internal static TowerPracticalResult Unverified(string execution, string integrity, string reason,
        BossStudyReport? report = null) => new(Version, execution, integrity,
            execution is "Incomplete" or "Cancelled" ? "IncompleteEvidence" : "IntegrityFailure", null, [], [], null, [],
            report?.Balance?.Assessment, reason, report is null ? null : HarnessJson.Hash(report), null);

    // Only called after the production verifier returns the exact recorded report. Internal for literal-evidence fixtures.
    internal static TowerPracticalResult Assess(TowerBossDiscoveryDefinition d, BossStudyReport report, string archiveHash)
    {
        Prepare(d);
        Require(TowerContractJson.Hash(archiveHash), "Missing verified archive identity.");
        if (report.Status != "Complete") return Unverified(report.Status, "NotVerified", report.Error ?? report.Status, report);
        Require(report.ExitCode is 0 or 1 or 3 && report.Confirmation is not null && report.Discovery?.Status == "Complete"
            && report.Balance is not null && report.Accounting.Attempted.All(p => report.Accounting.Completed[p.Key] == p.Value),
            "Incomplete practical execution cannot establish improvement.");
        var family = report.Confirmation!;
        var primary = family.Members.Single(m => m.Primary);
        var selected = primary.GeneratedIds.Single();
        Require(family.Members.Count is 2 or 3 && family.Members.SelectMany(m => m.ReferenceIds).Order(StringComparer.Ordinal)
            .SequenceEqual(d.References.Select(r => r.Id).Order(StringComparer.Ordinal)), "Changed practical confirmation family.");
        var schedule = d.Stages.Schedules.Single().Value.Confirmation;
        bool[] Wins(BossConfirmationMember member)
        {
            var evidence = report.Evidence.Single(e => e.CellId == member.CellId);
            Require(evidence.Status == "Complete" && evidence.Trials.Select(t => t.Seed).SequenceEqual(schedule)
                && evidence.Trials.All(t => Enum.IsDefined(t.Outcome)), "Missing, reordered or invalid paired confirmation evidence.");
            return evidence.Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray();
        }
        var wins = Wins(primary); var n = wins.Length;
        var rate = TowerBalanceEvaluator.Wilson(wins.Count(w => w), n, IntervalFamily)!;
        var contrasts = d.References.Select(reference => {
            var anchor = Wins(family.Members.Single(m => m.ReferenceIds.Contains(reference.Id)));
            var gained = wins.Zip(anchor).Count(p => p.First && !p.Second);
            var lost = wins.Zip(anchor).Count(p => !p.First && p.Second);
            var g = TowerBalanceEvaluator.Wilson(gained, n, IntervalFamily)!;
            var l = TowerBalanceEvaluator.Wilson(lost, n, IntervalFamily)!;
            return new TowerPracticalContrast(reference.Id, gained, lost, (gained - lost) / (double)n,
                g.Lower - l.Upper, g.Upper - l.Lower);
        }).ToArray();
        var novel = primary.ReferenceIds.Count == 0;
        var improved = novel && rate.Lower >= .10 && contrasts.All(c => c.ObservedGain >= .05 && c.Lower > 0);
        var decision = !novel ? "IncumbentRetained" : improved ? "DemonstratedImprovement" : "ImprovementNotDemonstrated";
        var ancestry = Ancestry(report, selected);
        var recommended = improved ? new[] { selected } : d.Starts.Select(s => s.Party.Id).ToArray();
        return new(Version, report.Status, "Verified", decision, selected, ancestry, recommended, rate, contrasts,
            report.Balance!.Assessment, string.Join(", ", report.Discovery!.Arms.Select(a => a.StopReason).Distinct()),
            HarnessJson.Hash(report), archiveHash);
    }

    private static string[] Ancestry(BossStudyReport report, string party) => report.Discovery!.Arms.SelectMany(a => a.Proposals)
        .Where(p => p.Party?.Id == party && p.Result == "evaluated").SelectMany(p => p.Provenance.ReferenceIds)
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    internal static TowerPracticalTeams Export(TowerBossDiscoveryDefinition d, BossStudyReport report, TowerPracticalResult result)
    {
        Require(result.IntegrityStatus == "Verified" && result.StudyHash == HarnessJson.Hash(report)
            && TowerContractJson.Hash(result.ArchiveHash), "Unverified evidence cannot produce recommended team exports.");
        var teams = report.Confirmation!.Members.Select(member => {
            var scenario = report.Confirmation.Definition.Cells.Single(c => c.Id == member.CellId).Scenario with { Seeds = [] };
            var party = TowerPartySelection.Choice("practical-export", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            TowerBossDiscovery.ValidateParty(d, party);
            Require(scenario.Party.All(p => TowerCompositionSearch.IsCanonical(p.Build.EssenceIds)), "Noncanonical export.");
            var role = member.ReferenceIds.Count > 0 ? "Reference" : result.StrengthDecision == "DemonstratedImprovement"
                ? "ImprovedCandidate" : "MeasuredChallenger";
            return new TowerPracticalTeam(party.Id, role, result.RecommendedPartyIds.Contains(party.Id), scenario,
                scenario.Party.ToDictionary(p => p.PartySlot, p => WorldTowerPartyRules.GetPartyNumber(p.PartySlot)),
                scenario.Party.SelectMany(p => p.Build.EssenceIds).GroupBy(id => id).OrderBy(g => g.Key, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Count()), member.ReferenceIds, Ancestry(report, party.Id));
        }).ToArray();
        return new(Version, result.StrengthDecision, result.IntegrityStatus, result.StudyHash!, result.ArchiveHash!,
            d.ContentHashes, d.SettingsHash, d.ExecutionHash, teams);
    }

    internal static string Markdown(TowerPracticalResult result, TowerPracticalTeams? export,
        IReadOnlyDictionary<string, string>? essenceNames = null)
    {
        var text = new StringBuilder("# Practical Tower search\n\n");
        text.AppendLine($"Execution: **{result.ExecutionStatus}**. Evidence: **{result.IntegrityStatus}**. Strength: **{result.StrengthDecision}**.");
        text.AppendLine($"\nEncounter balance: **{result.BalanceAssessment?.ToString() ?? "Unavailable"}**. Stop: {result.StopReason}.");
        text.AppendLine("\nThis search uses two supplied references with fixed ability order. Supplied ancestry is not independent discovery. Balance and team strength are separate decisions.");
        if (result.IntegrityStatus != "Verified") { text.AppendLine("\nNo team is promoted from incomplete or unverified evidence."); return text.ToString(); }
        text.AppendLine("\nIndependent confirmation requires at least five percentage points of observed gain and a positive adjusted paired lower bound versus both references, plus supported 10% viability. The approximate Bonferroni-Wilson family contains seven quantities; failure does not prove equivalence.");
        text.AppendLine("\n| Reference | Gained / lost wins | Observed gain | Adjusted paired interval |\n| --- | ---: | ---: | ---: |");
        foreach (var c in result.Contrasts) text.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {c.ReferenceId} | {c.Gains} / {c.Losses} | {100*c.ObservedGain:F2} pp | {100*c.Lower:F2} to {100*c.Upper:F2} pp |"));
        text.AppendLine("\nExact recipes and equipment: [seed-free team sheet](teams.json). Evidence: [study](study/study.json), [archive manifest](study/files.json), [verification](verification.json). Historical schedules are not instructions to rerun a study.");
        foreach (var team in export?.Teams ?? [])
        {
            text.AppendLine($"\n## {team.Role}: {team.PartyId}\n\nRecommended reference/output: **{team.Recommended}**. Floor {team.Scenario.FloorNumber}; fixed progression and equipment are recorded in teams.json.");
            text.AppendLine("\n| Character | Subgroup | Essences in fixed order |\n| ---: | ---: | --- |");
            foreach (var p in team.Scenario.Party.OrderBy(p => p.PartySlot)) text.AppendLine($"| {p.PartySlot} | {team.Subgroups[p.PartySlot]} | {string.Join(", ", p.Build.EssenceIds.Select(id => essenceNames?.GetValueOrDefault(id) is { } name ? name + " (`" + id + "`)" : "`" + id + "`"))} |");
            text.AppendLine("\nRequired copies: " + string.Join(", ", team.RequiredCopies.Select(p => $"{p.Key} ×{p.Value}")) + ".");
        }
        return text.ToString();
    }
}
