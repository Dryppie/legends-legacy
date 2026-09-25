using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerRecognitionRate(int Root, string PartyId, string Stratum, int Wins, RateEstimate Estimate);
public sealed record TowerRecognitionContrast(int Root, string CandidateId, string ReferenceId, string Stratum,
    int Gains, int Losses, double ObservedGain, double Lower, double Upper);
public sealed record TowerRecognitionStratum(int Root, string Stratum, int Measured, int Population,
    double InclusionWeight, double MeanGain, double EstimatedTotalGain);
public sealed record TowerRecognitionPopulation(int Root, string BenchmarkPartyId, double EstimatedCandidateMeanGain);
public sealed record TowerRecognitionUnmeasured(int Root, string PartyId, string Stratum, double? IndependentOutcome);
public sealed record TowerRecognitionResult(string Version, string ExecutionStatus, string IntegrityStatus, string Decision,
    string Interpretation, bool PolicyDefaultsChanged, int SamplesPerTeam, int ApproximateWilsonFamily,
    IReadOnlyList<TowerRecognitionRate> Rates, IReadOnlyList<TowerRecognitionContrast> Contrasts,
    IReadOnlyList<TowerRecognitionStratum> Strata, IReadOnlyList<TowerRecognitionPopulation> Populations,
    IReadOnlyList<TowerRecognitionUnmeasured> Unmeasured, string Limitations, string StudyHash, string ArchiveHash)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public TowerPairedRecognition? PairedPool { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public TowerNeighborhoodOutcome? Neighborhood { get; init; }
}

public static partial class TowerFixedFamilyConfirmation
{
    public const string RecognitionVersion = "tower-frozen-pool-recognition-v1";
    internal const string RecognitionPlanHash = "f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8";
    internal const string RecognitionTeamsHash = "1df5cf13de459812bbde36dc5c19ea0f70711a5365316203fcbbb56b956063e9";
    internal const string RecognitionLimitations = "Development diagnosis of twelve frozen roots only. Approximate family-540 Wilson intervals describe combat uncertainty. Lower-stratum sampling uncertainty is separate; weighted population summaries are point estimates without population confidence intervals. Unmeasured outcomes remain null. No qualification, adoption, future-root reliability or default-policy change.";

    internal static JsonElement RecognitionPlan(string path, string version = RecognitionVersion)
    {
        Require(HarnessJson.FileHash(path) == RecognitionPlanPin(version), "Changed frozen recognition plan.");
        return HarnessJson.Read<JsonElement>(path);
    }

    internal static TowerFixedFamily[] RecognitionTeams(JsonElement plan)
    {
        var neighborhood = plan.GetProperty("version").GetString() == NeighborhoodPlanVersion;
        var teams = neighborhood ? plan.GetProperty("teams").EnumerateArray().ToArray() : plan.GetProperty("roots").EnumerateArray()
            .SelectMany(r => r.GetProperty("teams").EnumerateArray()).ToArray();
        return teams.Select(t => new TowerFixedFamily(t.GetProperty(neighborhood ? "membership" : "stratum").GetString()!,
            t.GetProperty("partyId").GetString()!, [], t.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!)).ToArray();
    }

