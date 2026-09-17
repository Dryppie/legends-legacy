using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace BalanceHarness;

public sealed record TowerFixedTeam(string Role, string PartyId, IReadOnlyList<string> ReferenceIds, TowerScenario Scenario);
public sealed record TowerFixedTeamDefinition(string Version, IReadOnlyList<TowerFixedTeam> Teams,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, string ExecutionHash, IReadOnlyList<int> ExcludedCombatSeeds);
public sealed record TowerFixedTeamRequest(string Version, string ContentRoot, string DefinitionPath, string DefinitionHash,
    string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    int MaximumSeconds, long MaximumBytes, IReadOnlyDictionary<string, TowerDiagnosticPhaseLimit> Phases,
    double PriorSeconds = 0, long PriorBytes = 0,
    IReadOnlyDictionary<string, string>? PendingHistoryRecoveries = null, IReadOnlyDictionary<string, string>? RecoveryReceiptHashes = null)
{
    internal string? ArchiveRoot { get; init; }
}
internal sealed record TowerFixedTeamInputs(TowerFixedTeamDefinition Definition, TowerRefinementLiveHistory History);
public sealed record TowerFixedTeamFreeze(string Version, string RequestHash, string DefinitionHash, TowerFixedTeamDefinition Definition);
public sealed record TowerFixedTeamChunk(int TeamOrdinal, int SliceOrdinal, string PartyId, string SeedFreeHash, TowerScenario Scenario);
public sealed record TowerFixedTeamStudy(string Version, TowerFixedTeamFreeze Freeze, IReadOnlyList<TowerDiagnosticCell> Evidence);
public sealed record TowerFixedTeamResult(string Version, string ExecutionStatus, string IntegrityStatus, string StrengthDecision,
    string Adoption, string? CandidateId, IReadOnlyList<TowerDiagnosticRate> Rates, IReadOnlyList<TowerDiagnosticContrast> Contrasts,
    IReadOnlyList<string> RecommendedPartyIds, IReadOnlyList<string> ControlPartyIds, string BalanceAssessment,
    string SamplingAssumption, string? StudyHash, string? ArchiveHash, string StopReason);

