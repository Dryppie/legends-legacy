using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

internal sealed record TowerRecognitionEnvelope(int NativeSeconds, long NativeBytes, int AuditSeconds, long AuditBytes)
{
    internal int Seconds => NativeSeconds + AuditSeconds;
    internal long Bytes => NativeBytes + AuditBytes;
}
public sealed record TowerRecognitionLinearEstimate(double Mean, double SamplingVariance, double SamplingStandardError,
    double CombatVariance, double CombatStandardError);
public sealed record TowerPairedRecognitionSummary(TowerRecognitionLinearEstimate Control, TowerRecognitionLinearEstimate Candidate,
    TowerRecognitionLinearEstimate Difference, TowerRecognitionLinearEstimate ControlNominees, TowerRecognitionLinearEstimate CandidateNominees,
    TowerRecognitionLinearEstimate ControlChallenger, TowerRecognitionLinearEstimate CandidateChallenger,
    double SamplingCovariance, double CombatCovariance);
public sealed record TowerPairedRecognitionRoot(int Root, string BenchmarkPartyId, TowerPairedRecognitionSummary Estimates);
public sealed record TowerPairedRecognitionCell(int Root, string PartyId, string Stratum, string Membership,
    bool Measured, JsonElement InclusionProbability, JsonElement Provenance);
public sealed record TowerPairedRecognition(IReadOnlyList<TowerPairedRecognitionRoot> Roots, TowerPairedRecognitionSummary AllRoots,
    IReadOnlyList<TowerPairedRecognitionCell> Cells);

public static partial class TowerFixedFamilyConfirmation
{
    public const string PreservationRecognitionVersion = "tower-affinity-preservation-recognition-v1";
    internal const string PreservationRecognitionPlanHash = "8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6";
    internal const string PreservationRecognitionTeamsHash = "2b77f4d612e3adef132bd68abe221971dbd4481e8850c97818a02887b41822c3";
    // Exact root boundaries of the externally pinned plan, not a reusable cohort default.
    internal static IReadOnlyList<int> PreservationRootSizes { get; } = Array.AsReadOnly(new[] { 13,13,12,13,11,11,12,12,11,12,12,13 });
    internal const string PreservationRecognitionLimitations = "Development diagnosis of twelve frozen paired pools only. Approximate family-799 Wilson intervals describe individual combat contrasts. Sampling variances condition on fixed full-panel outcomes; combat variances condition on the selected sample and retain shared-seed covariance. Report these components separately, never add them as a combined variance. Nominee summaries average all five frozen nominees. All-root summaries average twelve fixed roots, not future-root effects. Unknown outcomes remain null; no full-pool maximum, qualification, selector fitting, policy promotion or default change.";

    internal static TowerRecognitionEnvelope RecognitionLimits(string version)
    {
        Require(IsRecognition(version), "Unknown recognition limits.");
        if (version == NeighborhoodRecognitionVersion) return new(14400,16L*1073741824,3600,1073741824);
        return version == PreservationRecognitionVersion ? new(9000,5632L*1048576,1800,512L*1048576)
            : new(6000,3072L*1048576,1200,512L*1048576);
    }

    internal static int RecognitionRoot(string version, int ordinal)
    {
        Require(IsRecognition(version) && ordinal >= 0 && ordinal < Policy(version).Teams, "Invalid recognition ordinal.");
        if (version == NeighborhoodRecognitionVersion) return 0;
        if (version != PreservationRecognitionVersion) return ordinal/9;
        var offset = 0;
        for (var root = 0; root < PreservationRootSizes.Count; root++)
        {
            offset += PreservationRootSizes[root];
            if (ordinal < offset) return root;
        }
        throw new InvalidDataException("Unresolved paired root.");
    }

    internal static double RecognitionCovariance(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        Require(a.Count == b.Count && a.Count > 1 && a.All(double.IsFinite) && b.All(double.IsFinite), "Invalid covariance sample.");
        var am = a.Average(); var bm = b.Average();
        return a.Zip(b).Sum(p => (p.First-am)*(p.Second-bm))/(a.Count-1);
    }

