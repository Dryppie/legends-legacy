using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerNeighborhoodEndpoint(string Id, double Mean, double Lower, double Upper);
public sealed record TowerNeighborhoodOutcome(string Version, string Status, IReadOnlyList<string> EligiblePartyIds,
    IReadOnlyDictionary<string, int> Wins, IReadOnlyList<TowerNeighborhoodEndpoint> Endpoints,
    bool Promoted, int AdditionalSamples, string Interpretation);

public static partial class TowerFixedFamilyConfirmation
{
    public const string NeighborhoodRecognitionVersion = "tower-affinity-neighborhood-recognition-v1";
    internal const string NeighborhoodPlanVersion = "tower-affinity-neighborhood-recognition-plan-v1";
    internal const string NeighborhoodOutcomeVersion = "tower-affinity-neighborhood-recognition-outcome-v1";
    internal const string NeighborhoodPlanHash = "a0b4a5dbc8fe7c65d4df80bfcb17b8f5623343cef7e0c1a1f2506648b3e2fc2b";
    internal const string NeighborhoodTeamsHash = "db0a0389088956d29e6f8dd1af1528b3a3bc8d927d7af580e1da801fd3d2c26c";
    internal const string NeighborhoodLimitations = "Complete captured finite neighborhood only. Equal distinct-recipe means are not proposer output performance. Two-sided Hoeffding bounds use one family of 46 fixed endpoints at alpha 0.05 over uniform fresh seeds without replacement. All 44 win rates are descriptive. The conservative 2048-value panel is not a high-power test of a true three-point gain. Report every generated recipe whose lower bound reaches three points; never promote, impute, extend, retry or generalize to future search roots.";

    internal static TowerNeighborhoodOutcome NeighborhoodOutcome(TowerFixedFamilyStudy study, JsonElement plan)
    {
        var d = study.Freeze.Definition; ValidateDefinition(d);
        Require(study.Version == NeighborhoodRecognitionVersion && study.Freeze.Version == study.Version && d.Version == study.Version
            && TowerContractJson.Hash(study.Freeze.RequestHash) && TowerContractJson.Hash(study.Freeze.DefinitionHash)
            && plan.GetProperty("version").GetString() == NeighborhoodPlanVersion
            && HarnessJson.Hash(d.Teams) == HarnessJson.Hash(RecognitionTeams(plan)), "Changed complete-neighborhood scope.");
        var teams = plan.GetProperty("teams").EnumerateArray().ToArray();
        Require(study.Evidence.Count == 44 && study.Evidence.Select(e => e.PartyId).SequenceEqual(d.Teams.Select(t => t.PartyId)),
            "Changed complete-neighborhood cell order.");
        var panel = study.Evidence[0].Trials.Select(t => t.Seed).ToArray(); Chunks(d, panel);
        var wins = new Dictionary<string,int>(StringComparer.Ordinal);
        for (var i = 0; i < 44; i++)
        {
            var row = study.Evidence[i].Trials;
            Require(row.Count == 2048 && row.Select(t => t.Seed).SequenceEqual(panel)
                && row.All(t => t.Outcome is BattleOutcome.Victory or BattleOutcome.Defeat or BattleOutcome.Draw),
                "Incomplete, mispaired or invalid neighborhood outcome.");
            wins.Add(d.Teams[i].PartyId, row.Count(t => t.Outcome == BattleOutcome.Victory));
        }
        var benchmark = plan.GetProperty("benchmarkPartyId").GetString()!;
        var old = teams.Where(t => t.GetProperty("membership").GetString() != "reference").Select(t => t.GetProperty("partyId").GetString()!).ToArray();
        var subset = teams.Where(t => t.GetProperty("membership").GetString() == "shared").Select(t => t.GetProperty("partyId").GetString()!).ToArray();
        Require(old.Length == 41 && subset.Length == 26 && !old.Contains(benchmark) && subset.All(old.Contains), "Changed neighborhood membership.");
        var endpoints = new List<TowerNeighborhoodEndpoint>();
        TowerNeighborhoodEndpoint Endpoint(string id, long numerator, long denominator, double bound)
        {
            var mean = (double)numerator/denominator;
            var radius = 2*bound*Math.Sqrt(Math.Log(2*46/.05)/(2*2048));
            return new(id, mean, Math.Max(-bound,mean-radius), Math.Min(bound,mean+radius));
        }
        foreach (var team in d.Teams.Where(t => t.PartyId != benchmark))
            endpoints.Add(Endpoint(team.PartyId,wins[team.PartyId]-wins[benchmark],2048,1));
        var a = old.Sum(id => (long)wins[id]); var b = subset.Sum(id => (long)wins[id]);
        endpoints.Add(Endpoint("controlMean",a-41L*wins[benchmark],41L*2048,1));
        endpoints.Add(Endpoint("candidateMean",b-26L*wins[benchmark],26L*2048,1));
        endpoints.Add(Endpoint("candidateMinusControlMean",41*b-26*a,41L*26*2048,15d/41));
        Require(endpoints.Count == 46, "Changed endpoint family.");
        var generated = endpoints.Where(e => old.Contains(e.Id)).ToArray();
        var eligible = generated.Where(e => e.Lower >= .03).Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
        var status = eligible.Length > 0 ? "FreshConfirmationWarranted" : generated.All(e => e.Upper < .03)
            ? "RetireBelowPracticalThreshold" : "RetireUnresolvedAtBudget";
        return new(NeighborhoodOutcomeVersion,status,eligible,wins,endpoints,false,0,"FiniteCapturedNeighborhoodOnly");
    }

