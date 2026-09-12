using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BalanceHarness;
using Microsoft.AspNetCore.Builder;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDashboardTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public async Task Http_dashboard_runs_compares_replays_and_downloads_verified_evidence()
    {
        await using var host = await Host.Start();
        var html = await host.Client.GetStringAsync("/");
        Assert.Contains("Run Tower Benchmarks", html);
        Assert.Contains("/dashboard.js", html);
        Assert.Contains("function renderEvents", await host.Client.GetStringAsync("/dashboard.js"));
        Assert.Equal(JsonValueKind.Null, (await host.Client.GetFromJsonAsync<JsonElement>("/api/job")).ValueKind);
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.NotEmpty(session.GetProperty("catalogs").EnumerateArray());
        var floors = session.GetProperty("floors").EnumerateArray().Select(f => f.GetProperty("floorNumber").GetInt32()).ToArray();
        var catalog = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-benchmark");
        var progression = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-progression");
        var factors = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-factors");
        var slots = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-essence-slots");
        var anchors = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-anchors");
        var entryRanks = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-entry-ranks");
        var uncommonEntry = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-entry-uncommon");
        var curve = session.GetProperty("catalogs").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "tower-curve");
        Assert.Equal(2, curve.GetProperty("definition").GetProperty("schemaVersion").GetInt32());
        Assert.All(curve.GetProperty("definition").GetProperty("parties").EnumerateArray(),
            p => Assert.Equal(15, p.GetProperty("floorCellProfiles").EnumerateObject().Count()));
        Assert.Equal(new[] { "standard-rank-1", "standard-rank-2", "fine-rank-1", "fine-rank-2", "floor-10-6" }, uncommonEntry.GetProperty("definition").GetProperty("parties").EnumerateArray().Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(new[] { "rank-0", "rank-1", "rank-2", "rank-3", "floor-10-6" }, entryRanks.GetProperty("definition").GetProperty("parties").EnumerateArray().Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(new[] { "entry-4", "floor-10-6" }, anchors.GetProperty("definition").GetProperty("parties").EnumerateArray().Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(Enumerable.Range(4, 7).Select(n => $"essences-{n}"), slots.GetProperty("definition").GetProperty("parties").EnumerateArray().Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(8, factors.GetProperty("definition").GetProperty("parties").GetArrayLength());
        Assert.Equal(new[] { "early", "mid", "late" }, progression.GetProperty("definition").GetProperty("parties").EnumerateArray().Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(floors, catalog.GetProperty("definition").GetProperty("floors").EnumerateArray().Select(f => f.GetInt32()));
        Assert.Contains(session.GetProperty("floors").EnumerateArray(), f => f.GetProperty("requiredSlots").GetInt32() == 15);
        host.Client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        var request = new DashboardRunRequest("tower-benchmark", floors, ["mixed"], 1, 1337, null);
        var response = await host.Client.PostAsJsonAsync("/api/runs", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var first = await Wait(host.Service);
        Assert.Equal("Complete", first.Status);
        Assert.Equal(floors.Length, first.Completed);
        var original = host.Service.ResolveRun(first.Run!);
        var hash = HarnessJson.FileHash(Path.Combine(original, "benchmark.json"));
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{first.Run}");
        Assert.Equal(floors.Length, details.GetProperty("report").GetProperty("validBattles").GetInt32());
        Assert.True(details.GetProperty("replayCompatible").GetBoolean());
        var download = await host.Client.GetAsync($"/api/runs/{first.Run}/download/md");
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Contains("Tower benchmarks", await download.Content.ReadAsStringAsync());

        response = await host.Client.PostAsJsonAsync("/api/runs", request with { Reference = first.Run });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var second = await Wait(host.Service);
        Assert.Equal("Complete", second.Status);
        details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{second.Run}");
        Assert.Equal("Compared", details.GetProperty("comparison").GetProperty("status").GetString());
        Assert.All(details.GetProperty("comparison").GetProperty("cells").EnumerateArray(), c => Assert.Equal(0, c.GetProperty("gameplayChanges").GetInt32()));
        Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(original, "benchmark.json")));

        response = await host.Client.PostAsJsonAsync("/api/replays", new DashboardReplayRequest(second.Run!, "floor-15.mixed/tower.0001"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var replay = await Wait(host.Service);
        Assert.Equal("Complete", replay.Status);
        var result = await host.Client.GetFromJsonAsync<TowerBattleReport>($"/api/jobs/{replay.Id}/replay", HarnessJson.Options);
        Assert.NotEmpty(result!.Battle.EventLog!);
        Assert.Equal(15, result.Battle.Summary.Friendly.Count);
        Assert.Equal(2, host.Service.Runs().Count);
    }

    [Fact]
    public async Task Local_http_boundary_rejects_cross_origin_missing_token_and_arbitrary_paths()
    {
        await using var host = await Host.Start();
        var request = new DashboardRunRequest("tower-benchmark", [1], ["mixed"], 1, 1337, null);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/runs", request)).StatusCode);
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/session");
        host.Client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        host.Client.DefaultRequestHeaders.Add("Origin", "https://unrelated.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/runs", request)).StatusCode);
        host.Client.DefaultRequestHeaders.Remove("Origin");
        host.Client.DefaultRequestHeaders.Host = "unrelated.example:" + host.Client.BaseAddress!.Port;
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/session")).StatusCode);
        host.Client.DefaultRequestHeaders.Host = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/runs", request with { Catalog = "../elsewhere" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/runs", request with { Floors = [999] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/runs", request with { Samples = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/runs", request with { Reference = "C:/arbitrary" })).StatusCode);
        Assert.Empty(host.Service.Runs());
        Assert.Null(host.Service.Job);
    }

    [Fact]
    public async Task Single_worker_cancels_preserves_partial_trials_and_accepts_the_next_run()
    {
        await using var host = await Host.Start();
        var request = new DashboardRunRequest("tower-benchmark", [1, 3, 5], ["mixed", "starter"], 1000, 1337, null);
        var job = host.Service.Start(request);
        Assert.Throws<InvalidOperationException>(() => host.Service.Start(request));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (host.Service.Job!.Completed == 0) await Task.Delay(10, timeout.Token);
        host.Service.Cancel(job.Id);
        var cancelled = await Wait(host.Service);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.True(cancelled.Completed > 0);
        var saved = TowerBenchmark.ReadSaved(host.Service.ResolveRun(cancelled.Run!));
        Assert.Equal("Cancelled", saved.Report.Status);
        Assert.Equal(cancelled.Completed, saved.Report.ValidBattles);
        host.Service.Start(request with { Floors = [1], Parties = ["starter"], Samples = 1 });
        Assert.Equal("Complete", (await Wait(host.Service)).Status);
    }

    [Fact]
    public async Task Corrupt_evidence_is_visible_and_never_replayed_as_verified()
    {
        await using var host = await Host.Start();
        host.Service.Start(new("tower-benchmark", [1], ["starter"], 1, 1337, null));
        var done = await Wait(host.Service);
        var path = host.Service.ResolveRun(done.Run!);
        File.AppendAllText(Path.Combine(path, "cells/floor-1.starter/battles/tower.0001.json"), " ");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{done.Run}")).StatusCode);
        host.Service.Replay(new(done.Run!, "floor-1.starter/tower.0001"));
        Assert.Equal("Failed", (await Wait(host.Service)).Status);
        Assert.Throws<InvalidDataException>(() => host.Service.ReplayFile(host.Service.Job!.Id));
    }

    [Fact]
    public async Task Search_button_runs_a_bounded_plan_and_serves_verified_rankings()
    {
        await using var host = await Host.Start(smallSearch: true);
        var plan = await host.Client.GetFromJsonAsync<JsonElement>("/api/search-plan");
        Assert.Equal(16, plan.GetProperty("candidates").GetInt32());
        Assert.Equal(1, plan.GetProperty("floors").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/search", new { })).StatusCode);
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/session");
        host.Client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/search", new { })).StatusCode);
        var done = await Wait(host.Service);
        Assert.Equal("Complete", done.Status); Assert.Equal(done.Completed, done.Planned);
        Assert.Contains(host.Service.Runs(), r => r.Id == done.Run);
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{done.Run}");
        Assert.True(details.GetProperty("hasSearchReport").GetBoolean());
        var report = await host.Client.GetFromJsonAsync<TowerSearchReport>($"/api/runs/{done.Run}/search/json", HarnessJson.Options);
        Assert.Equal("Complete", report!.Status); Assert.Contains(TowerEssenceSearch.Control, report.Selection.Parties);
        Assert.Contains("Reserved-seed confirmation", await host.Client.GetStringAsync($"/api/runs/{done.Run}/search/md"));
        var folder = Path.GetDirectoryName(host.Service.ResolveRun(done.Run!))!;
        File.WriteAllText(Path.Combine(folder, "selection.json"), "{}");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{done.Run}/search/json")).StatusCode);
    }

    private static async Task<DashboardJob> Wait(TowerDashboardService service)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        while (service.Job?.Status is "Running" or "Cancelling") await Task.Delay(25, timeout.Token);
        // The final job record is persisted just after status changes.
        await Task.Delay(50, timeout.Token);
        return service.Job!;
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "tower-dashboard-tests-" + Guid.NewGuid().ToString("N"));
        private WebApplication _app = null!;
        public TowerDashboardService Service { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;
        public static async Task<Host> Start(bool smallSearch = false)
        {
            var host = new Host();
            var catalogs = Catalogs;
            if (smallSearch)
            {
                catalogs = Path.Combine(host._root, "catalogs"); Directory.CreateDirectory(catalogs);
                var config = HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(Catalogs, TowerEssenceSearch.ConfigFile));
                HarnessJson.WriteNew(Path.Combine(catalogs, TowerEssenceSearch.ConfigFile), config with { DiscoverySamples = 1, ConfirmationSamples = 1 });
                HarnessJson.WriteNew(Path.Combine(catalogs, config.BaseCatalog),
                    HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Catalogs, config.BaseCatalog)) with { Floors = [1] });
            }
            host.Service = new(Root, catalogs, host._root);
            host._app = TowerDashboardServer.Create(host.Service, 0);
            await host._app.StartAsync();
            host.Client = new() { BaseAddress = new Uri(host._app.Urls.Single()), Timeout = TimeSpan.FromSeconds(45) };
            return host;
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await Service.DisposeAsync();
            await _app.DisposeAsync();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
    }
}