/// <summary>One fixed family, one entropy batch and one final strength decision. No search or recovery route.</summary>
public static partial class TowerFixedTeamConfirmation
{
    public const string Version = "tower-practical-fixed-team-confirmation-v1";
    internal const int Samples = 5500, TotalFights = 16500, Family = 7, EntropyBytes = 44000;
    internal static readonly string[] PhaseNames = ["admission", "combat", "audit"];
    internal static readonly int[] SliceSizes = [1000, 1000, 1000, 1000, 1000, 500];
    // Canonical hashes of the exact seed-free family and content in the frozen planning artifact.
    // Runtime and complete history are bound later, at admission; recipes and settings cannot change.
    internal const string TeamsHash = "1162c50936eb5116d27e20642c65fdca6f9d5ad6a9b5a7058be5d282bcb32428";
    internal const string ContentHash = "34efd7e17756456f245724b271f4c40a5e0f87dcbc626363ed558c1bb4343995";
    internal const string SettingsHash = "f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74";
    internal const string SamplingAssumption = "One post-freeze cryptographic batch modeled as independent uniform bits; approximate family-seven Wilson coverage; operational completion is not assumed.";
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string message)
    { if (!ok) throw new InvalidDataException(message); }
    internal static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;
    internal static string P(TowerFixedTeamRequest q, string name) => Path.Combine(q.ArchiveRoot ?? q.OutputRoot, name);
    internal static void Match(TowerFixedTeamRequest q, string name, object value)
        => Require(HarnessJson.Hash(HarnessJson.Read<JsonElement>(P(q, name))) == HarnessJson.Hash(value), "Changed fixed-team artifact: " + name);

    internal static void ValidateDefinition(TowerFixedTeamDefinition d)
    {
        Require(d is not null && d.Version == Version && d.Teams is { Count: 3 } && HarnessJson.Hash(d.Teams) == TeamsHash
            && d.ContentHashes is not null && HarnessJson.Hash(d.ContentHashes) == ContentHash && d.SettingsHash == SettingsHash
            && TowerContractJson.Hash(d.ExecutionHash) && d.ExcludedCombatSeeds is { Count: > 0 }
            && d.ExcludedCombatSeeds.Count <= TowerStudyLimits.HistoricalSeeds - EntropyBytes/4
            && d.ExcludedCombatSeeds.SequenceEqual(d.ExcludedCombatSeeds.Distinct().Order()),
            "Changed fixed family, cohort, content, settings, runtime identity or complete sorted history.");
    }

    internal static IReadOnlyList<TowerFixedTeamChunk> Chunks(TowerFixedTeamDefinition d, IReadOnlyList<int> panel)
    {
        ValidateDefinition(d);
        Require(panel.Count == Samples && panel.Distinct().Count() == Samples && !panel.Intersect(d.ExcludedCombatSeeds).Any(), "Invalid fixed paired panel.");
        return d.Teams.SelectMany((team, ordinal) => SliceSizes.Select((size, slice) => new TowerFixedTeamChunk(ordinal, slice,
            team.PartyId, HarnessJson.Hash(team.Scenario), team.Scenario with { Seeds = panel.Skip(slice*1000).Take(size).ToArray() }))).ToArray();
    }

    internal static async Task<TowerFixedTeamStudy> Execute(TowerFixedTeamFreeze freeze, IReadOnlyList<int> panel,
        TowerBossDiscoveryRun.Battle battle, Action<bool> attempt, CancellationToken ct)
    {
        Require(freeze.Version == Version && TowerContractJson.Hash(freeze.RequestHash) && TowerContractJson.Hash(freeze.DefinitionHash), "Changed freeze.");
        var chunks = Chunks(freeze.Definition, panel); var evidence = new List<TowerDiagnosticCell>(); var ordinal = 0;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var team in chunks.GroupBy(c => c.TeamOrdinal))
        {
            var trials = new List<TowerBalanceTrial>();
            foreach (var chunk in team)
            {
                var recipeHash = HarnessJson.Hash(chunk.Scenario);
                foreach (var seed in chunk.Scenario.Seeds)
                {
                    ct.ThrowIfCancellationRequested(); Require(ordinal < TotalFights, "Attempt cap reached."); attempt(false);
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
        Require(ordinal == TotalFights, "Incomplete fixed confirmation.");
        return new(Version, freeze, evidence);
    }

    internal static void ValidateReport(TowerBattleReport report, TowerScenario scenario, int seed)
        => Require(report.Battle.Seed == seed && report.Battle.ScenarioId == scenario.Id
            && Enum.IsDefined(report.Battle.Summary.ContentOutcome)
            && report.Succeeded == (report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Inconsistent report binding or outcome.");

    internal static TowerFixedTeamResult Assess(TowerFixedTeamStudy study, string archiveHash)
    {
        Require(study.Version == Version && study.Freeze.Version == Version && TowerContractJson.Hash(archiveHash), "Changed fixed study.");
        ValidateDefinition(study.Freeze.Definition); var teams = study.Freeze.Definition.Teams;
        Require(study.Evidence.Count == 3 && study.Evidence.Select(e => e.PartyId).SequenceEqual(teams.Select(t => t.PartyId)), "Changed family order.");
        var panel = study.Evidence[0].Trials.Select(t => t.Seed).ToArray(); Chunks(study.Freeze.Definition, panel);
        Require(study.Evidence.All(e => e.Trials.Select(t => t.Seed).SequenceEqual(panel) && e.Trials.All(t => Enum.IsDefined(t.Outcome))), "Incomplete paired cells.");
        var wins = study.Evidence.Select(e => e.Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray()).ToArray();
        var rates = teams.Select((t, i) => new TowerDiagnosticRate(t.PartyId, wins[i].Count(w => w),
            TowerBalanceEvaluator.Wilson(wins[i].Count(w => w), Samples, Family)!)).ToArray();
        var contrasts = Enumerable.Range(1, 2).Select(i => {
            var pairs = wins[0].Zip(wins[i]).ToArray();
            var gains = pairs.Count(p => p.First && !p.Second); var losses = pairs.Count(p => !p.First && p.Second);
            var g = TowerBalanceEvaluator.Wilson(gains, Samples, Family)!; var l = TowerBalanceEvaluator.Wilson(losses, Samples, Family)!;
            return new TowerDiagnosticContrast(teams[i].PartyId, gains, losses, (gains-losses)/(double)Samples,
                g.Lower-l.Upper, g.Upper-l.Lower, rates[0].Estimate.Lower >= .10 && gains-losses >= 275 && g.Lower-l.Upper > 0);
        }).ToArray();
        var pass = contrasts.All(c => c.Qualifies); var controls = teams.Skip(1).Select(t => t.PartyId).ToArray();
        return new(Version, "Complete", "Verified", pass ? "StrongerFixedTeamConfirmed" : "StrengthNotDemonstrated", pass ? "AdoptFixedTeam" : "Hold",
            teams[0].PartyId, rates, contrasts, pass ? [teams[0].PartyId] : controls, controls, "NotAssessed", SamplingAssumption,
            HarnessJson.Hash(study), archiveHash, "One complete fixed panel; no retry, extension, method-reliability or global-optimality claim.");
    }

    internal static object Export(TowerFixedTeamStudy study, TowerFixedTeamResult result) => new { version = Version, result.Adoption, result.CandidateId,
        teams = study.Freeze.Definition.Teams.Select(t => new { t.Role, t.PartyId, t.ReferenceIds,
            recommended = result.RecommendedPartyIds.Contains(t.PartyId), control = result.ControlPartyIds.Contains(t.PartyId), scenario = t.Scenario,
            subgroups = t.Scenario.Party.ToDictionary(p => p.PartySlot, p => WorldTowerPartyRules.GetPartyNumber(p.PartySlot)),
            requiredCopies = t.Scenario.Party.SelectMany(p => p.Build.EssenceIds).GroupBy(e => e).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()) }).ToArray() };

    internal static string Markdown(TowerFixedTeamResult result)
    {
        var text = new StringBuilder("# Fixed-team Tower confirmation\n\n");
        text.AppendLine($"Decision: **{result.StrengthDecision}**. Adoption: **{result.Adoption}**.");
        text.AppendLine("\nThree fixed teams; 5,500 paired trials each; one final family-seven strength test. Anchors remain controls. This does not establish search reliability, global optimality or encounter balance.");
        text.AppendLine("\n| Anchor | Candidate gains / losses | Observed gain | Adjusted paired interval |\n| --- | ---: | ---: | ---: |");
        foreach (var c in result.Contrasts) text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {c.OtherPartyId} | {c.Gains} / {c.Losses} | {100*c.ObservedGain:F2} pp | {100*c.Lower:F2} to {100*c.Upper:F2} pp |"));
        text.AppendLine("\n[All rates and decisions](result.json) · [Seed-free equipment and teams](teams.json).\n\nSampling assumption: " + SamplingAssumption);
        return text.ToString();
    }
}
