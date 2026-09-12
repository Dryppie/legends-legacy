using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BalanceHarness;
using Microsoft.AspNetCore.Builder;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDashboardStudyTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public async Task Preview_is_reference_independent_declares_every_character_and_rejects_invalid_or_stale_start()
    {
        await using var host = await Host.Start();
        var request = new DashboardStudyRequest(1, 4, 479187);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/team-plan", request)).StatusCode);
        await host.Authorize();
        var original = await host.Preview(request);
        Assert.Equal(5, original.Definition.RequiredPartySize); Assert.Equal(2, original.Definition.References.Count);
        Assert.All(original.Definition.Contexts[0].CharacterTemplates, t => { Assert.Empty(t.Build.EssenceIds); Assert.Null(t.Build.IdentityEssenceIds); });
        Assert.Equal(14624, original.Cost.Total);
        var reference = await host.Preview(request with { IncludeUserReference = true });
        Assert.Equal(15624, reference.Cost.Total); Assert.Equal(3, reference.Definition.References.Count);
        Assert.Equal(HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(original.Definition)), HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(reference.Definition)));
        Assert.Equal(HarnessJson.Hash(original.Definition.Stages), HarnessJson.Hash(reference.Definition.Stages));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/teams", new DashboardStudyStart(original.PlanHash))).StatusCode);
        Assert.Null(host.Service.Job);
        foreach (var invalid in new[] { request with { Floor = 16 }, request with { Slots = 3 }, request with { Floor = 11 },
            request with { Floor = 5, Slots = 5, IncludeUserReference = true }, request with { AllowedEssences = ["missing"] }, request with { OwnedCopies = new Dictionary<string, int>() } })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/team-plan", invalid)).StatusCode);
        var late = await host.Preview(new(11, 7, 479187));
        Assert.True(late.Definition.RequiredPartySize > 5);
        Assert.Equal(Enumerable.Range(1, late.Definition.RequiredPartySize), late.Definition.Contexts[0].CharacterTemplates.Select(t => t.PartySlot));
        Assert.Equal(late.Definition.RequiredPartySize, late.Definition.Contexts[0].CharacterTemplates.Select(t => t.Build.Id).Distinct().Count());
        var diagnostic = await host.Preview(new(11, 4, 479187, "diagnostic")); Assert.Equal("diagnostic", diagnostic.Definition.BudgetPurpose);
        Assert.Empty(host.Service.Runs());
    }

    [Fact]
    public async Task Complete_imported_study_runs_exact_preview_reports_and_exports_replays_then_excludes_its_full_schedules()
    {
        await using var host = await Host.Start(); await host.Authorize();
        var d = BalanceHarnessTowerBossStudyTests.Small() with { References = [BalanceHarnessTowerBossDiscoveryContractTests.Reference()] };
        var response = await host.Client.PostAsJsonAsync("/api/team-plan/import", d, HarnessJson.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = (await response.Content.ReadFromJsonAsync<DashboardStudyPreview>(HarnessJson.Options))!;
        response = await host.Client.PostAsJsonAsync("/api/teams", new DashboardStudyStart(plan.PlanHash));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/teams", new DashboardStudyStart(plan.PlanHash))).StatusCode);
        var job = await Wait(host.Service); Assert.Equal("Complete", job.Status);
        var run = Assert.Single(host.Service.Runs()); Assert.Equal("Independent teams", run.Kind);
        var path = host.Service.ResolveRun(run.Id);
        var saved = HarnessJson.Read<BossStudyReport>(Path.Combine(path, "study.json"));
        Assert.Equal(HarnessJson.Hash(plan.Definition), HarnessJson.Hash(TowerBossDiscovery.Read(Path.Combine(path, "definition.json"))));
        Assert.Equal(saved.Accounting.Completed.Values.Sum(), job.Completed);
        Assert.Contains("Balance:", job.Message);
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}");
        Assert.True(detail.GetProperty("isStudy").GetBoolean()); Assert.Equal("Reconstructed", detail.GetProperty("integrity").GetString());
        Assert.Equal(saved.Conclusion!.OverallAssessment.ToString(), detail.GetProperty("study").GetProperty("conclusion").GetProperty("overallAssessment").GetString());
        var cached = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}"); Assert.Equal(HarnessJson.Hash(detail), HarnessJson.Hash(cached));
        Assert.Equal(HarnessJson.Hash(saved), HarnessJson.Hash(await host.Client.GetFromJsonAsync<BossStudyReport>($"/api/runs/{run.Id}/download/json", HarnessJson.Options)));
        var cell = saved.Confirmation!.Definition.Cells[0];
        Assert.Equal(HarnessJson.Hash(cell.Scenario), HarnessJson.Hash(await host.Client.GetFromJsonAsync<TowerScenario>($"/api/runs/{run.Id}/team-recipe/{cell.Id}", HarnessJson.Options)));
        var battle = detail.GetProperty("battles")[0].GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/{battle}")).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/replays", new DashboardReplayRequest(run.Id, battle))).StatusCode);
        Assert.Equal("Complete", (await Wait(host.Service)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/team-plan/import", d, HarnessJson.Options)).StatusCode);
        var next = await host.Preview(new(1, 4, 983412));
        var schedule = d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics)).ToArray();
        Assert.Empty(schedule.Except(next.Definition.ExcludedCombatSeeds));
        Assert.Empty(schedule.Intersect(next.Definition.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation))));
        File.AppendAllText(Path.Combine(path, "study.md"), "changed");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
    }

    [Fact]
    public async Task Cancellation_preserves_partial_status_and_never_shows_verified_acceptance()
    {
        await using var host = await Host.Start(); await host.Authorize();
        var plan = await host.Preview(new(1, 4, 631826));
        var response = await host.Client.PostAsJsonAsync("/api/teams", new DashboardStudyStart(plan.PlanHash));
        var started = (await response.Content.ReadFromJsonAsync<DashboardJob>(HarnessJson.Options))!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (host.Service.Job!.Completed == 0) await Task.Delay(10, timeout.Token);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsJsonAsync($"/api/jobs/{started.Id}/cancel", new { })).StatusCode);
        var job = await Wait(host.Service); Assert.Equal("Cancelled", job.Status);
        var run = Assert.Single(host.Service.Runs()); Assert.Equal("Cancelled", run.Status);
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}");
        Assert.True(details.GetProperty("isPartialEvidence").GetBoolean()); Assert.Equal("File integrity only", details.GetProperty("integrity").GetString());
        Assert.Equal(JsonValueKind.Null, details.GetProperty("study").GetProperty("confirmation").ValueKind);
        Assert.NotEmpty(details.GetProperty("battles").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/runs/{run.Id}/download/md")).StatusCode);
    }

    [Fact]
    public async Task Local_ledgers_are_read_independently_of_references_and_imports_are_strict()
    {
        await using var host = await Host.Start(); await host.Authorize();
        var ledgerDirectory = Path.Combine(host.Directory, "prior-tuning"); System.IO.Directory.CreateDirectory(ledgerDirectory);
        HarnessJson.WriteNew(Path.Combine(ledgerDirectory, "seed-ledger.json"), new { Coarse = new[] { 345613, -41221 }, Confirmation = new[] { 775611 } });
        var older = Path.Combine(host.Directory, "older-tower"); System.IO.Directory.CreateDirectory(older);
        HarnessJson.WriteNew(Path.Combine(older, "definition.json"), new[] { new { Unrelated = true } });
        HarnessJson.WriteNew(Path.Combine(older, "tower-input.json"), new[] { new { Scenario = new { Seeds = new[] { 915442, -88127 } }, Rules = new { RandomSeed = 614418 } } });
        var request = new DashboardStudyRequest(1, 4, 812913, ExcludedSeeds: [713452]);
        var plan = await host.Preview(request);
        Assert.Empty(new[] { 345613, -41221, 775611, 713452, 915442, -88127, 614418 }.Except(plan.Definition.ExcludedCombatSeeds));
        Assert.Contains(plan.SeedSources, s => s.Contains("prior-tuning"));
        var withReference = await host.Preview(request with { IncludeUserReference = true });
        Assert.Equal(HarnessJson.Hash(plan.Definition.Stages), HarnessJson.Hash(withReference.Definition.Stages));
        var malformed = JsonSerializer.SerializeToNode(plan.Definition, HarnessJson.Options)!; malformed["unexpected"] = true;
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/team-plan/import", malformed)).StatusCode);
        var replaced = plan.Definition with { ExecutionHash = new string('f', 64) };
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/team-plan/import", replaced, HarnessJson.Options)).StatusCode);
        Assert.Null(host.Service.Job);
    }

    [Fact]
    public async Task Large_history_survives_preview_http_import_and_cumulative_local_union_without_raising_combat_caps()
    {
        await using var host = await Host.Start(); await host.Authorize();
        var historical = Enumerable.Range(-900000, 300000).ToArray();
        var folder = Path.Combine(host.Directory, "larger-history"); System.IO.Directory.CreateDirectory(folder);
        HarnessJson.WriteNew(Path.Combine(folder, "seed-ledger.json"), new { Prior = historical });
        var plan = await host.Preview(new(1, 4, 744031));
        Assert.Empty(historical.Except(plan.Definition.ExcludedCombatSeeds));
        Assert.True(plan.Definition.ExcludedCombatSeeds.Count > 100000);
        var json = JsonSerializer.Serialize(plan.Definition, HarnessJson.Options);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) > 2 * 1024 * 1024);
        var response = await host.Client.PostAsJsonAsync("/api/team-plan/import", plan.Definition, HarnessJson.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var imported = (await response.Content.ReadFromJsonAsync<DashboardStudyPreview>(HarnessJson.Options))!;
        Assert.Equal(plan.PlanHash, imported.PlanHash);
        Assert.Equal(100000, imported.Definition.MaximumBattles);
        var overlap = plan.Definition with { Stages = plan.Definition.Stages with { Schedules = plan.Definition.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = [historical[0]] }) } };
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/team-plan/import", overlap, HarnessJson.Options)).StatusCode);
        Assert.Null(host.Service.Job);
    }

    [Fact]
    public async Task Imported_improvement_is_explicit_in_preview_run_and_saved_result_and_retains_finalists()
    {
        await using var host = await Host.Start(); await host.Authorize();
        var d = BalanceHarnessTowerBossImprovementTests.WithStart(BalanceHarnessTowerBossStudyTests.Small());
        var response = await host.Client.PostAsJsonAsync("/api/team-plan/import", d, HarnessJson.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = (await response.Content.ReadFromJsonAsync<DashboardStudyPreview>(HarnessJson.Options))!;
        Assert.Contains("reference ancestry", plan.Note);
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/teams", new DashboardStudyStart(plan.PlanHash))).StatusCode);
        Assert.Equal("Complete", (await Wait(host.Service)).Status);
        var run = Assert.Single(host.Service.Runs()); Assert.Equal("Retained-build improvement", run.Kind);
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}");
        Assert.Equal("improve-supplied", detail.GetProperty("studyDefinition").GetProperty("mode").GetString());
        Assert.Contains("Reference-derived", detail.GetProperty("note").GetString());
        Assert.Equal("Reconstructed", detail.GetProperty("integrity").GetString());
        Assert.Single(TowerRetainedBuilds.Read(Path.Combine(host.Directory, TowerRetainedBuilds.LocalFile)).Studies);
    }

    private static async Task<DashboardJob> Wait(TowerDashboardService service)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (service.Job?.Status is "Running" or "Cancelling") await Task.Delay(20, timeout.Token);
        await Task.Delay(50, timeout.Token); return service.Job!;
    }
    private sealed class Host : IAsyncDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "tower-dashboard-study-" + Guid.NewGuid().ToString("N"));
        public TowerDashboardService Service { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;
        private WebApplication app = null!;
        public static async Task<Host> Start()
        {
            var host = new Host(); host.Service = new(Root, Catalogs, host.Directory); host.app = TowerDashboardServer.Create(host.Service, 0);
            await host.app.StartAsync(); host.Client = new() { BaseAddress = new Uri(host.app.Urls.Single()), Timeout = TimeSpan.FromSeconds(90) }; return host;
        }
        public async Task Authorize()
        {
            var session = await Client.GetFromJsonAsync<JsonElement>("/api/session"); Client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        }
        public async Task<DashboardStudyPreview> Preview(DashboardStudyRequest request)
        {
            var response = await Client.PostAsJsonAsync("/api/team-plan", request, HarnessJson.Options);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<DashboardStudyPreview>(HarnessJson.Options))!;
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose(); await app.StopAsync(); await Service.DisposeAsync(); await app.DisposeAsync();
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
        }
    }
}
