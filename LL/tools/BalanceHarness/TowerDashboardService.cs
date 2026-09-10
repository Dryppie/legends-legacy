using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public sealed record DashboardRunRequest(string Catalog, int[] Floors, string[] Parties, int Samples, int Seed, string? Reference);
public sealed record DashboardReplayRequest(string Run, string Battle);
public sealed record DashboardCatalog(string Id, TowerBenchmarkDefinition Definition);
public sealed record DashboardRun(string Id, string Name, string Status, int Valid, int Planned, string Kind = "Benchmark");
public sealed record DashboardJob(string Id, string Kind, string Status, string? Run, int Completed, int Planned,
    string Message, string? ReplayFile = null);

/// <summary>One cancellable local operation at a time. Existing evidence is always read-only.</summary>
public sealed partial class TowerDashboardService(string apiRoot, string catalogsRoot, string runsRoot) : IAsyncDisposable
{
    private readonly string _runsRoot = Path.GetFullPath(runsRoot);
    private readonly object _gate = new();
    private DashboardJob? _job;
    private CancellationTokenSource? _cancellation;
    private Task _work = Task.CompletedTask;
    private bool _disposed;

    public DashboardJob? Job { get { lock (_gate) return _job; } }
    public object Floors() => new Services.LL.WorldTower.JsonWorldTowerDefinitionProvider(
        Path.Combine(apiRoot, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options).GetFloors()
        .Select(f => new { f.FloorNumber, f.Name, f.GuardianName, f.RequiredSlots }).ToArray();

    public IReadOnlyList<DashboardCatalog> Catalogs()
    {
        var catalogs = new List<DashboardCatalog>();
        foreach (var file in Directory.EnumerateFiles(catalogsRoot, "*.json").Order())
        {
            try
            {
                var definition = HarnessJson.Read<TowerBenchmarkDefinition>(file);
                if (definition.Profiles is null || definition.Parties is null || definition.Floors is null) continue;
                TowerBenchmark.Expand(definition, apiRoot, 1337, 1);
                catalogs.Add(new(Path.GetFileNameWithoutExtension(file), definition));
            }
            catch (Exception error) when (error is JsonException or InvalidDataException or ArgumentException) { }
        }
        return catalogs;
    }

    // Bounded discovery, without walking enormous battle/content archives or following directory links.
    private IReadOnlyDictionary<string, string> RunPaths()
    {
        var result = new Dictionary<string, string>();
        if (!Directory.Exists(_runsRoot)) return result;
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((_runsRoot, 0));
        var visited = 0;
        while (queue.Count > 0 && visited++ < 2000)
        {
            var (path, depth) = queue.Dequeue();
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
            if (IsLoadouts(path) || (File.Exists(Path.Combine(path, "benchmark-input.json")) && File.Exists(Path.Combine(path, "benchmark.json"))))
            {
                result.Add(RunId(path), path);
                continue;
            }
            if (depth >= 3 || File.Exists(Path.Combine(path, "tower-input.json")) || File.Exists(Path.Combine(path, "suite-input.json"))) continue;
            foreach (var child in Directory.EnumerateDirectories(path).OrderDescending().Take(2000))
                if (Path.GetFileName(child) is not ("content" or "source" or "executable" or "verification-executable" or "verified-source" or "replays" or "cells" or "scenarios" or "recipes" or "battles" or "searches" or "catalogs"))
                    queue.Enqueue((child, depth + 1));
        }
        return result;
    }

    private string RunId(string path) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
        Path.GetRelativePath(_runsRoot, path).Replace('\\', '/'))));

    public IReadOnlyList<DashboardRun> Runs() => RunPaths().Select(entry =>
    {
        try
        {
            if (IsLoadouts(entry.Value))
            {
                var report = HarnessJson.Read<PartySearchReport>(Path.Combine(entry.Value, "party-search.json"));
                return new DashboardRun(entry.Key, Path.GetRelativePath(_runsRoot, entry.Value), report.Status, report.ActualBattles, report.PlannedMaximum, "Loadouts");
            }
            var summary = HarnessJson.Read<TowerBenchmarkReport>(Path.Combine(entry.Value, "benchmark.json"));
            return new DashboardRun(entry.Key, Path.GetRelativePath(_runsRoot, entry.Value), summary.Status, summary.ValidBattles, summary.PlannedBattles);
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidDataException)
        { return new DashboardRun(entry.Key, Path.GetRelativePath(_runsRoot, entry.Value), "Unreadable", 0, 0); }
    }).ToArray();

    public string ResolveRun(string id) => RunPaths().TryGetValue(id, out var path)
        ? path : throw new InvalidDataException("Saved run was not found in the configured results folder.");

    public object Details(string id, CancellationToken token)
    {
        var path = ResolveRun(id);
        if (IsLoadouts(path)) return LoadoutDetails(path, token);
        var saved = TowerBenchmark.ReadSaved(path, token);
        TowerBenchmarkComparisonReport? comparison = null;
        var comparisonPath = Path.Combine(path, "comparison", "comparison.json");
        if (File.Exists(comparisonPath))
        {
            var stored = HarnessJson.Read<TowerBenchmarkComparisonReport>(comparisonPath);
            // Recompute from verified evidence, never trust a cached comparison as a verified result.
            var reference = RunPaths().Values.SingleOrDefault(p => Path.GetFullPath(p) == stored.ReferenceRun);
            if (reference is not null) comparison = TowerBenchmarkComparison.Compare(TowerBenchmark.ReadSaved(reference, token), saved);
        }
        return new
        {
            saved.Report, saved.Input.Definition, saved.Input.MasterSeed, Comparison = comparison,
            HasSavedComparison = File.Exists(comparisonPath),
            HasSearchReport = File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "search-report.json")),
            ReplayCompatible = HarnessJson.Hash(saved.Manifest.Execution) == HarnessJson.Hash(ExecutionIdentity.Current()),
            Battles = saved.Cells.SelectMany(c => c.Value.Scorecard.Trials.Select(t => new
            {
                Id = $"{c.Key}/{t.Id}", t.Seed, Outcome = t.Report.Battle.Summary.ContentOutcome.ToString(),
                t.Report.Battle.Summary.DurationSeconds, t.Report.GuardianHealthRemainingPercent
            })).ToArray()
        };
    }

    public DashboardJob Start(DashboardRunRequest request)
    {
        var catalog = Catalogs().SingleOrDefault(c => c.Id == request.Catalog)
            ?? throw new InvalidDataException("Choose an available profile catalog.");
        if (request.Floors is null || request.Parties is null || request.Floors.Length == 0 || request.Parties.Length == 0
            || request.Floors.Distinct().Count() != request.Floors.Length || request.Parties.Distinct().Count() != request.Parties.Length
            || request.Floors.Except(catalog.Definition.Floors).Any() || request.Parties.Except(catalog.Definition.Parties.Select(p => p.Id)).Any())
            throw new InvalidDataException("Choose floors and parties from the selected catalog.");
        var definition = catalog.Definition with
        {
            Floors = request.Floors, Parties = catalog.Definition.Parties.Where(p => request.Parties.Contains(p.Id)).ToArray(),
            SamplesPerCell = request.Samples
        };
        var scenarios = TowerBenchmark.Expand(definition, apiRoot, request.Seed);
        var reference = string.IsNullOrEmpty(request.Reference) ? null : ResolveRun(request.Reference);
        return Begin("Benchmark", scenarios.Sum(s => s.Seeds.Count), async (folder, token) =>
        {
            var recipe = Path.Combine(folder, "catalog.json");
            HarnessJson.WriteNew(recipe, definition);
            var output = Path.Combine(folder, "run");
            Change(j => j with { Run = RunId(output), Message = "Preparing profiles and capturing content…" });
            await TowerBenchmark.RunAsync(apiRoot, recipe, output, request.Seed, request.Samples, reference, token,
                message => Change(j => j with { Completed = j.Completed + 1, Message = message }));
        });
    }

    public DashboardJob Replay(DashboardReplayRequest request)
    {
        var path = ResolveRun(request.Run);
        if (string.IsNullOrWhiteSpace(request.Battle)) throw new InvalidDataException("Choose a saved battle.");
        return Begin("Replay", 1, async (folder, token) =>
        {
            var replay = IsLoadouts(path) ? await TowerLoadoutArchive.ReplayAsync(path, request.Battle, true, token)
                : await TowerBenchmark.ReplayAsync(path, request.Battle, true, token);
            var file = Path.Combine(folder, "replay.json");
            HarnessJson.WriteNew(file, replay);
            Change(j => j with { Completed = 1, Run = request.Run, ReplayFile = file, Message = "Verified replay matched preparation, combat and outcome." });
        });
    }

    public object SearchPlan()
    {
        var config = HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(catalogsRoot, TowerEssenceSearch.ConfigFile));
        var catalog = TowerEssenceSearch.Generate(config, HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogsRoot, config.BaseCatalog)));
        TowerBenchmark.Expand(catalog, apiRoot, config.DiscoverySeed);
        return new { Candidates = catalog.Parties.Count, Floors = catalog.Floors.Count, config.DiscoverySamples,
            config.ConfirmationSamples, MaximumBattles = catalog.Floors.Count * (catalog.Parties.Count * config.DiscoverySamples + Math.Min(6, catalog.Parties.Count) * config.ConfirmationSamples) };
    }

    public DashboardJob Search()
    {
        var config = HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(catalogsRoot, TowerEssenceSearch.ConfigFile));
        var catalog = TowerEssenceSearch.Generate(config, HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogsRoot, config.BaseCatalog)));
        TowerBenchmark.Expand(catalog, apiRoot, config.DiscoverySeed);
        var maximum = catalog.Floors.Count * (catalog.Parties.Count * config.DiscoverySamples + Math.Min(6, catalog.Parties.Count) * config.ConfirmationSamples);
        return Begin("Essence search", maximum, async (folder, token) =>
        {
            var output = Path.Combine(folder, "search");
            await TowerEssenceSearch.RunAsync(apiRoot, catalogsRoot, output, config, token,
                message => Change(j => j with { Completed = j.Completed + 1, Message = message }));
            Change(j => j with { Run = RunId(Path.Combine(output, "confirmation")), Planned = j.Completed,
                Message = "Search and independent confirmation saved. Review the search report before interpreting candidates as strong builds." });
        });
    }

    public TowerSearchReport SearchReport(string run, CancellationToken token) =>
        TowerEssenceSearch.ReadReport(Path.GetDirectoryName(ResolveRun(run))!, token);

    private DashboardJob Begin(string kind, int planned, Func<string, CancellationToken, Task> action)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_work.IsCompleted) throw new InvalidOperationException("An operation is already running. Wait for it or cancel it first.");
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            var id = $"dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
            var folder = Path.Combine(_runsRoot, id);
            Directory.CreateDirectory(folder);
            _job = new(id, kind, "Running", null, 0, planned, "Starting…");
            _work = Task.Run(async () =>
            {
                try
                {
                    await action(folder, token);
                    Change(j => j with { Status = "Complete", Message = kind is "Replay" or "Essence search" or "Loadouts" ? j.Message : "Benchmark saved. Results are descriptive." });
                }
                catch (OperationCanceledException) { Change(j => j with { Status = "Cancelled", Message = "Cancelled. Any completed trials remain saved." }); }
                catch (Exception error) { Change(j => j with { Status = "Failed", Message = error.Message }); }
                finally { HarnessJson.WriteNew(Path.Combine(folder, "job.json"), Job); }
            });
            return _job;
        }
    }

    private void Change(Func<DashboardJob, DashboardJob> update) { lock (_gate) _job = update(_job!); }
    public DashboardJob Cancel(string id)
    {
        lock (_gate)
        {
            if (_job?.Id != id) throw new InvalidDataException("Operation not found.");
            if (_job.Status == "Running")
            {
                _job = _job with { Status = "Cancelling", Message = "Cancellation requested; preserving completed trials…" };
                _cancellation!.Cancel();
            }
            return _job;
        }
    }

    public string ReplayFile(string id)
    {
        lock (_gate) return _job is { Status: "Complete", ReplayFile: not null } && _job.Id == id
            ? _job.ReplayFile : throw new InvalidDataException("Verified replay is not available for this operation.");
    }

    public async ValueTask DisposeAsync()
    {
        Task work;
        lock (_gate) { _disposed = true; _cancellation?.Cancel(); work = _work; }
        await work;
        _cancellation?.Dispose();
    }
}