    private static double Inclusion(JsonElement team)
    {
        var p = team.GetProperty("inclusionProbability");
        var n = p.GetProperty("numerator").GetInt32(); var d = p.GetProperty("denominator").GetInt32();
        Require(n > 0 && n <= d, "Invalid inclusion probability.");
        return (double)n/d;
    }

    // Coefficients belong to finite-population estimands. Inverse inclusion weights
    // enter only the observed estimator; the design variance uses unweighted z_i.
    internal static double RecognitionSamplingCovariance(JsonElement root, double[][] gains, double[] a, double[] b)
    {
        var teams = root.GetProperty("teams").EnumerateArray().ToArray(); var total = 0d;
        foreach (var stratum in root.GetProperty("strata").EnumerateObject())
        {
            var n = stratum.Value.GetProperty("populationCount").GetInt32();
            var k = stratum.Value.GetProperty("sampleCount").GetInt32();
            var indices = Enumerable.Range(0,teams.Length).Where(i => teams[i].GetProperty("stratum").GetString() == stratum.Name).ToArray();
            Require(indices.Length == k && k >= 0 && k <= n && (k == n || k >= 2), "Invalid sampled stratum.");
            if (k == n) continue;
            var x = indices.Select(i => a[i]*gains[i].Average()).ToArray();
            var y = indices.Select(i => b[i]*gains[i].Average()).ToArray();
            total += n*(double)n*(1-(double)k/n)*RecognitionCovariance(x,y)/k;
        }
        return total;
    }

    private static (TowerRecognitionLinearEstimate Estimate, double[] Trials) EstimateRecognition(
        JsonElement root, double[][] gains, double[] coefficients)
    {
        var teams = root.GetProperty("teams").EnumerateArray().ToArray();
        var trials = Enumerable.Range(0,256).Select(s => Enumerable.Range(0,teams.Length)
            .Sum(i => coefficients[i]/Inclusion(teams[i])*gains[i][s])).ToArray();
        var sampling = RecognitionSamplingCovariance(root,gains,coefficients,coefficients);
        var combat = RecognitionCovariance(trials,trials)/256;
        return (new(trials.Average(),sampling,Math.Sqrt(sampling),combat,Math.Sqrt(combat)),trials);
    }

    private static double[] RecognitionCoefficients(JsonElement[] teams, string arm, string field)
        => teams.Select(t => {
            var p = t.GetProperty("provenance").GetProperty(arm);
            if (p.ValueKind == JsonValueKind.Null) return 0d;
            return field switch {
                "generated" => p.GetProperty(field).GetBoolean() ? 1d/17 : 0,
                "nomineeRank" => p.GetProperty(field).ValueKind != JsonValueKind.Null ? 1d/5 : 0,
                "validationChallenger" => p.GetProperty(field).GetBoolean() ? 1d : 0,
                _ => throw new InvalidDataException("Unknown paired estimand.")
            };
        }).ToArray();

    private static TowerPairedRecognitionSummary RecognitionRootEstimates(JsonElement root, double[][] gains)
    {
        var teams = root.GetProperty("teams").EnumerateArray().ToArray();
        var a = RecognitionCoefficients(teams,"control","generated"); var b = RecognitionCoefficients(teams,"candidate","generated");
        var control = EstimateRecognition(root,gains,a); var candidate = EstimateRecognition(root,gains,b);
        var difference = EstimateRecognition(root,gains,a.Zip(b).Select(p => p.Second-p.First).ToArray());
        return new(control.Estimate,candidate.Estimate,difference.Estimate,
            EstimateRecognition(root,gains,RecognitionCoefficients(teams,"control","nomineeRank")).Estimate,
            EstimateRecognition(root,gains,RecognitionCoefficients(teams,"candidate","nomineeRank")).Estimate,
            EstimateRecognition(root,gains,RecognitionCoefficients(teams,"control","validationChallenger")).Estimate,
            EstimateRecognition(root,gains,RecognitionCoefficients(teams,"candidate","validationChallenger")).Estimate,
            RecognitionSamplingCovariance(root,gains,a,b),RecognitionCovariance(control.Trials,candidate.Trials)/256);
    }

