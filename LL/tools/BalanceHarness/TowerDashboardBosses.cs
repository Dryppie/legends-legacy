using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BalanceHarness;

public sealed record DashboardBossRequest(int Floor, int Slots, string Effort, int Seed, string Intent = "any");

public sealed partial class TowerDashboardService
{
    private sealed record BossBattlePreview(string Id, int Seed, string Outcome, double DurationSeconds,
        decimal GuardianHealthRemainingPercent);
    private sealed record BossArchiveRead(TowerBossSearchReport Report, IReadOnlyList<LoadoutTrial> Trials,
        IReadOnlyDictionary<string, BossBattlePreview> Battles);
    private sealed record BossArchiveEvidence(TowerBossSearchReport Report, IReadOnlyDictionary<string, BossBattlePreview> Battles);
    private static readonly JsonSerializerOptions BossCacheJson = new(HarnessJson.Options) { WriteIndented = false };
    private readonly object _bossReadGate = new();
    // Exactly one completed archive per dashboard. Store serialized data so callers cannot mutate
    // a trusted cached report. A hit still checks the complete inventory and every file's SHA-256.
    private (string Path, string InventoryHash, string Json)? _bossRead;

    public static bool IsBossSearch(string path) => File.Exists(Path.Combine(path, "boss-search.json"));
    public object BossBudgets() => Enumerable.Range(4, 7).Select(TowerPartyProgression.Budget).ToArray();

    public TowerBossSearchDefinition BossPlan(DashboardBossRequest request)
    {
        var excluded = TowerPartyProgression.HistoricalSeeds.ToList();
        foreach (var path in RunPaths().Values)
        {
            if (IsBossSearch(path))
            {
                var previous = HarnessJson.Read<TowerBossSearchDefinition>(Path.Combine(path, "definition.json"));
                excluded.AddRange(previous.ExcludedCombatSeeds);
                excluded.AddRange(TowerBossSearch.CombatSeeds(previous));
            }
            else if (IsLoadouts(path))
            {
                var previous = TowerPartySearch.Read(Path.Combine(path, "definition.json"));
                excluded.AddRange(previous.ExcludedCombatSeeds);
                excluded.AddRange(TowerPartyProgression.CombatSeeds(previous));
            }
        }
        return TowerBossSearch.Definition(apiRoot, catalogsRoot, request.Floor, request.Slots, request.Seed,
            request.Intent, request.Effort, excluded.Distinct().Order().ToArray());
    }

    public DashboardJob FindBossLoadouts(DashboardBossRequest request)
    {
        var definition = BossPlan(request);
        return Begin("Boss loadouts", TowerBossSearch.Validate(definition), async (folder, token) =>
        {
            var output = Path.Combine(folder, "run");
            Change(j => j with { Run = RunId(output), Message = "Preparing boss strategies and freezing the experiment budget…" });
            TowerBossSearchReport report;
            try
            {
                report = await TowerBossSearch.RunAsync(apiRoot, catalogsRoot, output, definition, token, message =>
                {
                    var match = Regex.Match(message, @"(\d+) combats\.");
                    Change(j => j with
                    {
                        Completed = match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : j.Completed,
                        Message = message
                    });
                });
            }
            catch (OperationCanceledException)
            {
                var saved = Path.Combine(output, "boss-search.json");
                if (File.Exists(saved)) Change(j => j with { Completed = HarnessJson.Read<TowerBossSearchReport>(saved).ActualBattles });
                throw;
            }
            Change(j => j with { Completed = report.ActualBattles,
                Message = "Boss alternatives and controls saved with fresh confirmation on all 15 floors. Review clear rates, weaknesses and exact party dependencies." });
        });
    }