    // Seed-free context inspection is also used by the separately bounded admission owner.
    internal static object RecognitionContext(string content, string version = RecognitionVersion)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Context inspection cannot fight.")).Activate();
        Require(IsRecognition(version), "Unknown recognition context profile.");
        var execution = ExecutionIdentity.Current(); var settings = TowerBundle.ReadSettings(content);
        Require(HarnessJson.Hash(settings) == SettingsHash, "Changed diagnostic settings.");
        return new { version, execution, executionHash = HarnessJson.Hash(execution), settingsHash = HarnessJson.Hash(settings), fights = 0, newValues = 0 };
    }

    internal static TowerRecognitionResult AssessRecognition(TowerFixedFamilyStudy study, string planPath, string archiveHash)
    {
        if (study.Version == NeighborhoodRecognitionVersion) return AssessNeighborhoodRecognition(study,planPath,archiveHash);
        if (study.Version == PreservationRecognitionVersion) return AssessPreservationRecognition(study,planPath,archiveHash);
        var plan = RecognitionPlan(planPath, study.Version); var d = study.Freeze.Definition; ValidateDefinition(d);
        Require(IsRecognition(study.Version) && study.Freeze.Version == study.Version && d.Version == study.Version
            && TowerContractJson.Hash(study.Freeze.RequestHash) && TowerContractJson.Hash(study.Freeze.DefinitionHash)
            && TowerContractJson.Hash(archiveHash) && HarnessJson.Hash(d.Teams) == HarnessJson.Hash(RecognitionTeams(plan)), "Changed diagnostic scope.");
        Require(study.Evidence.Count == 108 && study.Evidence.Select(e => e.PartyId).SequenceEqual(d.Teams.Select((t,i) => CellId(d,i))), "Changed root-specific cell order.");
        var panel = Enumerable.Range(0,12).SelectMany(r => study.Evidence[r*9].Trials.Select(t => t.Seed)).ToArray();
        Chunks(d,panel); // Enforces all twelve panels, no cross-root reuse, and historical exclusion.
        var wins = new List<bool[]>();
        for (var i = 0; i < 108; i++)
        {
            var row = study.Evidence[i].Trials;
            Require(row.Count == 256 && row.Select(t => t.Seed).SequenceEqual(panel.Skip(i/9*256).Take(256))
                && row.All(t => t.Outcome is BattleOutcome.Victory or BattleOutcome.Defeat or BattleOutcome.Draw), "Incomplete or mispaired root cell.");
            wins.Add(row.Select(t => t.Outcome == BattleOutcome.Victory).ToArray());
        }
        var rates = d.Teams.Select((t,i) => new TowerRecognitionRate(i/9+1,t.PartyId,t.Role,wins[i].Count(w => w),
            TowerBalanceEvaluator.Wilson(wins[i].Count(w => w),256,540)!)).ToArray();
        var contrasts = new List<TowerRecognitionContrast>(); var strata = new List<TowerRecognitionStratum>();
        var populations = new List<TowerRecognitionPopulation>(); var unmeasured = new List<TowerRecognitionUnmeasured>();
        for (var root = 0; root < 12; root++)
        {
            for (var c = 3; c < 9; c++)
            for (var r = 0; r < 3; r++)
            {
                var ci = root*9+c; var ri = root*9+r; var pairs = wins[ci].Zip(wins[ri]).ToArray();
                var gains = pairs.Count(p => p.First && !p.Second); var losses = pairs.Count(p => !p.First && p.Second);
                var g = TowerBalanceEvaluator.Wilson(gains,256,540)!; var l = TowerBalanceEvaluator.Wilson(losses,256,540)!;
                contrasts.Add(new(root+1,d.Teams[ci].PartyId,d.Teams[ri].PartyId,d.Teams[ci].Role,gains,losses,(gains-losses)/256d,g.Lower-l.Upper,g.Upper-l.Lower));
            }
            var benchmark = d.Teams[root*9+2].PartyId;
            foreach (var stratum in new[] { "nominee", "near-miss", "lower" })
            {
                var gain = contrasts.Where(c => c.Root == root+1 && c.Stratum == stratum && c.ReferenceId == benchmark).Sum(c => c.ObservedGain);
                var population = stratum == "lower" ? 13 : 2; var weight = population/2d;
                strata.Add(new(root+1,stratum,2,population,weight,gain/2,weight*gain));
            }
            populations.Add(new(root+1,benchmark,strata.Where(s => s.Root == root+1).Sum(s => s.EstimatedTotalGain)/17));
            foreach (var item in plan.GetProperty("roots")[root].GetProperty("unmeasured").EnumerateArray())
                unmeasured.Add(new(root+1,item.GetProperty("partyId").GetString()!,"lower",null));
        }
        return new(study.Version,"Complete","Verified","CompleteDiagnosticOnly","DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion",false,
            256,540,rates,contrasts,strata,populations,unmeasured,RecognitionLimitations,HarnessJson.Hash(study),archiveHash);
    }

    internal static object RecognitionExport(TowerFixedFamilyStudy study, TowerRecognitionResult result) => result.Neighborhood is { } neighborhood
        ? NeighborhoodExport(study,neighborhood) : new {
        version = study.Version, result.Decision, result.Interpretation, result.PolicyDefaultsChanged,
        teams = study.Freeze.Definition.Teams.Select((t,i) => new { root = RecognitionRoot(study.Version,i)+1, cellId = CellId(study.Freeze.Definition,i), t.PartyId, stratum = t.Role, t.Scenario }), result.Unmeasured };

    internal static string RecognitionMarkdown(TowerRecognitionResult result)
    {
        if (result.Neighborhood is { } neighborhood) return NeighborhoodMarkdown(neighborhood);
        if (result.PairedPool is not null) return PreservationRecognitionMarkdown(result);
        var text = new StringBuilder("# Frozen-pool recognition diagnostic\n\nDecision: **CompleteDiagnosticOnly**. No policy change.\n\n");
        text.AppendLine(RecognitionLimitations);
        text.AppendLine("\n108 root/recipe rates and 216 signed paired contrasts; 256 common trials within each root, twelve disjoint panels. Draws are non-wins. Fixed benchmark: 96b94357.\n\n| Root | Nominee gain | Near-miss gain | Lower mean estimate | All-17 weighted mean estimate |\n| --- | ---: | ---: | ---: | ---: |");
        foreach (var p in result.Populations)
        {
            var s = result.Strata.Where(s => s.Root == p.Root).ToArray();
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,$"| {p.Root} | {100*s[0].MeanGain:F3} pp | {100*s[1].MeanGain:F3} pp | {100*s[2].MeanGain:F3} pp | {100*p.EstimatedCandidateMeanGain:F3} pp |"));
        }
        text.AppendLine("\n[Every rate, contrast and explicit unmeasured outcome](result.json) · [Exact measured teams](teams.json).\n\nPopulation estimates use (sum of four nominee/near-miss gains + 6.5 × sum of two lower gains) / 17. They carry no population confidence interval.");
        return text.ToString();
    }
}
