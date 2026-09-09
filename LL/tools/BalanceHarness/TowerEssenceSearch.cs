using System.Text;

namespace BalanceHarness;

public sealed record TowerEssenceSwap(string Role, string Remove, string Add, string Rationale);
public sealed record TowerEssenceSearchDefinition(int SchemaVersion, string BaseCatalog, string BaseParty,
    int DiscoverySeed, int ConfirmationSeed, int DiscoverySamples, int ConfirmationSamples,
    IReadOnlyList<TowerEssenceSwap> Swaps);
public sealed record TowerSearchRank(string Party, int Wins, int Trials, double MeanGuardianHealth, double MeanSurvival);
public sealed record TowerSearchSelection(IReadOnlyList<string> Parties, IReadOnlyList<TowerSearchRank> Ranking,
    IReadOnlyDictionary<int, string> FloorWinners);
public sealed record TowerSearchPair(int Floor, string Party, int Wins, int Draws, int Trials,
    int ControlWins, int GainedWins, int LostWins, PairedEstimate ClearRateChange);
public sealed record TowerSearchRoleEvidence(int Floor, string Party, string Role, double Damage,
    double Healing, double DamageTaken, double AttentionPercent, double Stagger, double? FirstDeathSeconds);
public sealed record TowerSearchReport(int SchemaVersion, string Status, TowerSearchSelection Selection,
    IReadOnlyList<TowerSearchPair> Confirmation, IReadOnlyList<TowerSearchRoleEvidence> RoleEvidence,
    IReadOnlyDictionary<string, string> CandidateDescriptions);

/// <summary>Finite factorial search of authored Essence substitutions. Never changes equipment or balance content.</summary>
public static class TowerEssenceSearch
{
    public const string ConfigFile = "tower-essence-search.json";
    public const string Control = "candidate-00";

