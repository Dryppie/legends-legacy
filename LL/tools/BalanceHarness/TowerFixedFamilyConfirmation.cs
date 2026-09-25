using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace BalanceHarness;

public sealed record TowerFixedFamily(string Role, string PartyId, IReadOnlyList<string> ReferenceIds, TowerScenario Scenario);
public sealed record TowerFixedFamilyCharge(string Scope, double Seconds, long Bytes, string ReceiptPath, string ReceiptHash);
public sealed record TowerFixedFamilyDefinition(string Version, IReadOnlyList<TowerFixedFamily> Teams,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, string ExecutionHash, IReadOnlyList<int> ExcludedCombatSeeds);
public sealed partial record TowerFixedFamilyRequest(string Version, string ContentRoot, string DefinitionPath, string DefinitionHash,
    string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    int MaximumSeconds, long MaximumBytes, IReadOnlyDictionary<string, TowerDiagnosticPhaseLimit> Phases,
    double PriorSeconds, long PriorBytes, IReadOnlyList<TowerFixedFamilyCharge> PriorCharges,
    IReadOnlyDictionary<string, string>? PendingHistoryRecoveries = null, IReadOnlyDictionary<string, string>? RecoveryReceiptHashes = null)
{
    internal string? ArchiveRoot { get; init; }
}
internal sealed record TowerFixedFamilyInputs(TowerFixedFamilyDefinition Definition, TowerRefinementLiveHistory History);
public sealed record TowerFixedFamilyFreeze(string Version, string RequestHash, string DefinitionHash, TowerFixedFamilyDefinition Definition);
public sealed record TowerFixedFamilyChunk(int TeamOrdinal, int SliceOrdinal, string PartyId, string SeedFreeHash, TowerScenario Scenario);
public sealed record TowerFixedFamilyStudy(string Version, TowerFixedFamilyFreeze Freeze, IReadOnlyList<TowerDiagnosticCell> Evidence);
public sealed record TowerFixedFamilyContrast(string CandidateId, string ReferenceId, int Gains, int Losses,
    double ObservedGain, double Lower, double Upper, bool Qualifies);
public sealed record TowerFixedFamilyResult(string Version, string ExecutionStatus, string IntegrityStatus, string StrengthDecision,
    string Adoption, IReadOnlyList<string> CandidateIds, IReadOnlyList<string> QualifyingPartyIds,
    IReadOnlyList<TowerDiagnosticRate> Rates, IReadOnlyList<TowerFixedFamilyContrast> Contrasts,
    IReadOnlyList<string> RecommendedPartyIds, IReadOnlyList<string> ControlPartyIds, string BalanceAssessment,
    string SamplingAssumption, string? StudyHash, string? ArchiveHash, string StopReason);