    private static TowerPairedRecognitionSummary AverageRecognition(IReadOnlyList<TowerPairedRecognitionRoot> roots)
    {
        TowerRecognitionLinearEstimate Mean(Func<TowerPairedRecognitionSummary,TowerRecognitionLinearEstimate> select)
        {
            var rows = roots.Select(r => select(r.Estimates)).ToArray();
            var sampling = rows.Sum(x => x.SamplingVariance)/144; var combat = rows.Sum(x => x.CombatVariance)/144;
            return new(rows.Average(x => x.Mean),sampling,Math.Sqrt(sampling),combat,Math.Sqrt(combat));
        }
        return new(Mean(x => x.Control),Mean(x => x.Candidate),Mean(x => x.Difference),Mean(x => x.ControlNominees),
            Mean(x => x.CandidateNominees),Mean(x => x.ControlChallenger),Mean(x => x.CandidateChallenger),
            roots.Sum(r => r.Estimates.SamplingCovariance)/144,roots.Sum(r => r.Estimates.CombatCovariance)/144);
    }

    internal static TowerRecognitionResult AssessPreservationRecognition(TowerFixedFamilyStudy study, string planPath, string archiveHash)
    {
        var plan = RecognitionPlan(planPath,PreservationRecognitionVersion); var d = study.Freeze.Definition; ValidateDefinition(d);
        Require(study.Version == PreservationRecognitionVersion && study.Freeze.Version == study.Version && d.Version == study.Version
            && TowerContractJson.Hash(study.Freeze.RequestHash) && TowerContractJson.Hash(study.Freeze.DefinitionHash)
            && TowerContractJson.Hash(archiveHash) && HarnessJson.Hash(d.Teams) == HarnessJson.Hash(RecognitionTeams(plan)), "Changed paired diagnostic scope.");
        var roots = plan.GetProperty("roots").EnumerateArray().ToArray();
        Require(roots.Length == 12 && roots.Select(r => r.GetProperty("teams").GetArrayLength()).SequenceEqual(PreservationRootSizes)
            && study.Evidence.Count == 145 && study.Evidence.Select(e => e.PartyId).SequenceEqual(d.Teams.Select((t,i) => CellId(d,i))), "Changed paired cells or root boundaries.");
        var offsets = new int[12]; for (var r = 1; r < 12; r++) offsets[r] = offsets[r-1]+PreservationRootSizes[r-1];
        var panel = offsets.SelectMany(i => study.Evidence[i].Trials.Select(t => t.Seed)).ToArray(); Chunks(d,panel);
        var wins = new List<bool[]>();
        for (var i = 0; i < 145; i++)
        {
            var row = study.Evidence[i].Trials; var root = RecognitionRoot(d.Version,i);
            Require(row.Count == 256 && row.Select(t => t.Seed).SequenceEqual(panel.Skip(root*256).Take(256))
                && row.All(t => t.Outcome is BattleOutcome.Victory or BattleOutcome.Defeat or BattleOutcome.Draw), "Changed paired panel or outcome.");
            wins.Add(row.Select(t => t.Outcome == BattleOutcome.Victory).ToArray());
        }
        var rates = d.Teams.Select((t,i) => new TowerRecognitionRate(RecognitionRoot(d.Version,i)+1,t.PartyId,t.Role,wins[i].Count(w => w),
            TowerBalanceEvaluator.Wilson(wins[i].Count(w => w),256,799)!)).ToArray();
        var contrasts = new List<TowerRecognitionContrast>(); var strata = new List<TowerRecognitionStratum>();
        var unmeasured = new List<TowerRecognitionUnmeasured>(); var pairedRoots = new List<TowerPairedRecognitionRoot>(); var cells = new List<TowerPairedRecognitionCell>();
        for (var r = 0; r < 12; r++)
        {
            var root = roots[r]; var teams = root.GetProperty("teams").EnumerateArray().ToArray(); var start = offsets[r];
            var benchmark = root.GetProperty("benchmarkPartyId").GetString()!;
            var benchmarkIndex = Array.FindIndex(teams,t => t.GetProperty("partyId").GetString() == benchmark);
            Require(benchmarkIndex is >= 0 and < 3, "Lost paired benchmark.");
            var gains = Enumerable.Range(0,teams.Length).Select(i => wins[start+i].Zip(wins[start+benchmarkIndex])
                .Select(p => (p.First ? 1d : 0)-(p.Second ? 1d : 0)).ToArray()).ToArray();
            for (var c = 3; c < teams.Length; c++)
            for (var reference = 0; reference < 3; reference++)
            {
                var pairs = wins[start+c].Zip(wins[start+reference]).ToArray();
                var g = pairs.Count(p => p.First && !p.Second); var l = pairs.Count(p => p.Second && !p.First);
                var gi = TowerBalanceEvaluator.Wilson(g,256,799)!; var li = TowerBalanceEvaluator.Wilson(l,256,799)!;
                contrasts.Add(new(r+1,d.Teams[start+c].PartyId,d.Teams[start+reference].PartyId,d.Teams[start+c].Role,g,l,(g-l)/256d,gi.Lower-li.Upper,gi.Upper-li.Lower));
            }
            foreach (var name in new[] { "mandatory", "remaining-shared", "remaining-control-only", "remaining-candidate-only" })
            {
                var indices = Enumerable.Range(0,teams.Length).Where(i => teams[i].GetProperty("stratum").GetString() == name).ToArray();
                var n = name == "mandatory" ? indices.Length : root.GetProperty("strata").GetProperty(name).GetProperty("populationCount").GetInt32();
                var total = indices.Sum(i => gains[i].Average()); var weight = indices.Length == 0 ? 0 : (double)n/indices.Length;
                strata.Add(new(r+1,name,indices.Length,n,weight,indices.Length == 0 ? 0 : total/indices.Length,total*weight));
            }
            pairedRoots.Add(new(r+1,benchmark,RecognitionRootEstimates(root,gains)));
            foreach (var item in root.GetProperty("unmeasured").EnumerateArray())
                unmeasured.Add(new(r+1,item.GetProperty("partyId").GetString()!,item.GetProperty("stratum").GetString()!,null));
            foreach (var (items,measured) in new[] { (root.GetProperty("teams"),true),(root.GetProperty("unmeasured"),false) })
            foreach (var item in items.EnumerateArray())
                cells.Add(new(r+1,item.GetProperty("partyId").GetString()!,item.GetProperty("stratum").GetString()!,item.GetProperty("membership").GetString()!,
                    measured,item.GetProperty("inclusionProbability").Clone(),item.GetProperty("provenance").Clone()));
        }
        return new(study.Version,"Complete","Verified","CompleteDiagnosticOnly","DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion",false,
            256,799,rates,contrasts,strata,[],unmeasured,PreservationRecognitionLimitations,HarnessJson.Hash(study),archiveHash)
            { PairedPool = new(pairedRoots,AverageRecognition(pairedRoots),cells) };
    }

