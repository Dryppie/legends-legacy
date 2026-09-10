using BalanceHarness;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerPartyProgressionTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Theory]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public async Task Every_slot_budget_prepares_every_floor_with_legal_extensions_and_pinned_identities(int count)
    {
        var d = TowerPartyProgression.Definition(Root, Catalogs, count, 3700 + count);
        Assert.Equal(5040, TowerPartySelection.Validate(d));
        Assert.Equal(count, d.Budget!.EssenceSlots);
        Assert.Equal(count >= 6 ? 10 : 1, d.Budget.PriorityFloor);
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var settings = new TowerSettings(config.RootElement.GetProperty("Combat").GetProperty("ThreatAndTanking")
            .Deserialize<Services.LL.Combat.Engine.ThreatAndTankingOptions>(HarnessJson.Options)!, 10);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        foreach (var context in TowerPartySearch.Contexts(Root, Catalogs, 0, d.Budget).Values)
            foreach (var scenario in context)
            {
                Assert.All(scenario.Party, p => Assert.Equal(count, p.Build.EssenceIds.Count));
                var changed = TowerPartySelection.Apply(scenario, d.ReferenceBuilds!, [100]);
                Assert.All(changed.Party.Zip(scenario.Party), p => Assert.Equal(p.Second.Build.IdentityEssenceIds, p.First.Build.IdentityEssenceIds));
                await runner.PrepareAsync(runner.CreateInput(changed, 100, settings.Threat, settings.CheckpointIntervalTicks));
            }
        Assert.All(d.StartingLoadouts!, pair => Assert.All(pair.Value, ids => Assert.Equal(count, ids.Count)));
        Assert.Equal(HarnessJson.Hash(d), HarnessJson.Hash(TowerPartyProgression.Definition(Root, Catalogs, count, 3700 + count)));
    }

    [Fact]
    public void Study_budgets_and_seed_ledger_are_separate_and_reuse_is_rejected()
    {
        var exclusions = TowerPartyProgression.HistoricalSeeds.ToList();
        Assert.Equal(1578, exclusions.Count);
        var total = 0;
        foreach (var slots in new[] { 6, 5, 7, 8, 9, 10 })
        {
            var d = TowerPartyProgression.Definition(Root, Catalogs, slots, 202609110 + slots, exclusions, slots == 6);
            total += TowerPartySelection.Validate(d);
            var seeds = TowerPartyProgression.CombatSeeds(d);
            Assert.Empty(seeds.Intersect(exclusions)); exclusions.AddRange(seeds);
            Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(d with { ExcludedCombatSeeds = exclusions }));
            Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(d with { Budget = d.Budget! with { CharacterLevel = 1 } }));
        }
        Assert.Equal(41040, total);
    }

    [Fact]
    public void Priority_floor_changes_selection_signal_without_changing_all_floor_totals()
    {
        PartyFloorScore Cell(int floor, bool clear) => new("context", floor, [clear], 0, 50, 50, []);
        var control = new[] { Cell(1, false), Cell(10, false) };
        var entry = new[] { Cell(1, true), Cell(10, false) };
        var tower = new[] { Cell(1, false), Cell(10, true) };
        Assert.Equal(1, TowerPartySelection.Fitness(entry, control, 1).EntryWins);
        Assert.Equal(0, TowerPartySelection.Fitness(entry, control, 10).EntryWins);
        Assert.Equal(1, TowerPartySelection.Fitness(tower, control, 10).EntryWins);
        Assert.Equal(TowerPartySelection.Fitness(tower, control).Wins, TowerPartySelection.Fitness(entry, control).Wins);
    }

    [Theory]
    [InlineData(6)] [InlineData(10)]
    public async Task Progression_archive_reconstructs_all_stages_and_replays_later_floors(int slots)
    {
        using var temp = new Temp();
        var d = TowerPartyProgression.Definition(Root, Catalogs, slots, 5100 + slots) with
        { CandidatesPerArm = 6, PartyCandidates = 8, ConfirmationSamples = 1, PartySamples = 1 };
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerPartySearch.RunAsync(Root, Catalogs, output, d);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerPartySearch.VerifyAsync(output)));
        Assert.Contains($"{slots} Essences, level", File.ReadAllText(Path.Combine(output, "party-search.md")));
        foreach (var cell in report.Confirmation[1].Cells.Where(c => c.Context.EndsWith("balanced", StringComparison.Ordinal) && c.Floor is 1 or 10 or 15))
            Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(output, cell.Trials[0], true)).Battle.EventLog!);
    }

    [Fact]
    public async Task Http_loadout_workflow_previews_cost_cancels_and_preserves_partial_trials()
    {
        using var temp = new Temp();
        await using var service = new TowerDashboardService(Root, Catalogs, temp.Path);
        await using var app = TowerDashboardServer.Create(service, 0);
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromMinutes(3) };
        var request = new DashboardLoadoutRequest(6, "coverage", 88271);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/loadouts", request)).StatusCode);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");
        client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        var response = await client.PostAsJsonAsync("/api/loadout-plan", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5040, plan.GetProperty("maximumBattles").GetInt32());
        Assert.Equal(10, plan.GetProperty("definition").GetProperty("budget").GetProperty("priorityFloor").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/loadout-plan", request with { Slots = 11 })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/loadouts", request)).StatusCode);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        while (service.Job?.Completed == 0 && service.Job.Status == "Running") await Task.Delay(20, timeout.Token);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/loadouts", request)).StatusCode);
        await client.PostAsJsonAsync($"/api/jobs/{service.Job!.Id}/cancel", new { });
        while (service.Job.Status is "Running" or "Cancelling") await Task.Delay(20, timeout.Token);
        Assert.Equal("Cancelled", service.Job.Status);
        var path = service.ResolveRun(service.Job.Run!);
        Assert.NotEmpty(TowerLoadoutArchive.Verify(path));
        Assert.Equal("Loadouts", service.Runs().Single().Kind);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/loadout-plan", request)).StatusCode);
        var trial = TowerLoadoutArchive.Verify(path)[0];
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/runs/{service.Job.Run}/recipe/{trial.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/runs/{service.Job.Run}/recipe/unknown")).StatusCode);
        await app.StopAsync();
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-progression-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