/// <summary>One fixed family, one entropy batch and one final strength decision. No search or recovery route.</summary>
public static partial class TowerFixedFamilyConfirmation
{
    public const string Version = "tower-practical-fixed-family-confirmation-v1";
    internal const int Samples = 5500, CandidateCount = 6, TeamCount = 8, TotalFights = 44000, Family = 32, EntropyBytes = 44000;
    internal const int CloseoutSeconds = 60, EnclosingSeconds = 300;
    internal const long CloseoutBytes = 16L*1048576, EnclosingBytes = 240L*1048576;
    internal static readonly string[] PhaseNames = ["admission", "combat", "audit"];
    internal static readonly int[] SliceSizes = [1000, 1000, 1000, 1000, 1000, 500];
    // Canonical hashes of the exact seed-free family and content in the frozen planning artifact.
    // Runtime and complete history are bound later, at admission; recipes and settings cannot change.
    internal const string TeamsHash = "6cec8956d42a175044731021d9395ade5eca8275986892bfa94a248b54cc5d8d";
    internal const string ContentHash = "34efd7e17756456f245724b271f4c40a5e0f87dcbc626363ed558c1bb4343995";
    internal const string SettingsHash = "f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74";
    internal const string SamplingAssumption = "One post-freeze cryptographic batch modeled as independent uniform bits; approximate family-32 Wilson coverage; operational completion is not assumed.";
    internal const string StopReason = "One complete fixed panel; report every qualifier in frozen order; no new primary, retry, extension, method-reliability or global-optimality claim.";
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string message)
    { if (!ok) throw new InvalidDataException(message); }
    internal static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;
    internal static string P(TowerFixedFamilyRequest q, string name) => Path.Combine(q.ArchiveRoot ?? q.OutputRoot, name);
    internal static void Match(TowerFixedFamilyRequest q, string name, object value)
        => Require(HarnessJson.Hash(HarnessJson.Read<JsonElement>(P(q, name))) == HarnessJson.Hash(value), "Changed fixed-family artifact: " + name);

    internal static void ValidateDefinition(TowerFixedFamilyDefinition d)
    {
        Require(d is not null && d.Teams is not null && d.Teams.Count == Policy(d.Version).Teams && HarnessJson.Hash(d.Teams) == Policy(d.Version).TeamsHash
            && d.ContentHashes is not null && HarnessJson.Hash(d.ContentHashes) == ContentHash && d.SettingsHash == SettingsHash
            && TowerContractJson.Hash(d.ExecutionHash) && d.ExcludedCombatSeeds is { Count: > 0 }
            && d.ExcludedCombatSeeds.Count <= TowerStudyLimits.HistoricalSeeds - Policy(d.Version).EntropyBytes/4
            && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order()),
            "Changed fixed family, cohort, content, settings, runtime identity or complete sorted history.");
    }

    internal static IReadOnlyList<TowerFixedFamilyChunk> Chunks(TowerFixedFamilyDefinition d, IReadOnlyList<int> panel)
    {
        ValidateDefinition(d); var policy = Policy(d.Version);
        Require(panel.Count == policy.PanelValues && panel.Distinct().Count() == policy.PanelValues && !panel.Intersect(d.ExcludedCombatSeeds).Any(), "Invalid fixed paired panel.");
        return d.Teams.SelectMany((team, ordinal) => policy.Slices.Select((size, slice) => new TowerFixedFamilyChunk(ordinal, slice,
            CellId(d, ordinal), HarnessJson.Hash(team.Scenario), team.Scenario with { Seeds = panel.Skip((IsRecognition(d.Version) ? RecognitionRoot(d.Version,ordinal)*256 : 0)+slice*1000).Take(size).ToArray() }))).ToArray();
    }

    internal static string CellId(TowerFixedFamilyDefinition d, int ordinal)
        => IsRecognition(d.Version) && d.Version != NeighborhoodRecognitionVersion ? $"r{RecognitionRoot(d.Version,ordinal)+1:D2}-"+d.Teams[ordinal].PartyId : d.Teams[ordinal].PartyId;

    internal static async Task<TowerFixedFamilyStudy> Execute(TowerFixedFamilyFreeze freeze, IReadOnlyList<int> panel,
        TowerBossDiscoveryRun.Battle battle, Action<bool> attempt, CancellationToken ct)
    {
        Require(freeze.Version == freeze.Definition.Version && TowerContractJson.Hash(freeze.RequestHash) && TowerContractJson.Hash(freeze.DefinitionHash), "Changed freeze.");
        var policy = Policy(freeze.Version); var chunks = Chunks(freeze.Definition, panel); var evidence = new List<TowerDiagnosticCell>(); var ordinal = 0;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var team in chunks.GroupBy(c => c.TeamOrdinal))
        {
            var trials = new List<TowerBalanceTrial>();
            foreach (var chunk in team)
            {
                var recipeHash = HarnessJson.Hash(chunk.Scenario);
                foreach (var seed in chunk.Scenario.Seeds)
                {
                    ct.ThrowIfCancellationRequested(); Require(ordinal < policy.Fights, "Attempt cap reached."); attempt(false);
                    var (trial, report) = await battle("confirmation/"+chunk.PartyId, "confirmation", chunk.Scenario, seed, ct);
                    Require(trial.Id == $"trial-{ordinal+1:D6}" && trial.Stage == "confirmation" && trial.Seed == seed
                        && trial.Recipe == recipeHash && TowerContractJson.Hash(trial.InputHash)
                        && TowerContractJson.Hash(trial.CacheKey) && keys.Add(trial.CacheKey), "Changed, reused or reordered trial.");
                    ValidateReport(report, chunk.Scenario, seed);
                    trials.Add(new(seed, report.Battle.Summary.ContentOutcome)); attempt(true); ordinal++;
                }
            }
            evidence.Add(new(team.First().PartyId, trials));
        }
        Require(ordinal == policy.Fights, "Incomplete fixed confirmation.");
        return new(freeze.Version, freeze, evidence);
    }

    internal static void ValidateReport(TowerBattleReport report, TowerScenario scenario, int seed)
        => Require(report.Battle.Seed == seed && report.Battle.ScenarioId == scenario.Id
            && Enum.IsDefined(report.Battle.Summary.ContentOutcome)
            && report.Succeeded == (report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Inconsistent report binding or outcome.");

    internal static TowerFixedFamilyResult Assess(TowerFixedFamilyStudy study, string archiveHash)
    {
        Require(!IsRecognition(study.Version), "Recognition has no qualification or adoption endpoint.");
        Require(study.Version == study.Freeze.Version && study.Version == study.Freeze.Definition.Version && TowerContractJson.Hash(archiveHash)
            && TowerContractJson.Hash(study.Freeze.RequestHash) && TowerContractJson.Hash(study.Freeze.DefinitionHash), "Changed fixed study.");
        ValidateDefinition(study.Freeze.Definition); var policy = Policy(study.Version); var teams = study.Freeze.Definition.Teams;
        Require(study.Evidence.Count == TeamCount && study.Evidence.Select(e => e.PartyId).SequenceEqual(teams.Select(t => t.PartyId)), "Changed family order.");
        var panel = study.Evidence[0].Trials.Select(t => t.Seed).ToArray(); Chunks(study.Freeze.Definition, panel);
        Require(study.Evidence.All(e => e.Trials.Select(t => t.Seed).SequenceEqual(panel) && e.Trials.All(t => Enum.IsDefined(t.Outcome))), "Incomplete paired cells.");
        var wins = study.Evidence.Select(e => e.Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray()).ToArray();
        var rates = teams.Select((t, i) => new TowerDiagnosticRate(t.PartyId, wins[i].Count(w => w),
            TowerBalanceEvaluator.Wilson(wins[i].Count(w => w), policy.Samples, policy.Family)!)).ToArray();
        var contrasts = Enumerable.Range(0, policy.Candidates).SelectMany(candidate => Enumerable.Range(policy.Candidates, TeamCount-policy.Candidates).Select(reference => {
            var pairs = wins[candidate].Zip(wins[reference]).ToArray();
            var gains = pairs.Count(p => p.First && !p.Second); var losses = pairs.Count(p => !p.First && p.Second);
            var g = TowerBalanceEvaluator.Wilson(gains, policy.Samples, policy.Family)!; var l = TowerBalanceEvaluator.Wilson(losses, policy.Samples, policy.Family)!;
            return new TowerFixedFamilyContrast(teams[candidate].PartyId, teams[reference].PartyId, gains, losses, (gains-losses)/(double)policy.Samples,
                g.Lower-l.Upper, g.Upper-l.Lower, rates[candidate].Estimate.Lower >= .10 && gains-losses >= policy.MinimumNet && g.Lower-l.Upper > 0);
        })).ToArray();
        var candidates = teams.Take(policy.Candidates).Select(t => t.PartyId).ToArray();
        var qualifiers = candidates.Where(id => contrasts.Where(c => c.CandidateId == id).All(c => c.Qualifies)).ToArray();
        var controls = teams.Skip(policy.Candidates).Select(t => t.PartyId).ToArray(); var pass = qualifiers.Length > 0;
        return new(study.Version, "Complete", "Verified", pass ? "StrongerFixedCandidatesConfirmed" : "StrengthNotDemonstrated", pass ? "RecommendFixedCandidates" : "Hold",
            candidates, qualifiers, rates, contrasts, Recommendations(study.Version, qualifiers, controls), controls, "NotAssessed", Assumption(study.Version),
            HarnessJson.Hash(study), archiveHash, StopReason);
    }

    internal static object Export(TowerFixedFamilyStudy study, TowerFixedFamilyResult result) => new { version = study.Version, result.Adoption, result.CandidateIds, result.QualifyingPartyIds,
        teams = study.Freeze.Definition.Teams.Select(t => new { t.Role, t.PartyId, t.ReferenceIds,
            qualifies = result.QualifyingPartyIds.Contains(t.PartyId),
            recommended = result.RecommendedPartyIds.Contains(t.PartyId), control = result.ControlPartyIds.Contains(t.PartyId), scenario = t.Scenario,
            subgroups = t.Scenario.Party.ToDictionary(p => p.PartySlot, p => WorldTowerPartyRules.GetPartyNumber(p.PartySlot)),
            requiredCopies = t.Scenario.Party.SelectMany(p => p.Build.EssenceIds).GroupBy(e => e).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()) }).ToArray() };

    internal static string Markdown(TowerFixedFamilyResult result)
    {
        var policy = Policy(result.Version); var text = new StringBuilder("# Fixed-family Tower confirmation\n\n");
        text.AppendLine($"Decision: **{result.StrengthDecision}**. Adoption: **{result.Adoption}**.");
        text.AppendLine(result.Version == Version ? "\nEight fixed recipes; 5,500 paired trials each; one final family-32 strength decision. Every qualifier is reported in frozen order, without choosing a new primary. Both references remain controls. Search reliability, candidate/candidate superiority and encounter balance are not assessed." : "\nEight fixed recipes; 6,500 paired trials each; one final family-38 strength decision. Every qualifier is followed by all three retained references; no new primary. Search reliability, candidate/candidate superiority and encounter balance are not assessed.");
        text.AppendLine("\n| Recipe | Wins / trials | Adjusted win-rate interval | Qualifies |\n| --- | ---: | ---: | --- |");
        foreach (var rate in result.Rates) text.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {rate.PartyId} | {rate.Wins} / {policy.Samples} | {100*rate.Estimate.Lower:F2}% to {100*rate.Estimate.Upper:F2}% | {(result.ControlPartyIds.Contains(rate.PartyId) ? "Control" : result.QualifyingPartyIds.Contains(rate.PartyId) ? "Yes" : "No")} |"));
        text.AppendLine("\n| Candidate | Reference | Gains / losses | Observed gain | Adjusted paired interval | Pass |\n| --- | --- | ---: | ---: | ---: | --- |");
        foreach (var c in result.Contrasts) text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {c.CandidateId} | {c.ReferenceId} | {c.Gains} / {c.Losses} | {100*c.ObservedGain:F2} pp | {100*c.Lower:F2} to {100*c.Upper:F2} pp | {(c.Qualifies ? "Yes" : "No")} |"));
        text.AppendLine("\n[All rates and decisions](result.json) · [Seed-free equipment and teams](teams.json).\n\nSampling assumption: " + Assumption(result.Version));
        return text.ToString();
    }
}