    private static string PreservationRecognitionMarkdown(TowerRecognitionResult result)
    {
        var text = new StringBuilder("# Paired-pool recognition diagnostic\n\nDecision: **CompleteDiagnosticOnly**. No policy change.\n\n");
        text.AppendLine(PreservationRecognitionLimitations);
        text.AppendLine("\n145 measured cells, 327 paired contrasts, 176 unknown cells. Each root shares 256 fresh values.\n\n| Root | Original pool gain | Preserving pool gain | Difference | Sampling SE of difference | Conditional combat SE of difference |\n| --- | ---: | ---: | ---: | ---: | ---: |");
        foreach (var root in result.PairedPool!.Roots)
        {
            var e = root.Estimates;
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,$"| {root.Root} | {100*e.Control.Mean:F3} pp | {100*e.Candidate.Mean:F3} pp | {100*e.Difference.Mean:F3} pp | {100*e.Difference.SamplingStandardError:F3} pp | {100*e.Difference.CombatStandardError:F3} pp |"));
        }
        text.AppendLine("\n[All rates, weighted estimates, covariance, provenance and unknown outcomes](result.json) · [Exact measured scenarios](teams.json).\n\nThe two uncertainty components are separate; neither is a combined confidence interval. Shared cells cancel from the paired pool difference. No full-pool best-team or future-root claim.");
        return text.ToString();
    }
}
