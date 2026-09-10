using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Randomness;
using Domain.Models.Items;

namespace BalanceHarness;

public sealed record LoadoutGear(string Id, ItemQuality Quality, int Rank);
public sealed record TowerLoadoutPilotDefinition(int SchemaVersion, int TargetPartySlot, IReadOnlyList<LoadoutGear> Gear,
    IReadOnlyList<int> SearchSeeds, int DiscoverySeed, int ConfirmationSeed, int CandidatesPerArm,
    int DiscoverySamples, int ConfirmationSamples, int MaximumBattles, IReadOnlyList<string>? AllowedEssences = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? AllyContexts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<LoadoutFinalist>? FixedCandidates = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<int>? ExcludedCombatSeeds = null);
public sealed record LoadoutFinalist(string Id, string Source, IReadOnlyList<string> Essences, bool PreviousParty = false);
public sealed record LoadoutCohortSelection(string Cohort, IReadOnlyList<LoadoutFinalist> Finalists);
public sealed record LoadoutMethodResult(string Cohort, LoadoutSearchResult Search);
public sealed record LoadoutConfirmation(string Cohort, string Candidate, int Floor, int Wins, int Draws, int Trials,
    RateEstimate ClearRate, int ControlWins, int Gained, int Lost, PairedEstimate Change, double GuardianHealth, double Survival);
public sealed record TowerLoadoutPilotReport(string Status, int PlannedMaximum, int ActualBattles, int CacheHits,
    IReadOnlyList<LoadoutMethodResult> Discovery, IReadOnlyList<LoadoutCohortSelection> Selection,
    IReadOnlyList<LoadoutConfirmation> Confirmation);

/// <summary>Four-Essence character pilot; fixed allies, separate entry gear cohorts, all released floors.</summary>
public static class TowerLoadoutPilot
{
    public static TowerLoadoutPilotDefinition Default => new(1, 2,
        [new("standard-rank-1", ItemQuality.Standard, 1), new("standard-rank-2", ItemQuality.Standard, 2),
         new("fine-rank-1", ItemQuality.Fine, 1), new("fine-rank-2", ItemQuality.Fine, 2)],
        [1701, 2903], 202609101, 202609102, 12, 2, 10, 10000);