    private DashboardRun BossRun(string id, string path)
    {
        var summary = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(path, "boss-search.json"));
        return new(id, Path.GetRelativePath(_runsRoot, path), summary.Status,
            summary.ActualBattles, summary.PlannedMaximum, "Boss loadouts");
    }

    private async Task<BossArchiveRead> ReadBossArchiveAsync(string path, CancellationToken token)
    {
        var inventory = TowerLoadoutArchive.VerifyInventory(path, token);
        lock (_bossReadGate)
            if (_bossRead is { } cached && cached.Path == path && cached.InventoryHash == inventory.Hash)
            {
                token.ThrowIfCancellationRequested();
                var evidence = JsonSerializer.Deserialize<BossArchiveEvidence>(cached.Json, BossCacheJson)!;
                token.ThrowIfCancellationRequested();
                return new(evidence.Report, inventory.Trials, evidence.Battles);
            }
        var report = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(path, "boss-search.json"));
        if (report.Status == "Complete")
        {
            var battles = new Dictionary<string, BossBattlePreview>(StringComparer.Ordinal);
            report = await TowerBossSearch.VerifyAsync(path, inventory.Trials,
                (trial, battle) => battles.Add(trial.Id, Preview(trial, battle)), token);
            var result = new BossArchiveRead(report, inventory.Trials, battles);
            var json = JsonSerializer.Serialize(new BossArchiveEvidence(report, battles), BossCacheJson);
            token.ThrowIfCancellationRequested();
            lock (_bossReadGate) _bossRead = (path, inventory.Hash, json);
            return result;
        }
        var trials = inventory.Trials;
        var definition = TowerBossSearch.Read(Path.Combine(path, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        if (report.Status is not ("Cancelled" or "Invalid" or "Incomplete") || report.ActualBattles != trials.Count
            || report.PlannedMaximum != TowerBossSearch.Validate(definition) || trials.Count > report.PlannedMaximum
            || scope.Algorithm != TowerBossSearch.Algorithm(definition))
            throw new InvalidDataException("Invalid incomplete boss search archive.");
        return new(report, trials, new Dictionary<string, BossBattlePreview>());
    }

    private static BossBattlePreview Preview(LoadoutTrial trial, TowerBattleReport battle) => new(trial.Id, trial.Seed,
        battle.Battle.Summary.ContentOutcome.ToString(), battle.Battle.Summary.DurationSeconds, battle.GuardianHealthRemainingPercent);

    private async Task<TowerBossSearchReport> ReadBossReportAsync(string path, CancellationToken token)
        => (await ReadBossArchiveAsync(path, token)).Report;

    private object BossDetails(string path, CancellationToken token)
    {
        var verified = ReadBossArchiveAsync(path, token).GetAwaiter().GetResult();
        var report = verified.Report;
        var complete = report.Status == "Complete";
        var definition = HarnessJson.Read<TowerBossSearchDefinition>(Path.Combine(path, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        var diagnosticPath = Path.Combine(path, "diagnostic-plan.json");
        var diagnosticPlan = File.Exists(diagnosticPath) ? HarnessJson.Read<BossDiagnosticPlan>(diagnosticPath) : null;
        var ledger = verified.Trials.ToDictionary(t => t.Id);
        var aliases = report.ContextAliases.ToDictionary(a => (a.Floor, a.Context), a => a.EvaluatedContext);
        var cells = new List<TowerBenchmarkCell>();
        var battles = new List<object>();
        foreach (var party in complete ? report.Confirmation : [])
            foreach (var cell in party.Cells)
            {
                if (aliases[(cell.Floor, cell.Context)] != cell.Context) continue;
                var wins = cell.Clears.Count(c => c);
                token.ThrowIfCancellationRequested();
                var saved = cell.Trials.Select(id => verified.Battles[id]).ToArray();
                cells.Add(new($"{party.Id[..12]} / {cell.Context} / floor {cell.Floor}", cell.Floor, party.Id, "Complete", cell.Clears.Count,
                    wins, cell.Clears.Count - wins - cell.Draws, cell.Draws, SuiteScorecard.Wilson(wins, cell.Clears.Count),
                    SuiteScorecard.Distribution([cell.Survival]),
                    SuiteScorecard.Distribution(saved.Select(s => s.DurationSeconds)),
                    SuiteScorecard.Distribution([cell.GuardianHealth])));
                // Include a saved example of each observed outcome, including draws and defeats.
                foreach (var example in saved.DistinctBy(s => s.Outcome)) battles.Add(example);
            }
        // Cancelled discovery is evidence, but never a confirmed recommendation.
        if (battles.Count == 0)
            foreach (var trial in ledger.Values.GroupBy(t => t.Recipe).Select(g => g.First()).Take(100))
            {
                token.ThrowIfCancellationRequested();
                var battle = TowerLoadoutArchive.ReadBattle(path, trial.Id, scope.ReportStorage);
                battles.Add(new { trial.Id, trial.Seed, Outcome = battle.Battle.Summary.ContentOutcome.ToString(),
                    battle.Battle.Summary.DurationSeconds, battle.GuardianHealthRemainingPercent });
            }
        return new
        {
            IsLoadoutSearch = true, IsBossSearch = true,
            IsPartialEvidence = !complete,
            Definition = new { Id = definition.Id, definition.Budget },
            Report = new TowerBenchmarkReport(1, report.Status, report.PlannedMaximum, report.ActualBattles, cells),
            MasterSeed = definition.DiscoverySeed,
            ReplayCompatible = HarnessJson.Hash(scope.Execution) == HarnessJson.Hash(ExecutionIdentity.Current()),
            Battles = battles, Loadouts = complete ? report.Selection : [],
            DiagnosticPlan = diagnosticPlan,
            BossDefinition = definition, BossSearch = complete ? report : report with { Selection = [], Confirmation = [], StrategyArchive = [] },
            Note = (complete ? "Best found for this boss under the displayed budget. " : "Incomplete experiment: saved file hashes are verified, but no confirmed recommendations are shown. Replay individually verifies each retained trial. ")
                + "The table contains fresh confirmation and all-floor transfer only; search trials are included in the total. Identical effective contexts appear once, with aliases retained in the strategy evidence. Zero target clears remain unsuccessful. Intent and mechanic hypotheses do not establish a recommended role or causal protection/healing benefit. Wilson intervals and paired outcomes are descriptive. Exact recipes include required allies, fixed gear and ordered Essences."
        };
    }
}
