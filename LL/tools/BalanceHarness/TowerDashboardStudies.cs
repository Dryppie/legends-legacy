using System.Text.Json;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record DashboardStudyRequest(int Floor, int Slots, int Seed, string Purpose = "intended-progression",
    bool IncludeUserReference = false, IReadOnlyList<string>? AllowedEssences = null,
    IReadOnlyDictionary<string, int>? OwnedCopies = null, IReadOnlyList<int>? ExcludedSeeds = null,
    IReadOnlyList<BossBenchmarkReference>? References = null);
public sealed record DashboardStudyStart(string PlanHash);
public sealed record DashboardStudyPreview(string PlanHash, TowerBossDiscoveryDefinition Definition, BossDiscoveryCost Cost,
    IReadOnlyList<string> SeedSources, string Note);

public sealed partial class TowerDashboardService
{
    private DashboardStudyPreview? _studyPlan;
    private readonly object _studyReadGate = new();
    private (string Path, string Hash, string Json)? _studyRead;
    public static bool IsStudy(string path) => File.Exists(Path.Combine(path, "study.json"));

    public object StudyOptions() => new { Pool = new OfflineContent(apiRoot, TowerBundle.ReadSettings(apiRoot).Threat).Essences.GetAll()
        .OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => new { e.Id, e.Name, Family = e.SourceMonsterId }).ToArray(),
        Note = "Compatible saved builds are included as benchmark controls. New teams are generated independently for every character. Import additional historical seed exclusions for campaigns outside the configured results folder." };

    public DashboardStudyPreview StudyPlan(DashboardStudyRequest request)
    {
        var budget = TowerPartyProgression.Budget(request.Slots) with { PriorityFloor = request.Floor };
        if (!TowerBossDiscovery.LegalPurpose(budget, request.Purpose)) throw new InvalidDataException("Use the intended slot checkpoint or explicitly choose a diagnostic budget.");
        var floor = new Services.LL.WorldTower.JsonWorldTowerDefinitionProvider(Path.Combine(apiRoot, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options)
            .GetFloor(request.Floor) ?? throw new InvalidDataException("Choose a released Tower floor.");
        // Equipment-only data: this path never loads an authored Essence party to construct a template.
        var gear = HarnessJson.Read<EquipmentReferenceEquipmentSelection[][]>(Path.Combine(catalogsRoot, "tower-team-equipment.json"));
        if (gear.Length == 0) throw new InvalidDataException("Missing neutral equipment pattern.");
        var templates = Enumerable.Range(1, floor.RequiredSlots).Select(slot => new TowerPartyRecipe(slot,
            new($"tower-discovery-character-{slot}", budget.CharacterLevel, budget.Tier, budget.Rank,
                gear[(slot - 1) % gear.Length], [], budget.Quality))).ToArray();
        var seeds = StudySeedExclusions();
        var retained = TowerRetainedBuilds.Load(catalogsRoot, _runsRoot);
        var d = TowerBossDiscovery.Create(apiRoot, $"floor-{request.Floor}-independent-teams", budget,
            DateTimeOffset.Parse("2000-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            [new("fixed-equipment", templates)], request.Seed, seeds.Seeds.Concat(request.ExcludedSeeds ?? []).Distinct().Order().ToArray(), budgetPurpose: request.Purpose);
        if (request.AllowedEssences is not null)
        {
            if (request.AllowedEssences.Count == 0 || request.AllowedEssences.Distinct().Count() != request.AllowedEssences.Count
                || request.AllowedEssences.Except(d.AllowedEssences.Select(e => e.Id)).Any()) throw new InvalidDataException("Choose a nonempty legal Essence pool without duplicate IDs.");
            d = d with { AllowedEssences = d.AllowedEssences.Where(e => request.AllowedEssences.Contains(e.Id)).ToArray() };
        }
        d = d with { OwnedCopies = request.OwnedCopies };
        TowerBossDiscovery.Validate(d with { References = request.References ?? [] });
        var references = TowerRetainedBuilds.Compatible(d, retained).ToList();
        foreach (var reference in request.References ?? [])
        {
            if (!references.Any(r => r.Context == reference.Context
                && TowerBossDiscovery.RecipeHash(r.Scenario.Party) == TowerBossDiscovery.RecipeHash(reference.Scenario.Party)))
                references.Add(reference);
        }
        if (request.IncludeUserReference)
        {
            if (request.Floor != 1 || request.Slots != 4) throw new InvalidDataException("The supplied benchmark is registered only for floor 1 with four Essences.");
            var file = Path.Combine(catalogsRoot, "tower-floor-1-user-party.json");
            references.Add(new("user-floor-1", "fixed-equipment", HarnessJson.Read<TowerScenario>(file) with { Seeds = [] },
                "User-authored floor-1 benchmark; reference only, never a search parent", HarnessJson.FileHash(file)));
        }
        return SaveStudyPlan(d with { References = references }, [.. seeds.Sources,
            $"Retained benchmark catalog: {references.Count} compatible controls; historical combat schedules excluded."]);
    }

    public DashboardStudyPreview ImportStudyPlan(TowerBossDiscoveryDefinition definition)
    {
        TowerBossDiscovery.Validate(apiRoot, definition); TowerBossImprovement.Inputs(definition);
        var seeds = StudySeedExclusions();
        var used = definition.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics));
        if (used.Intersect(seeds.Seeds).Any()) throw new InvalidDataException("Imported stage seeds overlap recorded history; author a fresh definition.");
        // Imported explicit schedules remain exact. Add known exclusions; never silently regenerate their samples.
        return SaveStudyPlan(definition with { ExcludedCombatSeeds = definition.ExcludedCombatSeeds.Concat(seeds.Seeds).Distinct().Order().ToArray() }, seeds.Sources);
    }

    private DashboardStudyPreview SaveStudyPlan(TowerBossDiscoveryDefinition definition, IReadOnlyList<string> sources)
    {
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        var cost = TowerBossDiscovery.Validate(apiRoot, d); TowerBossImprovement.Inputs(d);
        var result = new DashboardStudyPreview(HarnessJson.Hash(d), d, cost, sources,
            (d.Mode == TowerBossDiscovery.Improve ? "Retained-build improvement: explicit starts are scored on fresh discovery seeds and carry reference ancestry. " : "Independent discovery: no supplied starts. ") + "Preview only. Benchmark confirmation enters after search finalists freeze. Recorded history in this results folder is excluded; import the complete historical union for any additional campaigns. No global floor-balance guarantee.");
        lock (_gate) _studyPlan = JsonSerializer.Deserialize<DashboardStudyPreview>(JsonSerializer.Serialize(result, HarnessJson.Options), HarnessJson.Options);
        return result;
    }

    public DashboardJob FindTeams(DashboardStudyStart request)
    {
        lock (_gate)
        {
            if (_studyPlan is null || request.PlanHash != _studyPlan.PlanHash) throw new InvalidDataException("Preview the current study before starting it.");
            var plan = _studyPlan;
            TowerBossDiscovery.Validate(apiRoot, plan.Definition);
            var schedule = plan.Definition.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics));
            if (schedule.Intersect(StudySeedExclusions().Seeds).Any()) throw new InvalidDataException("The preview now overlaps recorded history. Prepare a new study.");
            var job = Begin(plan.Definition.Mode == TowerBossDiscovery.Improve ? "Retained-build improvement" : "Independent teams", plan.Cost.Total, async (folder, token) => {
                var output = Path.Combine(folder, "run");
                HarnessJson.WriteNew(Path.Combine(folder, "preview.json"), plan);
                Change(j => j with { Run = RunId(output), Message = "Freezing independent study inputs…" });
                var discoveryCount = 0; var selectionCount = 0;
                var report = await TowerBossStudy.RunAsync(apiRoot, output, plan.Definition, token, message => {
                    var match = System.Text.RegularExpressions.Regex.Match(message, @"(?:parties, |Confirmation: )(\d+)/");
                    if (message.StartsWith("Discovery:", StringComparison.Ordinal) && match.Success) discoveryCount = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var selection = System.Text.RegularExpressions.Regex.Match(message, @"^Selection: (\d+)/");
                    if (selection.Success) selectionCount = int.Parse(selection.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * plan.Definition.Stages.Schedules.Values.Sum(s => s.Selection.Count);
                    var confirmation = message.StartsWith("Confirmation:", StringComparison.Ordinal) && match.Success
                        ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
                    Change(j => j with { Completed = Math.Max(j.Completed, discoveryCount + selectionCount + confirmation), Message = message });
                });
                TowerRetainedBuilds.Remember(_runsRoot, output, plan.Definition, report);
                Change(j => j with { Status = report.Status, Completed = report.Accounting.Completed.Values.Sum(),
                    Message = $"Study {report.Status}. Balance: {report.Conclusion?.OverallAssessment.ToString() ?? "Unavailable"}; generated viability: {report.Conclusion?.GeneratedViability.ToString() ?? "Unavailable"}. Review the frozen family and earlier concerns." });
            });
            _studyPlan = null; return job;
        }
    }

    private DashboardRun StudyRun(string id, string path)
    {
        var report = HarnessJson.Read<BossStudyReport>(Path.Combine(path, "study.json"));
        return new(id, Path.GetRelativePath(_runsRoot, path), report.Status, report.Accounting.Completed.Values.Sum(), report.Accounting.Reserved, TowerBossDiscovery.Read(Path.Combine(path, "definition.json")).Mode == TowerBossDiscovery.Improve ? "Retained-build improvement" : "Independent teams");
    }

    private async Task<BossStudyReport> ReadStudyAsync(string path, CancellationToken token)
    {
        var inventory = TowerLoadoutArchive.VerifyInventory(path, token);
        lock (_studyReadGate)
            if (_studyRead is { } cache && cache.Path == path && cache.Hash == inventory.Hash)
                return JsonSerializer.Deserialize<BossStudyReport>(cache.Json, HarnessJson.Options)!;
        var report = HarnessJson.Read<BossStudyReport>(Path.Combine(path, "study.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        var compatible = HarnessJson.Hash(scope.Execution) == HarnessJson.Hash(ExecutionIdentity.Current());
        if (report.Status is "Complete" or "Incomplete" && compatible) report = await TowerBossStudy.VerifyAsync(path, token);
        else
        {
            if (report.Status is not ("Cancelled" or "Invalid" or "Complete" or "Incomplete") || report.Accounting.Completed.Where(p => p.Key != "replay").Sum(p => p.Value) != inventory.Trials.Count)
                throw new InvalidDataException("Invalid partial study accounting.");
            // Partial evidence has file integrity only, never a verified balance recommendation.
            report = report with { Conclusion = report.Conclusion is null ? null : report.Conclusion with { OverallAssessment = GoalOutcome.Invalid } };
        }
        lock (_studyReadGate) _studyRead = (path, inventory.Hash, JsonSerializer.Serialize(report, HarnessJson.Options));
        return report;
    }

    private object StudyDetails(string path, CancellationToken token)
    {
        var report = ReadStudyAsync(path, token).GetAwaiter().GetResult();
        var definition = TowerBossDiscovery.Read(Path.Combine(path, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(path, "scope.json"));
        var compatible = HarnessJson.Hash(scope.Execution) == HarnessJson.Hash(ExecutionIdentity.Current());
        var trials = TowerLoadoutArchive.Verify(path, token);
        var chosen = report.Replays.Select(r => r.TrialId).ToHashSet();
        foreach (var cell in report.Confirmation?.Definition.Cells ?? [])
        {
            var recipe = HarnessJson.Hash(cell.Scenario);
            var first = trials.FirstOrDefault(t => t.Recipe == recipe); if (first is not null) chosen.Add(first.Id);
        }
        if (chosen.Count == 0) foreach (var trial in trials.GroupBy(t => t.Recipe).Take(10).Select(g => g.First())) chosen.Add(trial.Id);
        var battles = trials.Where(t => chosen.Contains(t.Id)).Select(t => Preview(t, TowerLoadoutArchive.ReadBattle(path, t.Id, scope.ReportStorage))).ToArray();
        return new { IsStudy = true, IsLoadoutSearch = true, IsPartialEvidence = report.Status != "Complete" || !compatible,
            Definition = new { definition.Id, definition.Budget }, StudyDefinition = definition, Study = report,
            Integrity = compatible && report.Status is "Complete" or "Incomplete" ? "Reconstructed" : "File integrity only",
            Report = new TowerBenchmarkReport(1, report.Status, report.Accounting.Reserved, report.Accounting.Completed.Values.Sum(), []),
            MasterSeed = definition.Generation.Seeds[0], ReplayCompatible = compatible,
            Battles = battles, Loadouts = Array.Empty<PartyChoice>(), Note = definition.Mode == TowerBossDiscovery.Improve ? "Reference-derived improvement. Explicit starts and inherited ancestry remain in the archive; this does not establish independent rediscovery." : "Independent target-only study. Generation, references, balance and integrity are separate findings." };
    }

    public TowerScenario StudyRecipe(string id, string cell, CancellationToken token)
    {
        var path = ResolveRun(id); if (!IsStudy(path)) throw new InvalidDataException("Choose an independent study.");
        var report = ReadStudyAsync(path, token).GetAwaiter().GetResult();
        return report.Confirmation?.Definition.Cells.SingleOrDefault(c => c.Id == cell)?.Scenario
            ?? throw new InvalidDataException("Unknown frozen confirmation cell.");
    }
}