    public static TowerBenchmarkDefinition Generate(TowerEssenceSearchDefinition search, TowerBenchmarkDefinition baseline)
    {
        if (search.SchemaVersion != 1 || search.Swaps.Count is < 1 or > 4
            || search.Swaps.Select(s => s.Role).Distinct().Count() != search.Swaps.Count
            || search.Swaps.Any(s => !TowerBenchmark.SafeId(s.Role) || s.Remove == s.Add || string.IsNullOrWhiteSpace(s.Rationale))
            || search.DiscoverySamples is < 1 or > 20 || search.ConfirmationSamples is < 1 or > 100
            || search.DiscoverySeed == search.ConfirmationSeed
            || Path.GetFileName(search.BaseCatalog) != search.BaseCatalog || !search.BaseCatalog.EndsWith(".json", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid bounded Essence search: use 1–4 distinct role substitutions, separate seeds and bounded samples.");
        var parent = baseline.Parties.Single(p => p.Id == search.BaseParty);
        var profiles = baseline.Profiles.ToList();
        var variants = new Dictionary<string, string>();
        foreach (var swap in search.Swaps)
        {
            var matches = baseline.Profiles.Where(p => p.Id.EndsWith("-" + swap.Role, StringComparison.Ordinal)).ToArray();
            if (matches.Length == 0) throw new InvalidDataException("Search role has no base profiles.");
            foreach (var profile in matches)
            {
                if (!profile.Build.EssenceIds.Contains(swap.Remove) || profile.Build.EssenceIds.Contains(swap.Add))
                    throw new InvalidDataException("A substitution must replace one owned Essence with one new Essence in every role budget.");
                var id = "search-" + profile.Id;
                variants.Add(profile.Id, id);
                profiles.Add(profile with { Id = id, Assumptions = profile.Assumptions + " Search hypothesis: " + swap.Rationale,
                    Build = profile.Build with { Id = id, EssenceIds = profile.Build.EssenceIds.Select(e => e == swap.Remove ? swap.Add : e).ToArray() } });
            }
        }
        var parties = new List<TowerBenchmarkParty>();
        for (var mask = 0; mask < (1 << search.Swaps.Count); mask++)
        {
            var enabled = search.Swaps.Where((_, bit) => (mask & (1 << bit)) != 0).ToArray();
            IReadOnlyList<string> Cell(IReadOnlyList<string> cell) => cell.Select(id =>
                enabled.Any(s => id.EndsWith("-" + s.Role, StringComparison.Ordinal)) ? variants[id] : id).ToArray();
            parties.Add(new($"candidate-{mask:D2}", parent.Assumptions + " Fixed-budget Essence search. "
                + (mask == 0 ? "Unchanged control Essence selections." : string.Join(" ", enabled.Select(s => $"{s.Role}: {s.Remove} → {s.Add}. {s.Rationale}"))),
                Cell(parent.CellProfiles), parent.FloorCellProfiles?.ToDictionary(p => p.Key, p => Cell(p.Value))));
        }
        return baseline with { Id = "tower-essence-search-v1", Profiles = profiles, Parties = parties, SamplesPerCell = search.DiscoverySamples };
    }

    public static TowerSearchSelection Select(TowerBenchmarkReport report)
    {
        if (report.Status != "Complete" || report.Cells.Any(c => c.Status != "Complete" || c.Valid == 0)
            || !report.Cells.Any(c => c.Party == Control)) throw new InvalidDataException("Selection requires complete discovery and its control.");
        var floors = report.Cells.Select(c => c.Floor).Distinct().Count();
        if (report.Cells.GroupBy(c => c.Party).Any(g => g.Count() != floors)
            || report.Cells.Select(c => c.Valid).Distinct().Count() != 1)
            throw new InvalidDataException("Discovery must give every candidate the same floor/sample budget.");
        var ranking = report.Cells.GroupBy(c => c.Party).Select(g => new TowerSearchRank(g.Key,
                g.Sum(c => c.Wins), g.Sum(c => c.Valid), g.Average(c => c.GuardianHealthPercent.Mean!.Value), g.Average(c => c.SurvivalPercent.Mean!.Value)))
            .OrderByDescending(r => r.Wins).ThenBy(r => r.MeanGuardianHealth).ThenByDescending(r => r.MeanSurvival)
            .ThenBy(r => r.Party, StringComparer.Ordinal).ToArray();
        var winners = report.Cells.GroupBy(c => c.Floor).OrderBy(g => g.Key).ToDictionary(g => g.Key,
            g => g.OrderByDescending(c => c.Wins).ThenBy(c => c.GuardianHealthPercent.Mean)
                .ThenByDescending(c => c.SurvivalPercent.Mean).ThenBy(c => c.Party, StringComparer.Ordinal).First().Party);
        var selected = new List<string> { Control };
        selected.AddRange(ranking.Where(r => r.Party != Control).Take(2).Select(r => r.Party));
        foreach (var winner in winners.OrderByDescending(p => report.Cells.Single(c => c.Floor == p.Key && c.Party == p.Value).Wins
                     - report.Cells.Single(c => c.Floor == p.Key && c.Party == Control).Wins).ThenBy(p => p.Key))
            if (selected.Count < 6 && !selected.Contains(winner.Value)) selected.Add(winner.Value);
        return new(selected, ranking, winners);
    }

    public static async Task<TowerSearchReport> RunAsync(string apiRoot, string catalogsRoot, string output,
        TowerEssenceSearchDefinition search, CancellationToken token = default, Action<string>? progress = null)
    {
        if (Path.Exists(output)) throw new IOException("Search output already exists.");
        // Validate the relative source name before accessing it.
        if (Path.GetFileName(search.BaseCatalog) != search.BaseCatalog) throw new InvalidDataException("Use a catalog filename.");
        var candidates = Generate(search, HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogsRoot, search.BaseCatalog)));
        Directory.CreateDirectory(output);
        var status = "Invalid";
        try
        {
            token.ThrowIfCancellationRequested();
            var source = Path.Combine(output, "source");
            TowerBundle.CopyContent(apiRoot, source, token);
            var settings = TowerBundle.ReadSettings(apiRoot);
            HarnessJson.WriteNew(Path.Combine(source, "appsettings.json"), new Dictionary<string, object>
            {
                ["Combat"] = new Dictionary<string, object> { ["ThreatAndTanking"] = settings.Threat,
                    ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = 1 } },
                ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = settings.CheckpointIntervalTicks }
            });
            var discoveryScenarios = TowerBenchmark.Expand(candidates, source, search.DiscoverySeed);
            var heldOut = TowerBenchmark.Expand(candidates with { Parties = [candidates.Parties[0]] }, source, search.ConfirmationSeed, search.ConfirmationSamples);
            if (discoveryScenarios.SelectMany(s => s.Seeds).Intersect(heldOut.SelectMany(s => s.Seeds)).Any())
                throw new InvalidDataException("Discovery and confirmation schedules overlap.");
            HarnessJson.WriteNew(Path.Combine(output, "search-config.json"), search);
            var catalog = Path.Combine(output, "candidates.json");
            HarnessJson.WriteNew(catalog, candidates);
            var discovery = await TowerBenchmark.RunAsync(source, catalog, Path.Combine(output, "discovery"),
                search.DiscoverySeed, token: token, progress: message => progress?.Invoke("Discovery · " + message));
            var selection = Select(discovery);
            // This file is written before any held-out combat. Confirmation cannot change the shortlist.
            HarnessJson.WriteNew(Path.Combine(output, "selection.json"), selection);
            var finalists = candidates with { Parties = candidates.Parties.Where(p => selection.Parties.Contains(p.Id)).ToArray(),
                SamplesPerCell = search.ConfirmationSamples };
            var confirmationCatalog = Path.Combine(output, "finalists.json");
            HarnessJson.WriteNew(confirmationCatalog, finalists);
            await TowerBenchmark.RunAsync(source, confirmationCatalog, Path.Combine(output, "confirmation"), search.ConfirmationSeed,
                token: token, progress: message => progress?.Invoke("Confirmation · " + message));
            var report = ReadReport(output, token);
            HarnessJson.WriteNew(Path.Combine(output, "search-report.json"), report);
            File.WriteAllText(Path.Combine(output, "search-report.md"), Markdown(report));
            status = "Complete";
            return report;
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        finally { HarnessJson.WriteNew(Path.Combine(output, "search-status.json"), new { Status = status }); }
    }

    public static TowerSearchReport ReadReport(string output, CancellationToken token = default)
    {
        var discovery = TowerBenchmark.ReadSaved(Path.Combine(output, "discovery"), token);
        var confirmation = TowerBenchmark.ReadSaved(Path.Combine(output, "confirmation"), token);
        var selection = Select(discovery.Report);
        if (confirmation.Report.Status != "Complete"
            || HarnessJson.Hash(selection) != HarnessJson.Hash(HarnessJson.Read<TowerSearchSelection>(Path.Combine(output, "selection.json")))
            || !selection.Parties.Order().SequenceEqual(confirmation.Input.Definition.Parties.Select(p => p.Id).Order())
            || !discovery.Input.Definition.Floors.SequenceEqual(confirmation.Input.Definition.Floors)
            || HarnessJson.Hash(discovery.Manifest.ContentHashes) != HarnessJson.Hash(confirmation.Manifest.ContentHashes)
            || HarnessJson.Hash(discovery.Manifest.Execution) != HarnessJson.Hash(confirmation.Manifest.Execution)
            || HarnessJson.Hash(discovery.Input.Settings) != HarnessJson.Hash(confirmation.Input.Settings)
            || discovery.Input.Scenarios.SelectMany(s => s.Seeds).Intersect(confirmation.Input.Scenarios.SelectMany(s => s.Seeds)).Any())
            throw new InvalidDataException("Search selection, content, execution or held-out evidence is incompatible.");
        foreach (var scenario in confirmation.Input.Scenarios)
        {
            var before = discovery.Input.Scenarios.Single(s => s.Id == scenario.Id);
            if (HarnessJson.Hash(before with { Seeds = scenario.Seeds }) != HarnessJson.Hash(scenario))
                throw new InvalidDataException("Confirmation changed a selected build or budget.");
        }
        var pairs = new List<TowerSearchPair>();
        var evidence = new List<TowerSearchRoleEvidence>();
        foreach (var cell in confirmation.Report.Cells)
        {
            token.ThrowIfCancellationRequested();
            var trials = confirmation.Cells[cell.Id].Scorecard.Trials;
            var control = confirmation.Cells[$"floor-{cell.Floor}.{Control}"].Scorecard.Trials;
            if (!trials.Select(t => t.Seed).SequenceEqual(control.Select(t => t.Seed))) throw new InvalidDataException("Unpaired confirmation seeds.");
            var gained = trials.Zip(control).Count(p => p.First.Report.Succeeded && !p.Second.Report.Succeeded);
            var lost = trials.Zip(control).Count(p => !p.First.Report.Succeeded && p.Second.Report.Succeeded);
            pairs.Add(new(cell.Floor, cell.Party, cell.Wins, cell.Draws, cell.Valid, control.Count(t => t.Report.Succeeded),
                gained, lost, PairedStatistics.ClearRate(gained, lost, cell.Valid)));
            // Match only original friendly participant IDs, excluding summons and hostiles.
            var stats = trials.SelectMany(t => t.Report.Battle.Summary.Statistics.Where(s =>
                t.Report.Battle.Summary.Friendly.Any(f => f.Id == s.EntityId))).GroupBy(s => s.EntityName.Split('-').Last());
            foreach (var group in stats)
                evidence.Add(new(cell.Floor, cell.Party, group.Key, group.Average(s => (double)s.DamageDone),
                    group.Average(s => (double)s.HealingDone), group.Average(s => (double)s.DamageTaken),
                    group.Average(s => (double)s.AttentionSharePercent), group.Average(s => (double)s.StaggerContributed),
                    group.Where(s => s.FirstDeathTick.HasValue).Select(s => s.FirstDeathTick / (double)trials[0].Report.Battle.TicksPerSecond).DefaultIfEmpty(null).Average()));
        }
        return new(1, "Complete", selection, pairs, evidence, discovery.Input.Definition.Parties.ToDictionary(p => p.Id,
            p => p.Assumptions.Split(" Fixed-budget Essence search. ", StringSplitOptions.None).Last()));
    }

    public static string Markdown(TowerSearchReport report)
    {
        var text = new StringBuilder("# Tower Essence build search\n\nBounded hypotheses, not proven optimal builds. Gear/level/slot budgets are fixed within each floor. No boss tuning or automatic Tower win-rate target.\n\n");
        text.AppendLine("Discovery ranks equal-budget candidates by total wins, then lower guardian health, higher party survival, and stable ID. Shortlist: unchanged control, two generalists, and up to three distinct floor winners ranked by win gain. Selection is frozen before independent confirmation. Intervals are nominal; no multiple-comparison guarantee or post-confirmation reselection.\n");
        text.AppendLine("Selected: " + string.Join(", ", report.Selection.Parties) + ". Full recipes are in candidates.json/finalists.json.\n");
        text.AppendLine("## Candidate recipes\n");
        foreach (var candidate in report.CandidateDescriptions) text.AppendLine($"- **{candidate.Key}:** {candidate.Value}\n");
        text.AppendLine("| Discovery candidate | Wins / trials | Selected |\n| --- | --- | --- |");
        foreach (var rank in report.Selection.Ranking) text.AppendLine($"| {rank.Party} | {rank.Wins}/{rank.Trials} | {report.Selection.Parties.Contains(rank.Party)} |");
        text.AppendLine("\n## Reserved-seed confirmation\n\nGained/lost wins are paired against candidate-00. Inspect the companion benchmark reports for durations, survival and descriptive clear intervals.\n\n| Floor | Candidate | Wins / trials | Draws | Control wins | Gained / lost |\n| --- | --- | --- | --- | --- | --- |");
        foreach (var cell in report.Confirmation) text.AppendLine($"| {cell.Floor} | {cell.Party} | {cell.Wins}/{cell.Trials} | {cell.Draws} | {cell.ControlWins} | {cell.GainedWins}/{cell.LostWins} |");
        text.AppendLine("\n## Role diagnostics\n\nMeans per original character/trial; deaths condition time on characters that died. These are contributions, not causal attribution. Detailed replay retains ability-level statistics and events.\n\n| Floor | Candidate | Role | Damage | Healing | Damage taken | Attention % | Stagger | First death s |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var role in report.RoleEvidence) text.AppendLine(FormattableString.Invariant($"| {role.Floor} | {role.Party} | {role.Role} | {role.Damage:F1} | {role.Healing:F1} | {role.DamageTaken:F1} | {role.AttentionPercent:F1} | {role.Stagger:F1} | {role.FirstDeathSeconds:F1} |"));
        return text.ToString();
    }
}
