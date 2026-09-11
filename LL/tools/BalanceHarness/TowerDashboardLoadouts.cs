namespace BalanceHarness;

public sealed record DashboardLoadoutRequest(int Slots, string Effort, int Seed, bool WholeParty = false);

public sealed partial class TowerDashboardService
{
    public static bool IsLoadouts(string path) => File.Exists(Path.Combine(path, "party-search.json"));

    public TowerPartySearchDefinition LoadoutPlan(DashboardLoadoutRequest request)
    {
        if (request.Effort is not ("coverage" or "thorough")) throw new InvalidDataException("Choose coverage or thorough sampling.");
        var excluded = TowerPartyProgression.HistoricalSeeds.ToList();
        foreach (var path in RunPaths().Values.Where(IsLoadouts))
        {
            var d = TowerPartySearch.Read(Path.Combine(path, "definition.json"));
            excluded.AddRange(d.ExcludedCombatSeeds);
            excluded.AddRange(TowerPartyProgression.CombatSeeds(d));
        }
        foreach (var path in RunPaths().Values.Where(IsBossSearch))
        {
            var previous = HarnessJson.Read<TowerBossSearchDefinition>(Path.Combine(path, "definition.json"));
            excluded.AddRange(previous.ExcludedCombatSeeds);
            excluded.AddRange(TowerBossSearch.CombatSeeds(previous));
        }
        return request.WholeParty
            ? TowerWholeParty.Definition(apiRoot, catalogsRoot, request.Slots, request.Seed, excluded.Distinct().Order().ToArray(), request.Effort == "thorough")
            : TowerPartyProgression.Definition(apiRoot, catalogsRoot, request.Slots, request.Seed, excluded.Distinct().Order().ToArray(), request.Effort == "thorough");
    }

    public DashboardJob FindLoadouts(DashboardLoadoutRequest request)
    {
        var definition = LoadoutPlan(request);
        return Begin("Loadouts", TowerPartySelection.Validate(definition), async (folder, token) =>
        {
            var output = Path.Combine(folder, "run");
            Change(j => j with { Run = RunId(output), Message = "Preparing fixed progression budget and historical starts…" });
            var report = await TowerPartySearch.RunAsync(apiRoot, catalogsRoot, output, definition, token, message =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, @"(\d+) combats\.");
                Change(j => j with { Completed = match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : j.Completed, Message = message });
            });
            Change(j => j with { Completed = report.ActualBattles, Message = "Frozen parties confirmed on all 15 floors. Review exact builds, assumptions and tradeoffs." });
        });
    }

    private object LoadoutDetails(string path, CancellationToken token)
    {
        var report = TowerPartySearch.VerifyAsync(path, token).GetAwaiter().GetResult();
        var d = TowerPartySearch.Read(Path.Combine(path, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        var ledger = TowerLoadoutArchive.Verify(path, token).ToDictionary(t => t.Id);
        var cells = new List<TowerBenchmarkCell>(); var battles = new List<object>();
        foreach (var party in report.Confirmation)
            foreach (var cell in party.Cells)
            {
                var wins = cell.Clears.Count(c => c);
                cells.Add(new($"{party.Id[..12]} / {cell.Context} / floor {cell.Floor}", cell.Floor, party.Id, "Complete", cell.Clears.Count,
                    wins, cell.Clears.Count - wins - cell.Draws, cell.Draws, SuiteScorecard.Wilson(wins, cell.Clears.Count),
                    SuiteScorecard.Distribution([cell.Survival]), SuiteScorecard.Distribution([]), SuiteScorecard.Distribution([cell.GuardianHealth])));
                // A representative saved seed for every finalist/context/floor; full trial lists remain in JSON.
                var trial = ledger[cell.Trials[0]];
                var battle = TowerLoadoutArchive.ReadBattle(path, trial.Id, scope.ReportStorage);
                battles.Add(new { trial.Id, trial.Seed, Outcome = battle.Battle.Summary.ContentOutcome.ToString(),
                    battle.Battle.Summary.DurationSeconds, battle.GuardianHealthRemainingPercent });
            }
        return new { IsLoadoutSearch = true, Definition = new { Id = $"{d.Budget?.EssenceSlots ?? 4}-slot loadout search", d.Budget },
            Report = new TowerBenchmarkReport(1, report.Status, report.PlannedMaximum, report.ActualBattles, cells),
            MasterSeed = d.CharacterSeed, ReplayCompatible = HarnessJson.Hash(scope.Execution) == HarnessJson.Hash(ExecutionIdentity.Current()),
            Battles = battles, Loadouts = report.Selection, d.StartingLoadouts,
            DeploymentComparisons = d.SchemaVersion == 3 ? TowerWholeParty.Comparisons(d, report.Characters, report.Discovery, report.Confirmation) : null,
            Note = "Table shows confirmation only. Trial total includes both search stages. Replay menu contains the first seed per finalist/context/floor; all trials and paired results are in JSON/Markdown. Joint contexts share the same first-cell changes." };
    }

    public string ReportFile(string id, string format, CancellationToken token)
    {
        if (format is not ("json" or "md")) throw new InvalidDataException("Choose JSON or Markdown.");
        var path = ResolveRun(id);
        if (IsStudy(path))
        {
            ReadStudyAsync(path, token).GetAwaiter().GetResult();
            return Path.Combine(path, "study." + format);
        }
        if (IsBossSearch(path))
        {
            ReadBossReportAsync(path, token).GetAwaiter().GetResult();
            return Path.Combine(path, "boss-search." + format);
        }
        if (IsLoadouts(path))
        {
            TowerPartySearch.VerifyAsync(path, token).GetAwaiter().GetResult();
            return Path.Combine(path, "party-search." + format);
        }
        TowerBenchmark.ReadSaved(path, token); return Path.Combine(path, "benchmark." + format);
    }

    public string LoadoutRecipe(string id, string battle, CancellationToken token)
    {
        var path = ResolveRun(id);
        if (!IsStudy(path) && !IsLoadouts(path) && !IsBossSearch(path)) throw new InvalidDataException("Choose a loadout-search run.");
        if (IsStudy(path)) ReadStudyAsync(path, token).GetAwaiter().GetResult();
        var trials = IsBossSearch(path)
            ? ReadBossArchiveAsync(path, token).GetAwaiter().GetResult().Trials
            : TowerLoadoutArchive.Verify(path, token);
        var trial = trials.SingleOrDefault(t => t.Id == battle)
            ?? throw new InvalidDataException("Unknown saved battle.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(trial.Recipe, "^[a-f0-9]{64}$")) throw new InvalidDataException("Invalid saved recipe identity.");
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        var recipe = Path.Combine(path, "recipes", trial.Recipe + ".json");
        var scenario = HarnessJson.Read<TowerScenario>(recipe);
        var root = Path.Combine(path, "content");
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        if (HarnessJson.Hash(scenario) != trial.Recipe || HarnessJson.Hash(runner.CreateInput(scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)) != trial.InputHash)
            throw new InvalidDataException("Export recipe differs from its recorded input.");
        return recipe;
    }
}
