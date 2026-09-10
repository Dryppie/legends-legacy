using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

/// <summary>Three-stage experiment. Verification reruns selection with recorded combats instead of simulating.</summary>
public static class TowerPartySearch
{
    public static TowerPartySearchDefinition Read(string path) => JsonSerializer.Deserialize<TowerPartySearchDefinition>(File.ReadAllText(path),
        new JsonSerializerOptions(HarnessJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
        ?? throw new InvalidDataException("Empty party search definition.");

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(string root, string catalogs, int target, TowerSearchBudget? budget = null)
    {
        if (budget is not null) return TowerPartyProgression.Contexts(root, catalogs, target, budget);
        var d = TowerLoadoutReliability.Default with { TargetPartySlot = target == 0 ? 2 : target };
        var contexts = TowerLoadoutReliability.Contexts(d, root, catalogs);
        if (target != 0) return contexts;
        var original = contexts.Values.First();
        return contexts.ToDictionary(c => c.Key, c => (IReadOnlyList<TowerScenario>)c.Value.Select(s => s with
        { Party = s.Party.Select(p => TowerPartySelection.Slots.Contains(p.PartySlot)
            ? original.Single(o => o.FloorNumber == s.FloorNumber).Party.Single(o => o.PartySlot == p.PartySlot) : p).ToArray(),
            Assumptions = [.. s.Assumptions, "Joint search: first-cell slots 1–4 vary together. Alternative allies differ only in later cells; floor-1 parties are identical across contexts."] }).ToArray());
    }

    private delegate Task<(LoadoutTrial Trial, TowerBattleReport Report)> Evaluate(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token);

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(string root, string catalogs, int target, TowerPartySearchDefinition d) =>
        d.SchemaVersion == 3 ? TowerWholeParty.Contexts(root, catalogs, target, d) : Contexts(root, catalogs, target, d.Budget);

    private static async Task<PartySearchReport> Execute(TowerPartySearchDefinition d,
        IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>>> contexts,
        IReadOnlyDictionary<string, string> families, Evaluate evaluate, Action<string, object> snapshot,
        Action<string, LoadoutProposal>? proposal, CancellationToken token, Action<string>? progress)
    {
        var arms = new List<CharacterSearchArm>(); var shortlists = new List<CharacterShortlist>();
        var characterSchedule = TowerLoadoutPilot.Schedule(d.CharacterSeed, d.CharacterSamples);
        var partySchedule = TowerLoadoutPilot.Schedule(d.PartySeed, d.PartySamples);
        var confirmationSchedule = TowerLoadoutPilot.Schedule(d.ConfirmationSeed, d.ConfirmationSamples);
        var actual = 0;
        async Task<PartyMeasurement> Measure(string id, string arm, string stage, int target,
            IReadOnlyDictionary<int, IReadOnlyList<string>> builds, IReadOnlyDictionary<int, IReadOnlyList<int>> schedule,
            PartyMeasurement? control, CancellationToken ct)
        {
            var cells = new List<PartyFloorScore>();
            foreach (var context in contexts[target])
                foreach (var scenario in context.Value)
                {
                    var recipe = TowerPartySelection.Apply(scenario, builds, schedule[scenario.FloorNumber]);
                    var reports = new List<TowerBattleReport>(); var trials = new List<string>();
                    foreach (var seed in recipe.Seeds)
                    {
                        // Whole-party deployments can make contexts identical. Charge the declared
                        // actual combat budget in each context instead of silently sharing cached trials.
                        var result = await evaluate(d.SchemaVersion == 3 ? arm + "@" + context.Key : arm, stage, recipe, seed, ct);
                        actual++; reports.Add(result.Report); trials.Add(result.Trial.Id);
                    }
                    cells.Add(TowerPartySelection.Cell(context.Key, scenario.FloorNumber, reports, trials));
                }
            return new(id, TowerPartySelection.Fitness(cells, control?.Cells ?? cells, d.Budget?.PriorityFloor ?? 1), cells);
        }
        foreach (var slot in TowerPartySelection.Targets(d))
        {
            var original = contexts[slot].Values.First()[0].Party.Single(p => p.PartySlot == slot).Build.EssenceIds;
            IReadOnlyList<IReadOnlyList<string>> starts = d.StartingLoadouts is not null ? d.StartingLoadouts[slot] : slot == 2
                ? [.. TowerLoadoutReliability.Default.FixedCandidates!.Select(c => c.Essences), TowerPartySelection.CandidateC.Essences] : [];
            foreach (var seed in d.SearchSeeds)
                foreach (var method in new[] { "guided", "random" })
                {
                    var arm = $"slot-{slot}-{method}-{seed}";
                    var measured = new List<PartyMeasurement>();
                    var search = await LoadoutSearch.RunAsync(method, seed, d.CandidatesPerArm, 4000, original, families, starts,
                        async (ids, ct) =>
                        {
                            var row = await Measure(HarnessJson.Hash(ids), arm, "character", slot,
                                new Dictionary<int, IReadOnlyList<string>> { [slot] = ids }, characterSchedule, measured.FirstOrDefault(), ct);
                            measured.Add(row);
                            progress?.Invoke($"{arm}: {measured.Count}/{d.CandidatesPerArm} candidates; {actual} combats.");
                            return (row.Fitness, (IReadOnlyList<string>)row.Cells.SelectMany(c => c.Trials).ToArray());
                        }, token, p => proposal?.Invoke(arm, p), sharedStarts: true);
                    var result = new CharacterSearchArm(slot, search, measured);
                    arms.Add(result); snapshot("searches/" + arm + ".json", result);
                    if (search.Evaluations.Count != d.CandidatesPerArm) throw new InvalidDataException("Incomplete equal-budget search: proposal cap exhausted.");
                }
            shortlists.Add(TowerPartySelection.Shortlist(slot, arms.Where(a => a.Slot == slot).ToArray()));
        }
        snapshot("shortlists.json", shortlists);
        var parties = TowerPartySelection.Combinations(d, shortlists, arms);
        snapshot("party-candidates.json", parties);
        var discovery = new List<PartyMeasurement>();
        foreach (var party in parties)
        {
            discovery.Add(await Measure(party.Id, "party-" + party.Id, "party", 0, party.Builds, partySchedule, discovery.FirstOrDefault(), token));
            snapshot("parties/" + party.Id + ".json", discovery[^1]);
            progress?.Invoke($"Party screen {discovery.Count}/{parties.Count}; {actual} combats.");
        }
        var selection = TowerPartySelection.Select(d, parties, discovery, arms);
        snapshot("selection.json", selection);
        var confirmation = new List<PartyMeasurement>();
        foreach (var party in selection)
        {
            confirmation.Add(await Measure(party.Id, "confirmation-" + party.Id, "confirmation", 0, party.Builds, confirmationSchedule, confirmation.FirstOrDefault(), token));
            snapshot("confirmation/" + party.Id + ".json", confirmation[^1]);
            progress?.Invoke($"Confirmed party {confirmation.Count}/{selection.Count}; {actual} combats.");
        }
        if (d.SchemaVersion == 3) snapshot("deployment-comparisons.json", TowerWholeParty.Comparisons(d, arms, discovery, confirmation));
        return new("Complete", TowerPartySelection.Validate(d), actual, arms, shortlists, parties, discovery, selection, confirmation);
    }

    public static async Task<PartySearchReport> RunAsync(string root, string catalogs, string output,
        TowerPartySearchDefinition d, CancellationToken token = default, Action<string>? progress = null)
    {
        var planned = TowerPartySelection.Validate(d);
        if (Path.Exists(output)) throw new IOException("Output exists; choose a new directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "searches", "parties", "confirmation" }) Directory.CreateDirectory(Path.Combine(output, folder));
        TowerLoadoutArchive? archive = null; PartySearchReport? report = null; var status = "Invalid";
        try
        {
            var frozen = Path.Combine(output, "content"); var settings = TowerBundle.ReadSettings(root);
            var scope = new LoadoutScope(TowerPartyProgression.Algorithm(d), settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(root, frozen, token), "gzip-json-v1");
            HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope);
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), d);
            var mechanics = EssenceMechanicsInventory.Create(frozen, settings.Threat);
            HarnessJson.WriteNew(Path.Combine(output, "mechanics.json"), mechanics);
            var families = mechanics.Essences.ToDictionary(e => e.Id, e => e.SourceMonsterId);
            foreach (var ids in (d.StartingLoadouts?.Values.SelectMany(s => s) ?? []).Concat(d.ReferenceBuilds?.Values ?? [])
                .Concat(d.WholeParty?.Controls.SelectMany(p => p.Builds.Values) ?? []))
                if (ids.Any(id => !families.ContainsKey(id)) || ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
                    throw new InvalidDataException("Starting/reference loadouts contain unknown Essences or duplicate source families.");
            Directory.CreateDirectory(Path.Combine(output, "catalogs"));
            File.Copy(Path.Combine(catalogs, "tower-curve.json"), Path.Combine(output, "catalogs", "tower-curve.json"));
            var contexts = TowerPartySelection.Targets(d).Prepend(0).ToDictionary(slot => slot, slot => Contexts(frozen, catalogs, slot, d));
            HarnessJson.WriteNew(Path.Combine(output, "contexts.json"), contexts);
            HarnessJson.WriteNew(Path.Combine(output, "seed-ledger.json"), Ledger(d));
            var validator = new TowerBattleRunner(frozen, new OfflineContent(frozen, settings.Threat));
            foreach (var scenario in contexts.Values.SelectMany(c => c.Values).SelectMany(s => s))
                await validator.PrepareAsync(validator.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token);
            archive = new(output, scope, d.MaximumBattles);
            progress?.Invoke($"Party search upper bound {planned}; {TowerPartySelection.Targets(d).Count} character positions, two contexts, all 15 floors, one worker.");
            report = await Execute(d, contexts, families, archive.EvaluateAsync,
                (path, value) => HarnessJson.WriteNew(Path.Combine(output, path), value),
                (arm, p) => File.AppendAllText(Path.Combine(output, "searches", arm + ".jsonl"), JsonSerializer.Serialize(p,
                    new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n"), token, progress);
            if (archive.CacheHits != 0 || archive.Trials.Count != report.ActualBattles) throw new InvalidDataException("Actual combat accounting changed.");
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        catch (Exception error) { HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { error.Message, Type = error.GetType().Name }); throw; }
        finally
        {
            report ??= new(status, planned, archive?.Trials.Count ?? 0, [], [], [], [], [], []);
            report = report with { Status = status };
            HarnessJson.WriteNew(Path.Combine(output, "party-search.json"), report);
            File.WriteAllText(Path.Combine(output, "party-search.md"), TowerPartySelection.Markdown(report, d));
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));
        }
        return report;
    }

    private static object Ledger(TowerPartySearchDefinition d) => new { Character = TowerLoadoutPilot.Schedule(d.CharacterSeed, d.CharacterSamples),
        Party = TowerLoadoutPilot.Schedule(d.PartySeed, d.PartySamples), Confirmation = TowerLoadoutPilot.Schedule(d.ConfirmationSeed, d.ConfirmationSamples),
        ExcludedHistorical = d.ExcludedCombatSeeds, Note = "Three disjoint schedules; confirmation starts only after selection.json. Contexts and method restarts share paired seeds, not independent extra samples." };

    public static async Task<PartySearchReport> VerifyAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        var trials = TowerLoadoutArchive.Verify(output, token);
        var report = HarnessJson.Read<PartySearchReport>(Path.Combine(output, "party-search.json"));
        var d = Read(Path.Combine(output, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        if (report.Status != "Complete" || report.ActualBattles != trials.Count || report.PlannedMaximum != TowerPartySelection.Validate(d)
            || scope.Algorithm != TowerPartyProgression.Algorithm(d)) throw new InvalidDataException("Incomplete or incompatible party study.");
        void Snapshot(string path, object value)
        {
            var saved = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(output, path)), HarnessJson.Options);
            if (HarnessJson.Hash(saved) != HarnessJson.Hash(value)) throw new InvalidDataException("Reconstructed selection/report changed: " + path);
        }
        Snapshot("seed-ledger.json", Ledger(d));
        var root = Path.Combine(output, "content");
        foreach (var content in scope.ContentHashes)
            if (HarnessJson.FileHash(Path.Combine(root, "Data", content.Key)) != content.Value) throw new InvalidDataException("Frozen content changed.");
        var mechanics = EssenceMechanicsInventory.Create(root, scope.Settings.Threat);
        Snapshot("mechanics.json", mechanics);
        var contexts = HarnessJson.Read<Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>>>>(Path.Combine(output, "contexts.json"));
        ValidateContexts(contexts, d);
        Snapshot("contexts.json", TowerPartySelection.Targets(d).Prepend(0).ToDictionary(slot => slot,
            slot => Contexts(root, Path.Combine(output, "catalogs"), slot, d)));
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var index = 0;
        var proposals = new Dictionary<string, List<LoadoutProposal>>();
        var rebuilt = await Execute(d, contexts, mechanics.Essences.ToDictionary(e => e.Id, e => e.SourceMonsterId),
            (arm, stage, scenario, seed, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                if (index >= trials.Count) throw new InvalidDataException("Missing recorded combat.");
                var trial = trials[index++];
                var input = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                if (trial.Stage != stage || trial.Seed != seed || trial.Recipe != HarnessJson.Hash(scenario)
                    || trial.InputHash != HarnessJson.Hash(input) || trial.CacheKey != TowerLoadoutArchive.Key(scope, arm, input))
                    throw new InvalidDataException("Recorded combat differs from deterministic recipe, stage, arm or seed schedule.");
                var savedRecipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", trial.Recipe + ".json"));
                if (HarnessJson.Hash(savedRecipe) != trial.Recipe) throw new InvalidDataException("Saved export recipe changed.");
                var battle = TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage);
                if (battle.Battle.Seed != seed || battle.Battle.ScenarioId != scenario.Id) throw new InvalidDataException("Saved battle identity changed.");
                return Task.FromResult((trial, battle));
            }, Snapshot, (arm, p) => { if (!proposals.TryGetValue(arm, out var list)) proposals.Add(arm, list = []); list.Add(p); }, token, progress);
        foreach (var arm in proposals)
        {
            var saved = File.ReadLines(Path.Combine(output, "searches", arm.Key + ".jsonl")).Select(s => JsonSerializer.Deserialize<LoadoutProposal>(s, HarnessJson.Options)).ToArray();
            if (HarnessJson.Hash(saved) != HarnessJson.Hash(arm.Value)) throw new InvalidDataException("Candidate proposal history changed.");
        }
        if (index != trials.Count || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(report)) throw new InvalidDataException("Report differs from reconstructed three-stage experiment.");
        if (File.ReadAllText(Path.Combine(output, "party-search.md")) != TowerPartySelection.Markdown(rebuilt, d)) throw new InvalidDataException("Markdown differs from verified report.");
        return rebuilt;
    }

    private static void ValidateContexts(IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>>> contexts, TowerPartySearchDefinition d)
    {
        var budget = d.Budget;
        if (!contexts.Keys.Order().SequenceEqual(TowerPartySelection.Targets(d).Prepend(0))) throw new InvalidDataException("Missing character or joint contexts.");
        foreach (var pair in contexts)
        {
            var prefix = budget is null ? "standard-rank-1" : $"slots-{budget.EssenceSlots}";
            if (!pair.Value.Keys.SequenceEqual(new[] { prefix + "--balanced", prefix + "--previous-05" })) throw new InvalidDataException("Changed ally contexts.");
            foreach (var group in pair.Value.Values)
                if (!group.Select(s => s.FloorNumber).SequenceEqual(Enumerable.Range(1, 15))) throw new InvalidDataException("Missing floors.");
        }
    }
}
