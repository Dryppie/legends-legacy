using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BalanceHarness;
using Microsoft.AspNetCore.Builder;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDashboardBossTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public async Task Boss_preview_declares_target_gear_intent_full_transfer_cost_and_rejects_invalid_requests()
    {
        await using var host = await Host.Start();
        Assert.Contains("Find boss strategies", await host.Client.GetStringAsync("/"));
        var session = await host.Client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.Equal(Enumerable.Range(4, 7), session.GetProperty("bossBudgets").EnumerateArray().Select(b => b.GetProperty("essenceSlots").GetInt32()));
        Assert.Equal(15, session.GetProperty("floors").GetArrayLength());
        var request = new DashboardBossRequest(3, 6, "coverage", 763121, "add-clearing");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/boss-plan", request)).StatusCode);
        await host.Authorize();
        var response = await host.Client.PostAsJsonAsync("/api/boss-plan", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var definition = plan.GetProperty("definition").Deserialize<TowerBossSearchDefinition>(HarnessJson.Options)!;
        Assert.Equal([3], definition.Objective.TargetFloorWeights.Keys);
        Assert.Equal("add-clearing", definition.Objective.StrategyIntent);
        Assert.Equal(TowerPartyProgression.Budget(6) with { PriorityFloor = 3 }, definition.Budget);
        Assert.Equal(Enumerable.Range(1, 15), definition.ContextCounts.Keys.Order());
        Assert.Equal(TowerBossSearch.Validate(definition), plan.GetProperty("maximumBattles").GetInt32());
        var retainedResponse = await host.Client.PostAsJsonAsync("/api/boss-plan", new DashboardBossRequest(7, 5, "coverage", 763125));
        Assert.Equal(HttpStatusCode.OK, retainedResponse.StatusCode);
        var retainedPlan = await retainedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var retained = retainedPlan.GetProperty("definition").Deserialize<TowerBossSearchDefinition>(HarnessJson.Options)!;
        Assert.Equal(2, retained.SchemaVersion);
        Assert.Equal(42, retained.Controls.Count);
        Assert.NotNull(retained.Refinement!.ReferenceSetId);
        Assert.Contains("Historical", retained.Refinement.ReferenceEvidenceStatus!);
        Assert.Contains(retained.Controls, p => p.Id == retained.Refinement.AnchorId);
        foreach (var invalid in new[] { request with { Floor = 16 }, request with { Slots = 3 },
                     request with { Intent = "guaranteed-win" }, request with { Effort = "unbounded" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/bosses", invalid)).StatusCode);
        Assert.Null(host.Service.Job);
        Assert.Empty(host.Service.Runs());
    }

    [Fact]
    public async Task Historical_reference_library_preserves_separate_samples_transfer_and_exact_recipe_without_combat()
    {
        await using var host = await Host.Start();
        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-references/7/5");
        Assert.True(list.GetProperty("available").GetBoolean());
        Assert.Equal(42, list.GetProperty("recipes").GetArrayLength());
        var anchor = list.GetProperty("recipes").EnumerateArray().Single(row => row.GetProperty("isAnchor").GetBoolean());
        Assert.Equal(40, anchor.GetProperty("wins").GetInt32());
        Assert.Equal(40, anchor.GetProperty("samples").GetInt32());
        Assert.False(anchor.TryGetProperty("confirmation", out _));

        const string earlierAnchor = "e568dfaabbb86d4f3e8e0156d7ce16cdbf590b366a63fc0f1ee200e0de81de68";
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/boss-references/7/5/{earlierAnchor}");
        var evidence = details.GetProperty("evidence");
        var current = evidence.GetProperty("confirmation").GetProperty("cells");
        Assert.Equal(30, current.GetArrayLength());
        Assert.Equal(40, current[0].GetProperty("clears").GetArrayLength());
        var earlier = evidence.GetProperty("priorObservations")[0].GetProperty("evidence");
        Assert.Equal(20, earlier.GetProperty("confirmation").GetProperty("cells")[0].GetProperty("clears").GetArrayLength());
        Assert.Equal(Enumerable.Range(1, 15), current.EnumerateArray().Select(cell => cell.GetProperty("floor").GetInt32()).Distinct().Order());
        var response = await host.Client.GetAsync($"/api/boss-references/7/5/{earlierAnchor}/recipe");
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        var recipe = await response.Content.ReadFromJsonAsync<TowerScenario>(HarnessJson.Options);
        Assert.Equal(evidence.GetProperty("targetRecipeHash").GetString(), HarnessJson.Hash(recipe));
        Assert.Equal(40, recipe!.Seeds.Count);
        Assert.Equal(5, recipe.Party.Count);

        var nhalia = await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-references/13/7");
        Assert.Equal(53, nhalia.GetProperty("recipes").GetArrayLength());
        Assert.Equal(1, nhalia.GetProperty("recipes").EnumerateArray().Single(row => row.GetProperty("isAnchor").GetBoolean()).GetProperty("wins").GetInt32());
        Assert.Contains(nhalia.GetProperty("recipes").EnumerateArray(), row => !row.GetProperty("isAnchor").GetBoolean() && row.GetProperty("wins").GetInt32() == 6);
        var absent = await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-references/3/4");
        Assert.False(absent.GetProperty("available").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-references/16/5")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-references/7/5/unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-references/7/5/unknown/recipe")).StatusCode);
        Assert.Null(host.Service.Job);
        Assert.Empty(host.Service.Runs());
    }

    [Fact]
    public async Task Fixed_validation_is_target_only_and_exports_its_own_hundred_seed_recipe_without_changing_historical_references()
    {
        await using var host = await Host.Start();
        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-validations/13/7");
        Assert.True(list.GetProperty("available").GetBoolean());
        var recipes = list.GetProperty("recipes").EnumerateArray().ToArray();
        Assert.Equal(new[] { 0, 5, 8 }, recipes.Select(r => r.GetProperty("wins").GetInt32()));
        Assert.All(recipes, row => Assert.Equal(100, row.GetProperty("samples").GetInt32()));
        foreach (var row in recipes)
        {
            var id = row.GetProperty("id").GetString();
            var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/boss-validations/13/7/{id}");
            Assert.Equal(13, detail.GetProperty("floor").GetInt32());
            var evidence = detail.GetProperty("evidence");
            Assert.False(evidence.TryGetProperty("confirmation", out _));
            Assert.Equal(100, evidence.GetProperty("observations").GetArrayLength());
            Assert.Equal(100, evidence.GetProperty("observations").EnumerateArray().Select(o => o.GetProperty("seed").GetInt32()).Distinct().Count());
            var export = await host.Client.GetAsync($"/api/boss-validations/13/7/{id}/recipe");
            Assert.Equal("attachment", export.Content.Headers.ContentDisposition!.DispositionType);
            var recipe = await export.Content.ReadFromJsonAsync<TowerScenario>(HarnessJson.Options);
            Assert.Equal(evidence.GetProperty("targetRecipeHash").GetString(), HarnessJson.Hash(recipe));
            Assert.Equal(100, recipe!.Seeds.Count);
            Assert.Equal(10, recipe.Party.Count);
            Assert.All(recipe.Party, member => Assert.Equal(7, member.Build.EssenceIds.Count));
        }
        var references = await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-references/13/7");
        Assert.Equal(53, references.GetProperty("recipes").GetArrayLength());
        Assert.Equal(1, references.GetProperty("recipes").EnumerateArray().Single(r => r.GetProperty("isAnchor").GetBoolean()).GetProperty("wins").GetInt32());
        await host.Authorize();
        var planResponse = await host.Client.PostAsJsonAsync("/api/boss-plan", new DashboardBossRequest(13, 7, "coverage", 821599));
        Assert.Equal(HttpStatusCode.OK, planResponse.StatusCode);
        Assert.True((await planResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("validation").GetProperty("available").GetBoolean());
        foreach (var path in new[] { "11/5", "15/6", "13/8" })
            Assert.False((await host.Client.GetFromJsonAsync<JsonElement>("/api/boss-validations/" + path)).GetProperty("available").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-validations/16/7")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-validations/13/7/unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/boss-validations/13/7/unknown/recipe")).StatusCode);
        Assert.Null(host.Service.Job);
        Assert.Empty(host.Service.Runs());
    }

    [Fact]
    public async Task Boss_results_export_exact_recipes_and_replay_after_all_floor_confirmation()
    {
        await using var host = await Host.Start();
        var definition = TowerBossSearch.Definition(Root, Catalogs, 3, 4, 763122, "any", "coverage");
        definition = definition with { GenerationSeeds = [definition.GenerationSeeds[0]], CandidatesPerArm = definition.Controls.Count + 1,
            DiscoverySamples = 1, ConfirmationSamples = 1, DiagnosticSamples = 1,
            Finalists = definition.Controls.Count + definition.Methods.Count + 5 };
        var output = Path.Combine(host.Directory, "confirmed");
        var report = await TowerBossSearch.RunAsync(Root, Catalogs, output, definition);
        Assert.Equal("Complete", report.Status);
        var run = Assert.Single(host.Service.Runs());
        Assert.Equal("Boss loadouts", run.Kind);
        Assert.Equal(definition.ValidationReferenceHash,
            HarnessJson.FileHash(Path.Combine(output, "catalogs", TowerBossValidationReferences.FileName)));
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}");
        Assert.True(details.GetProperty("isBossSearch").GetBoolean());
        Assert.True(details.GetProperty("replayCompatible").GetBoolean());
        Assert.Equal(Enumerable.Range(1, 15), details.GetProperty("report").GetProperty("cells").EnumerateArray()
            .Select(c => c.GetProperty("floor").GetInt32()).Distinct().Order());
        Assert.Equal(report.Selection.Count, details.GetProperty("loadouts").GetArrayLength());
        var evidence = await host.Client.GetFromJsonAsync<TowerBossSearchReport>($"/api/runs/{run.Id}/download/json", HarnessJson.Options);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(evidence));
        Assert.Equal("attachment", (await host.Client.GetAsync($"/api/runs/{run.Id}/download/md")).Content.Headers.ContentDisposition!.DispositionType);
        var battle = details.GetProperty("battles")[0].GetProperty("id").GetString()!;
        var trial = TowerLoadoutArchive.Verify(output).Single(t => t.Id == battle);
        var recipeResponse = await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/{battle}");
        Assert.Equal(HttpStatusCode.OK, recipeResponse.StatusCode);
        var recipe = await recipeResponse.Content.ReadFromJsonAsync<TowerScenario>(HarnessJson.Options);
        Assert.Equal(trial.Recipe, HarnessJson.Hash(recipe));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/unknown")).StatusCode);
        await host.Authorize();
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/replays", new DashboardReplayRequest(run.Id, battle))).StatusCode);
        var replayJob = await Wait(host.Service);
        Assert.Equal("Complete", replayJob.Status);
        var replay = await host.Client.GetFromJsonAsync<TowerBattleReport>($"/api/jobs/{replayJob.Id}/replay", HarnessJson.Options);
        Assert.NotEmpty(replay!.Battle.EventLog!);
        var next = host.Service.BossPlan(new(3, 4, "coverage", 763123));
        Assert.Empty(TowerBossSearch.CombatSeeds(next).Intersect(TowerBossSearch.CombatSeeds(definition)));
        Assert.All(TowerBossSearch.CombatSeeds(definition), seed => Assert.Contains(seed, next.ExcludedCombatSeeds));

        // A caller can mutate a returned report, but that object must never become the trusted cache.
        var returned = host.Service.Details(run.Id, default);
        var returnedReport = (TowerBossSearchReport)returned.GetType().GetProperty("BossSearch")!.GetValue(returned)!;
        var mutableSelection = Assert.IsAssignableFrom<IList<PartyChoice>>(returnedReport.Selection);
        mutableSelection[0] = mutableSelection[0] with { Id = "caller mutation" };
        var fresh = JsonSerializer.SerializeToElement(host.Service.Details(run.Id, default), HarnessJson.Options);
        Assert.Equal(report.Selection[0].Id, fresh.GetProperty("bossSearch").GetProperty("selection")[0].GetProperty("id").GetString());
        using (var cancelledRead = new CancellationTokenSource())
        {
            cancelledRead.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(() => host.Service.Details(run.Id, cancelledRead.Token));
        }

        // Same length and timestamp must not permit a hash check to be skipped after a successful read.
        var markdownPath = Path.Combine(output, "boss-search.md");
        var markdown = File.ReadAllBytes(markdownPath);
        var stamp = File.GetLastWriteTimeUtc(markdownPath);
        var changed = markdown.ToArray(); changed[0] ^= 1;
        File.WriteAllBytes(markdownPath, changed); File.SetLastWriteTimeUtc(markdownPath, stamp);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
        File.WriteAllBytes(markdownPath, markdown); File.SetLastWriteTimeUtc(markdownPath, stamp);
        var addedPath = Path.Combine(output, "unrecorded.json");
        File.WriteAllText(addedPath, "{}");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
        File.Delete(addedPath);
        File.Delete(markdownPath);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
        File.WriteAllBytes(markdownPath, markdown);

        // Rehashing a forged selection changes the cache identity and must trigger native reconstruction.
        var selectionPath = Path.Combine(output, "selection.json");
        var manifestPath = Path.Combine(output, "files.json");
        var selection = File.ReadAllBytes(selectionPath); var manifest = File.ReadAllBytes(manifestPath);
        File.WriteAllText(selectionPath, "[]");
        var files = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
        files["selection.json"] = HarnessJson.FileHash(selectionPath);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(files, HarnessJson.Options));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}/download/json")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/{battle}")).StatusCode);
        File.WriteAllBytes(selectionPath, selection); File.WriteAllBytes(manifestPath, manifest);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);

        File.AppendAllText(Path.Combine(output, "boss-search.json"), " ");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}/download/json")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/{battle}")).StatusCode);
        host.Service.Replay(new(run.Id, battle));
        Assert.Equal("Failed", (await Wait(host.Service)).Status);
    }

    [Fact]
    public async Task Boss_api_run_and_cancel_preserve_completed_trials_without_confirmed_claims()
    {
        await using var host = await Host.Start();
        await host.Authorize();
        var response = await host.Client.PostAsJsonAsync("/api/bosses", new DashboardBossRequest(3, 4, "thorough", 763124));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<DashboardJob>(HarnessJson.Options);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (host.Service.Job!.Completed == 0) await Task.Delay(10, timeout.Token);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsJsonAsync($"/api/jobs/{started!.Id}/cancel", new { })).StatusCode);
        var cancelled = await Wait(host.Service);
        Assert.Equal("Cancelled", cancelled.Status);
        var run = Assert.Single(host.Service.Runs());
        Assert.Equal("Cancelled", run.Status);
        Assert.True(run.Valid > 0);
        Assert.Equal(run.Valid, cancelled.Completed);
        var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/runs/{run.Id}");
        Assert.True(details.GetProperty("isBossSearch").GetBoolean());
        Assert.True(details.GetProperty("isPartialEvidence").GetBoolean());
        Assert.Equal(0, details.GetProperty("report").GetProperty("cells").GetArrayLength());
        Assert.Equal(0, details.GetProperty("loadouts").GetArrayLength());
        Assert.NotEmpty(details.GetProperty("battles").EnumerateArray());
        var saved = await host.Client.GetFromJsonAsync<TowerBossSearchReport>($"/api/runs/{run.Id}/download/json", HarnessJson.Options);
        Assert.Equal("Cancelled", saved!.Status);
        var battle = details.GetProperty("battles")[0].GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/runs/{run.Id}/recipe/{battle}")).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/replays", new DashboardReplayRequest(run.Id, battle))).StatusCode);
        Assert.Equal("Complete", (await Wait(host.Service)).Status);
    }

    private static async Task<DashboardJob> Wait(TowerDashboardService service)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (service.Job?.Status is "Running" or "Cancelling") await Task.Delay(20, timeout.Token);
        await Task.Delay(50, timeout.Token);
        return service.Job!;
    }

    private sealed class Host : IAsyncDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "tower-dashboard-boss-tests-" + Guid.NewGuid().ToString("N"));
        private WebApplication _app = null!;
        public TowerDashboardService Service { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;
        public static async Task<Host> Start()
        {
            var host = new Host();
            host.Service = new(Root, Catalogs, host.Directory);
            host._app = TowerDashboardServer.Create(host.Service, 0);
            await host._app.StartAsync();
            host.Client = new() { BaseAddress = new Uri(host._app.Urls.Single()), Timeout = TimeSpan.FromSeconds(90) };
            return host;
        }
        public async Task Authorize()
        {
            var session = await Client.GetFromJsonAsync<JsonElement>("/api/session");
            Client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await Service.DisposeAsync();
            await _app.DisposeAsync();
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
        }
    }
}