    public static TowerLoadoutPilotDefinition Read(string path) => JsonSerializer.Deserialize<TowerLoadoutPilotDefinition>(File.ReadAllText(path),
        new JsonSerializerOptions(HarnessJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
        ?? throw new InvalidDataException("Empty pilot definition.");

    public static int Validate(TowerLoadoutPilotDefinition d)
    {
        if (d.SchemaVersion is not (1 or 2) || d.TargetPartySlot is < 1 or > 5 || d.Gear is not { Count: > 0 and <= 4 }
            || d.Gear.Any(g => g is null || !TowerBenchmark.SafeId(g.Id) || g.Rank is < 1 or > 2 || g.Quality is not (ItemQuality.Standard or ItemQuality.Fine))
            || d.Gear.Select(g => g.Id).Distinct().Count() != d.Gear.Count
            || d.Gear.Select(g => (g.Rank, g.Quality)).Distinct().Count() != d.Gear.Count
            || d.SearchSeeds is not { Count: > 0 and <= 8 } || (d.SchemaVersion == 1 && d.SearchSeeds.Count > 2)
            || d.SearchSeeds.Distinct().Count() != d.SearchSeeds.Count
            || d.DiscoverySeed == d.ConfirmationSeed || d.CandidatesPerArm is < 2 or > 40
            || d.DiscoverySamples is < 1 or > 20 || (d.SchemaVersion == 1 && d.DiscoverySamples > 10)
            || d.ConfirmationSamples is < 1 or > 100 || d.MaximumBattles is < 1 or > 100000
            || (d.SchemaVersion == 1 && (d.MaximumBattles > 20000 || d.AllyContexts is not null || d.FixedCandidates is not null || d.ExcludedCombatSeeds is not null))
            || (d.SchemaVersion == 2 && (d.AllyContexts is not { Count: 2 } || !d.AllyContexts.Order().SequenceEqual(new[] { "balanced", "previous-05" })
                || d.FixedCandidates is not { Count: <= 4 } || d.FixedCandidates.Any(c => c.PreviousParty || c.Essences.Count != 4
                    || c.Id != HarnessJson.Hash(c.Essences) || string.IsNullOrWhiteSpace(c.Source))
                || d.FixedCandidates.Select(c => c.Id).Distinct().Count() != d.FixedCandidates.Count
                || d.ExcludedCombatSeeds is not { Count: > 0 and <= 100000 } || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count)))
            throw new InvalidDataException("Invalid bounded four-slot pilot definition.");
        var allies = d.AllyContexts?.Count ?? 1;
        var finalists = d.SchemaVersion == 1 ? 2 + 2 * d.SearchSeeds.Count : 1 + d.FixedCandidates!.Count + 2 * d.SearchSeeds.Count * allies;
        var planned = d.Gear.Count * allies * 15 * (d.SearchSeeds.Count * 2 * d.CandidatesPerArm * d.DiscoverySamples + finalists * d.ConfirmationSamples);
        if (planned > d.MaximumBattles) throw new InvalidDataException($"Planned upper bound {planned} exceeds the hard combat budget.");
        ValidateSchedules(Schedule(d.DiscoverySeed, d.DiscoverySamples), Schedule(d.ConfirmationSeed, d.ConfirmationSamples));
        if (d.ExcludedCombatSeeds is { } excluded && Schedule(d.DiscoverySeed, d.DiscoverySamples).Values.SelectMany(s => s)
            .Concat(Schedule(d.ConfirmationSeed, d.ConfirmationSamples).Values.SelectMany(s => s)).Intersect(excluded).Any())
            throw new InvalidDataException("New schedules reuse an excluded historical combat seed.");
        return planned;
    }

    public static IReadOnlyDictionary<int, IReadOnlyList<int>> Schedule(int seed, int samples) => Enumerable.Range(1, 15).ToDictionary(f => f,
        f => (IReadOnlyList<int>)Enumerable.Range(0, samples).Select(i => StableRandom.Seed("tower-loadout-pilot-combat-v1",
            seed.ToString(CultureInfo.InvariantCulture), f.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture))).ToArray());

    public static void ValidateSchedules(IReadOnlyDictionary<int, IReadOnlyList<int>> discovery, IReadOnlyDictionary<int, IReadOnlyList<int>> confirmation)
    {
        var a = discovery.Values.SelectMany(s => s).ToArray(); var b = confirmation.Values.SelectMany(s => s).ToArray();
        if (a.Length != a.Distinct().Count() || b.Length != b.Distinct().Count() || a.Intersect(b).Any())
            throw new InvalidDataException("Combat schedules contain duplicate or overlapping seeds.");
    }

    public static IReadOnlyList<TowerScenario> Scenarios(string root, string catalogs, LoadoutGear gear)
    {
        var curve = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogs, "tower-curve.json"));
        var profiles = curve.Profiles.Where(p => p.Id.StartsWith("slots-4-", StringComparison.Ordinal)).Select(p => p with
        { Build = p.Build with { Rank = gear.Rank, Quality = gear.Quality }, Assumptions = "Four-Essence entry pilot: level 30, tier 1, Uncommon, "
            + gear.Quality + ", rank " + gear.Rank + "; baseline rolls, level-1 unascended/unevolved Essences; no styles. Ownership assumed." }).ToArray();
        var catalog = curve with { SchemaVersion = 1, Id = "tower-loadout-pilot-v1", Floors = Enumerable.Range(1, 15).ToArray(), Profiles = profiles,
            Parties = [new("balanced", "Fixed original balanced allies. Only the selected character in the FIRST cell changes. Later-floor four-slot results are coverage, not a progression recommendation.",
                ["slots-4-guardian", "slots-4-restorer", "slots-4-striker", "slots-4-striker", "slots-4-controller"])] };
        return TowerBenchmark.Expand(catalog, root, 1, 1).Select(s => s with { Party = s.Party.Select(p => p with
            { Build = p.Build with { IdentityEssenceIds = p.Build.EssenceIds.ToArray() } }).ToArray() }).ToArray();
    }

    public static TowerScenario Apply(TowerScenario scenario, int target, LoadoutFinalist candidate, IReadOnlyList<int> seeds) => scenario with
    {
        Seeds = seeds,
        Party = scenario.Party.Select(p => p with { Build = p.Build with { EssenceIds = candidate.PreviousParty
            ? p.Build.EssenceIds.Select(id => id == "essence.hobgoblin" && (p.PartySlot - 1) % 5 == 0 ? "essence.horned_wolf"
                : id == "essence.pixie" && (p.PartySlot - 1) % 5 is 2 or 3 ? "essence.dire_wolf" : id).ToArray()
            : p.PartySlot == target ? candidate.Essences : p.Build.EssenceIds } }).ToArray()
    };

    public static LoadoutCohortSelection Select(string cohort, IReadOnlyList<string> original, IEnumerable<LoadoutMethodResult> discovery)
    {
        var finalists = new List<LoadoutFinalist> { new("control", "Unchanged original balanced party", original),
            new("previous-05", "Historical candidate-05 substitutions, remeasured with pinned identities. Different allies; not a character-search candidate.", original, true) };
        foreach (var arm in discovery.Where(a => a.Cohort == cohort))
        {
            var winner = LoadoutSearch.Rank(arm.Search.Evaluations).First();
            if (finalists.Any(f => !f.PreviousParty && f.Essences.SequenceEqual(winner.Essences))) continue;
            finalists.Add(new(winner.Id, $"{arm.Search.Method}, search seed {arm.Search.Seed}", winner.Essences));
        }
        return new(cohort, finalists);
    }

    public static TowerLoadoutPilotReport ReadReport(string output, CancellationToken token = default)
    {
        var trials = TowerLoadoutArchive.Verify(output, token).ToDictionary(t => t.Id);
        var report = HarnessJson.Read<TowerLoadoutPilotReport>(Path.Combine(output, "pilot.json"));
        var definition = Read(Path.Combine(output, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        var contexts = HarnessJson.Read<Dictionary<string, IReadOnlyList<TowerScenario>>>(Path.Combine(output, "contexts.json"));
        if (report.Status != "Complete" || report.ActualBattles != trials.Count || report.PlannedMaximum != Validate(definition))
            throw new InvalidDataException("Pilot is incomplete or has inconsistent cost.");
        var firstConfirmation = trials.Values.TakeWhile(t => t.Stage == "discovery").Count();
        if (trials.Values.Skip(firstConfirmation).Any(t => t.Stage != "confirmation")) throw new InvalidDataException("Interleaved selection and confirmation.");
        var recipes = new Dictionary<string, TowerScenario>();
        TowerScenario Recipe(LoadoutTrial trial)
        {
            if (!recipes.TryGetValue(trial.Recipe, out var recipe))
                recipes.Add(trial.Recipe, recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", trial.Recipe + ".json")));
            return recipe;
        }
        ValidateSchedules(Schedule(definition.DiscoverySeed, definition.DiscoverySamples), Schedule(definition.ConfirmationSeed, definition.ConfirmationSamples));
        foreach (var arm in report.Discovery)
        {
            token.ThrowIfCancellationRequested();
            if (arm.Search.Evaluations.Count != definition.CandidatesPerArm) throw new InvalidDataException("Unequal search budget.");
            foreach (var evaluation in arm.Search.Evaluations)
            {
                var rows = evaluation.Trials.Select(id => trials[id]).ToArray();
                var results = rows.Select(t => (Floor: Recipe(t).FloorNumber,
                    Report: TowerLoadoutArchive.ReadBattle(output, t.Id, scope.ReportStorage))).ToArray();
                var fitness = new LoadoutFitness(results.Count(r => r.Floor == 1 && r.Report.Succeeded), results.Count(r => r.Report.Succeeded), results.Length,
                    results.Average(r => (double)r.Report.GuardianHealthRemainingPercent), results.Average(r => TowerBenchmark.Survival(r.Report)));
                if (rows.Any(t => t.Stage != "discovery") || rows.Length != 15 * definition.DiscoverySamples
                    || HarnessJson.Hash(fitness) != HarnessJson.Hash(evaluation.Fitness) || HarnessJson.Hash(evaluation.Essences) != evaluation.Id)
                    throw new InvalidDataException("Discovery scores differ from recorded combat trials.");
                if (definition.SchemaVersion == 2)
                    foreach (var scenario in contexts[arm.Cohort])
                    {
                        var recipe = Apply(scenario, definition.TargetPartySlot, new(evaluation.Id, "verification", evaluation.Essences),
                            Schedule(definition.DiscoverySeed, definition.DiscoverySamples)[scenario.FloorNumber]);
                        var group = rows.Where(t => Recipe(t).FloorNumber == scenario.FloorNumber).ToArray();
                        if (!group.Select(t => t.Seed).SequenceEqual(recipe.Seeds) || group.Any(t => t.Recipe != HarnessJson.Hash(recipe)))
                            throw new InvalidDataException("Discovery changed a candidate, its allies, budget or paired seed schedule.");
                    }
            }
        }
        var expected = TowerLoadoutReliability.Select(definition, contexts, report.Discovery);
        if (HarnessJson.Hash(expected) != HarnessJson.Hash(report.Selection)
            || HarnessJson.Hash(expected) != HarnessJson.Hash(HarnessJson.Read<LoadoutCohortSelection[]>(Path.Combine(output, "selection.json"))))
            throw new InvalidDataException("Frozen selection differs from discovery ranking.");
        if (definition.SchemaVersion == 2)
        {
            VerifyConfirmation(output, definition, contexts, trials.Values.ToArray(), report, scope, token);
            if (HarnessJson.Hash(TowerLoadoutReliability.Summarize(report, definition)) != HarnessJson.Hash(
                HarnessJson.Read<LoadoutReliabilityReport>(Path.Combine(output, "reliability.json"))))
                throw new InvalidDataException("Reliability comparisons differ from confirmation.");
        }
        return report;
    }

    private static LoadoutConfirmation ConfirmationCell(string cohort, string candidate, int floor,
        IReadOnlyList<TowerBattleReport> reports, IReadOnlyList<TowerBattleReport> control)
    {
        var wins = reports.Count(r => r.Succeeded);
        var gained = reports.Zip(control).Count(p => p.First.Succeeded && !p.Second.Succeeded);
        var lost = reports.Zip(control).Count(p => !p.First.Succeeded && p.Second.Succeeded);
        return new(cohort, candidate, floor, wins, reports.Count(r => r.Battle.Summary.ContentOutcome == Domain.Models.Combat.BattleOutcome.Draw),
            reports.Count, SuiteScorecard.Wilson(wins, reports.Count)!, control.Count(r => r.Succeeded), gained, lost,
            PairedStatistics.ClearRate(gained, lost, reports.Count), reports.Average(r => (double)r.GuardianHealthRemainingPercent), reports.Average(TowerBenchmark.Survival));
    }

    private static void VerifyConfirmation(string output, TowerLoadoutPilotDefinition d,
        IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> contexts, IReadOnlyList<LoadoutTrial> trials,
        TowerLoadoutPilotReport report, LoadoutScope scope, CancellationToken token)
    {
        var schedule = Schedule(d.ConfirmationSeed, d.ConfirmationSamples);
        var groups = trials.Where(t => t.Stage == "confirmation").GroupBy(t => t.Recipe).ToDictionary(g => g.Key, g => g.ToArray());
        var expected = new List<LoadoutConfirmation>(); var consumed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in report.Selection)
        {
            var control = new Dictionary<int, IReadOnlyList<TowerBattleReport>>();
            foreach (var finalist in selection.Finalists)
                foreach (var scenario in contexts[selection.Cohort])
                {
                    token.ThrowIfCancellationRequested();
                    var recipe = Apply(scenario, d.TargetPartySlot, finalist, schedule[scenario.FloorNumber]);
                    var key = HarnessJson.Hash(recipe);
                    if (!groups.TryGetValue(key, out var group) || !group.Select(t => t.Seed).SequenceEqual(recipe.Seeds))
                        throw new InvalidDataException("Confirmation changed a frozen recipe, ally context or seed schedule.");
                    consumed.Add(key);
                    var reports = group.Select(t => TowerLoadoutArchive.ReadBattle(output, t.Id, scope.ReportStorage)).ToArray();
                    if (!reports.Select(r => r.Battle.Seed).SequenceEqual(recipe.Seeds) || reports.Any(r => r.Battle.ScenarioId != recipe.Id))
                        throw new InvalidDataException("Confirmation result identity changed.");
                    if (finalist.Id == "control") control.Add(scenario.FloorNumber, reports);
                    expected.Add(ConfirmationCell(selection.Cohort, finalist.Id, scenario.FloorNumber, reports, control[scenario.FloorNumber]));
                }
        }
        if (consumed.Count != groups.Count || HarnessJson.Hash(expected) != HarnessJson.Hash(report.Confirmation))
            throw new InvalidDataException("Confirmation metrics differ from saved fights.");
    }

    public static async Task<TowerLoadoutPilotReport> RunAsync(string root, string catalogs, string output,
        TowerLoadoutPilotDefinition definition, CancellationToken token = default, Action<string>? progress = null)
    {
        var planned = Validate(definition);
        if (Path.Exists(output)) throw new IOException("Pilot output exists; choose a new directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "searches" }) Directory.CreateDirectory(Path.Combine(output, folder));
        var status = "Invalid";
        var discovery = new List<LoadoutMethodResult>(); var selections = new List<LoadoutCohortSelection>();
        var confirmation = new List<LoadoutConfirmation>(); TowerLoadoutArchive? archive = null;
        try
        {
            var settings = TowerBundle.ReadSettings(root);
            var frozen = Path.Combine(output, "content");
            var scope = new LoadoutScope(LoadoutSearch.Version, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(root, frozen, token),
                definition.SchemaVersion == 2 ? "gzip-json-v1" : null);
            HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope);
            var mechanics = EssenceMechanicsInventory.Create(frozen, settings.Threat);
            HarnessJson.WriteNew(Path.Combine(output, "mechanics.json"), mechanics);
            File.WriteAllText(Path.Combine(output, "mechanics.md"), EssenceMechanicsInventory.Markdown(mechanics));
            var pool = (definition.AllowedEssences ?? mechanics.Essences.Select(e => e.Id).ToArray()).ToArray();
            if (pool.Distinct(StringComparer.Ordinal).Count() != pool.Length || pool.Any(id => !mechanics.Essences.Any(e => e.Id == id)))
                throw new InvalidDataException("Allowed pool contains duplicate or unknown Essence IDs.");
            definition = definition with { AllowedEssences = pool.Order(StringComparer.Ordinal).ToArray() };
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
            var families = mechanics.Essences.Where(e => pool.Contains(e.Id)).ToDictionary(e => e.Id, e => e.SourceMonsterId);
            foreach (var candidate in definition.FixedCandidates ?? [])
                if (candidate.Essences.Any(id => !families.ContainsKey(id)) || candidate.Essences.Select(id => families[id]).Distinct().Count() != 4)
                    throw new InvalidDataException("Fixed candidates must belong to the allowed pool and use distinct source families.");
            var schedules = Schedule(definition.DiscoverySeed, definition.DiscoverySamples);
            var heldOut = Schedule(definition.ConfirmationSeed, definition.ConfirmationSamples);
            HarnessJson.WriteNew(Path.Combine(output, "seed-ledger.json"), new { Discovery = schedules, Confirmation = heldOut,
                ExcludedHistorical = definition.ExcludedCombatSeeds ?? [],
                Note = "Confirmation is unused until global selection.json is saved. Search-seed restarts share discovery combat seeds; do not pool them as independent samples." });
            var cohorts = TowerLoadoutReliability.Contexts(definition, frozen, catalogs);
            HarnessJson.WriteNew(Path.Combine(output, "contexts.json"), cohorts);
            var validator = new TowerBattleRunner(frozen, new OfflineContent(frozen, settings.Threat));
            foreach (var scenario in cohorts.Values.SelectMany(s => s))
                await validator.PrepareAsync(validator.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token);
            archive = new(output, scope, definition.MaximumBattles);
            progress?.Invoke($"Pilot upper bound: {planned} combat trials; {definition.Gear.Count} separate gear cohorts, all 15 floors; one worker.");
            foreach (var cohort in cohorts)
            {
                var original = cohort.Value[0].Party.Single(p => p.PartySlot == definition.TargetPartySlot).Build.EssenceIds;
                if (original.Any(id => !families.ContainsKey(id))) throw new InvalidDataException("Allowed pool must include the control.");
                // One direct-mechanics hypothesis; unknown mechanics remain in the uniform random pool.
                var signal = definition.TargetPartySlot == 2 ? "operation:Heal" : "operation:Damage";
                var authored = mechanics.Essences.Where(e => families.ContainsKey(e.Id) && e.Signals.Contains(signal))
                    .OrderBy(e => StableRandom.Seed("pilot-mechanics-start-v1", e.Id)).GroupBy(e => e.SourceMonsterId).Take(4).Select(g => g.First().Id).ToArray();
                foreach (var seed in definition.SearchSeeds)
                    foreach (var method in new[] { "guided", "random" })
                    {
                        var arm = $"{cohort.Key}-{method}-{seed}";
                        var search = await LoadoutSearch.RunAsync(method, seed, definition.CandidatesPerArm, 4000, original, families,
                            authored.Length == 4 ? [authored] : [], async (ids, ct) =>
                            {
                                var reports = new List<(int Floor, TowerBattleReport Report)>(); var trialIds = new List<string>();
                                foreach (var scenario in cohort.Value)
                                {
                                    var recipe = Apply(scenario, definition.TargetPartySlot, new(HarnessJson.Hash(ids), method, ids), schedules[scenario.FloorNumber]);
                                    foreach (var combatSeed in recipe.Seeds)
                                    {
                                        var trial = await archive.EvaluateAsync(arm, "discovery", recipe, combatSeed, ct);
                                        reports.Add((scenario.FloorNumber, trial.Report)); trialIds.Add(trial.Trial.Id);
                                    }
                                }
                                progress?.Invoke($"{arm}: candidate measured; {archive.Trials.Count}/{planned} combat trials.");
                                return (new LoadoutFitness(reports.Count(r => r.Floor == 1 && r.Report.Succeeded), reports.Count(r => r.Report.Succeeded), reports.Count,
                                    reports.Average(r => (double)r.Report.GuardianHealthRemainingPercent), reports.Average(r => TowerBenchmark.Survival(r.Report))), (IReadOnlyList<string>)trialIds);
                            }, token, proposal => File.AppendAllText(Path.Combine(output, "searches", arm + ".jsonl"),
                                JsonSerializer.Serialize(proposal, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n"));
                        HarnessJson.WriteNew(Path.Combine(output, "searches", arm + ".json"), search);
                        discovery.Add(new(cohort.Key, search));
                        if (search.Evaluations.Count != definition.CandidatesPerArm)
                            throw new InvalidDataException("Proposal budget exhausted before equal combat budgets were reached; comparison is incomplete.");
                    }
            }
            selections.AddRange(TowerLoadoutReliability.Select(definition, cohorts, discovery));
            // All cohorts' controls and arm winners are frozen before ANY confirmation fight.
            HarnessJson.WriteNew(Path.Combine(output, "selection.json"), selections);
            foreach (var selection in selections)
            {
                var control = new Dictionary<int, List<TowerBattleReport>>();
                foreach (var finalist in selection.Finalists)
                    foreach (var scenario in cohorts[selection.Cohort])
                    {
                        var recipe = Apply(scenario, definition.TargetPartySlot, finalist, heldOut[scenario.FloorNumber]);
                        var reports = new List<TowerBattleReport>();
                        foreach (var seed in recipe.Seeds)
                            reports.Add((await archive.EvaluateAsync("confirmation-" + selection.Cohort, "confirmation", recipe, seed, token)).Report);
                        if (finalist.Id == "control") control.Add(scenario.FloorNumber, reports);
                        confirmation.Add(ConfirmationCell(selection.Cohort, finalist.Id, scenario.FloorNumber, reports, control[scenario.FloorNumber]));
                    }
                progress?.Invoke($"Confirmed {selection.Cohort}; {archive.Trials.Count}/{planned} combat trials.");
            }
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        catch (Exception error) { HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { error.Message, Type = error.GetType().Name }); throw; }
        finally
        {
            var report = new TowerLoadoutPilotReport(status, planned, archive?.Trials.Count ?? 0, archive?.CacheHits ?? 0, discovery, selections, confirmation);
            HarnessJson.WriteNew(Path.Combine(output, "pilot.json"), report);
            var markdown = Markdown(report, definition.SchemaVersion == 2);
            if (status == "Complete" && definition.SchemaVersion == 2)
            {
                var reliability = TowerLoadoutReliability.Summarize(report, definition);
                HarnessJson.WriteNew(Path.Combine(output, "reliability.json"), reliability);
                markdown += TowerLoadoutReliability.Markdown(reliability);
            }
            File.WriteAllText(Path.Combine(output, "pilot.md"), markdown);
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));
        }
        return new(status, planned, archive!.Trials.Count, archive.CacheHits, discovery, selections, confirmation);
    }

    public static string Markdown(TowerLoadoutPilotReport report, bool reliability = false)
    {
        var text = new StringBuilder($"# Four-Essence Tower character pilot\n\nStatus: {report.Status}. {report.ActualBattles}/{report.PlannedMaximum} upper-bound combat trials; {report.CacheHits} exact cache hits.\n\n");
        text.AppendLine("Best found within the declared budget, not optimal or universally recommended. Rank discovery by floor-1 wins, then all-floor wins, lower mean guardian health, higher survival, stable ID. Each arm has the same actual combat budget; each search seed shares paired discovery combat seeds. Do not pool search restarts as independent combat samples. Uniform random arms include the same control plus uniformly sampled legal ordered loadouts.\n");
        text.AppendLine("Only the selected character in the first five-member cell changes within each fixed ally context. All characters retain four Essences at level 30, Uncommon tier-1 gear, level-1 unascended/unevolved Essences. Quality/rank cohorts stay separate. Later-floor results test coverage, not the six-slot floor-10 progression target.\n");
        text.AppendLine(reliability
            ? "Global selection.json freezes one common union per gear budget: control, predeclared historical loadouts and every arm winner from both ally contexts. Every finalist then runs every floor with BOTH original and candidate-05 allies. The target keeps its original recipe/identity across ally contexts. Duplicates merge. New combat schedules exclude the declared historical seed ledger. Full battle reports use lossless gzip JSON.\n"
            : "Global selection.json freezes the control, historical party comparator and each arm's winner before confirmation; duplicates merge. The historical comparator changes allies and is excluded from character ranking.\n");
        text.AppendLine("No confirmation reselection. Wilson and paired 95% intervals are descriptive, with no multiple-comparison guarantee, balance gate or starter target. Exported recipes regenerate through normal Tower preparation; replay uses captured content and exact input/execution hashes.\n");
        text.AppendLine("| Cohort | Method | Search seed | Candidates | Winner | Entry / all-floor discovery wins |\n| --- | --- | --- | --- | --- | --- |");
        foreach (var arm in report.Discovery)
        {
            var best = LoadoutSearch.Rank(arm.Search.Evaluations).First();
            text.AppendLine($"| {arm.Cohort} | {arm.Search.Method} | {arm.Search.Seed} | {arm.Search.Evaluations.Count} | {best.Id[..12]} | {best.Fitness.EntryWins} / {best.Fitness.Wins} | ");
        }
        text.AppendLine("\n## Frozen recipes\n");
        foreach (var selection in report.Selection)
            foreach (var candidate in selection.Finalists)
                text.AppendLine($"- {selection.Cohort}, **{candidate.Id}** ({candidate.Source}): {string.Join(", ", candidate.Essences)}.\n");
        text.AppendLine("## Confirmation on every floor\n\n| Cohort | Candidate | Floor | Wins / trials | Draws | Wilson 95% clear % | Control wins | Gained / lost | Paired change pp [95%] |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var row in report.Confirmation)
            text.AppendLine(FormattableString.Invariant($"| {row.Cohort} | {row.Candidate} | {row.Floor} | {row.Wins}/{row.Trials} | {row.Draws} | {100 * row.ClearRate.Lower:F1}–{100 * row.ClearRate.Upper:F1} | {row.ControlWins} | {row.Gained}/{row.Lost} | {row.Change.MeanChange:F1} [{row.Change.Lower:F1}, {row.Change.Upper:F1}] |"));
        text.AppendLine("\nLimits: only the declared ally contexts and search/sample budget; no joint party optimization, no generalist/specialist archive beyond arm winners, no resume or multiworker support. Mechanic starts use direct effect signals only; unknown mechanics stay eligible. Phase 2 remains deferred; no production balance change or baseline promotion.\n");
        return text.ToString();
    }
}