    internal static TowerRecognitionResult AssessNeighborhoodRecognition(TowerFixedFamilyStudy study, string planPath, string archiveHash)
    {
        Require(TowerContractJson.Hash(archiveHash), "Invalid neighborhood archive identity.");
        var plan = RecognitionPlan(planPath,NeighborhoodRecognitionVersion);
        var outcome = NeighborhoodOutcome(study,plan);
        // Legacy Wilson/sampling fields stay empty; this profile reports only its
        // separately typed finite-neighborhood endpoint and descriptive win counts.
        return new(study.Version,"Complete","Verified",outcome.Status,"FiniteCapturedNeighborhoodOnly",false,
            2048,0,[],[],[],[],[],NeighborhoodLimitations,HarnessJson.Hash(study),archiveHash) { Neighborhood = outcome };
    }

    internal static object NeighborhoodExport(TowerFixedFamilyStudy study, TowerNeighborhoodOutcome outcome) => new {
        version = study.Version, decision = outcome.Status, interpretation = outcome.Interpretation, policyDefaultsChanged = false,
        teams = study.Freeze.Definition.Teams.Select(t => new { t.PartyId, membership = t.Role, t.Scenario }),
        outcome.EligiblePartyIds, outcome.Promoted, outcome.AdditionalSamples };

    internal static string NeighborhoodMarkdown(TowerNeighborhoodOutcome outcome)
    {
        var text = new StringBuilder("# Complete affinity neighborhood diagnostic\n\n");
        text.AppendLine($"Decision: **{outcome.Status}**. No policy promotion or additional samples.\n");
        text.AppendLine(NeighborhoodLimitations);
        text.AppendLine("\n44 physical recipes, one shared panel of 2,048 values, 90,112 fights. The benchmark is fixed before measurement. All generated qualifiers are retained for a separate fresh confirmation design.\n");
        text.AppendLine("Descriptive win rates have no additional confidence intervals. Draws count as non-wins.\n");
        text.AppendLine("| Physical recipe | Wins / 2,048 | Descriptive win rate |\n| --- | ---: | ---: |");
        foreach (var row in outcome.Wins)
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,$"| {row.Key} | {row.Value} / 2048 | {100d*row.Value/2048:F6}% |"));
        text.AppendLine("\n| Endpoint | Gain | Simultaneous lower | Simultaneous upper |\n| --- | ---: | ---: | ---: |");
        foreach (var e in outcome.Endpoints)
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,$"| {e.Id} | {100*e.Mean:F6} pp | {100*e.Lower:F6} pp | {100*e.Upper:F6} pp |"));
        text.AppendLine("\n[Every endpoint and descriptive win count](result.json) · [Exact physical recipes and eligible identities](teams.json).\n\nRetireUnresolvedAtBudget does not establish absence of useful recipes. FreshConfirmationWarranted does not qualify a team or establish future-root search reliability.");
        return text.ToString();
    }
}
