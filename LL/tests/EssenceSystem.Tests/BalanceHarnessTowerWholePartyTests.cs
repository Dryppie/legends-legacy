using BalanceHarness;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerWholePartyTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Theory]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public async Task All_slot_budgets_deploy_five_roles_legally_on_every_floor(int slots)
    {
        var d = TowerWholeParty.Definition(Root, Catalogs, slots, 70000 + slots);
        Assert.Equal(slots == 6 ? 9720 : 9120, TowerPartySelection.Validate(d));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, TowerPartySelection.Targets(d));
        Assert.NotEmpty(d.StartingLoadouts![5]);
        var original = TowerPartyProgression.Scenarios(Root, Catalogs, d.Budget!);
        var threat = new Services.LL.Combat.Engine.ThreatAndTankingOptions();
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, threat));
        var first = d.ReferenceBuilds!.ToDictionary(p => p.Key, p => p.Value);
        first[5] = first[5].Reverse().ToArray();
        var second = first.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Reverse().ToArray());
        foreach (var policy in new[] { "first-cell", "repeat", "alternating" })
        {
            var builds = TowerWholeParty.Deploy(first, second, policy, d.WholeParty!.MaximumPartySlots);
            foreach (var s in original)
            {
                var changed = TowerPartySelection.Apply(s, builds, [77]);
                Assert.Equal(s.Party.Count, changed.Party.Count);
                foreach (var pair in changed.Party.Zip(s.Party))
                {
                    var slot = pair.First.PartySlot;
                    var expected = policy == "first-cell" && slot > 5 ? pair.Second.Build.EssenceIds
                        : (policy == "alternating" && (slot - 1) / 5 % 2 == 1 ? second : first)[(slot - 1) % 5 + 1];
                    Assert.Equal(expected, pair.First.Build.EssenceIds);
                    Assert.Equal(pair.Second.Build.IdentityEssenceIds, pair.First.Build.IdentityEssenceIds);
                    Assert.Equal(pair.Second.Build.Equipment, pair.First.Build.Equipment);
                }
                await runner.PrepareAsync(runner.CreateInput(changed, 77, threat, 10));
            }
        }
        Assert.Equal(HarnessJson.Hash(d), HarnessJson.Hash(TowerWholeParty.Definition(Root, Catalogs, slots, 70000 + slots)));
    }

    [Fact]
    public void Batch_cost_exclusions_controls_and_legacy_contracts_are_explicit()
    {
        var excluded = new List<int>(); var total = 0;
        foreach (var slots in new[] { 6, 4, 5, 7, 8, 9, 10 })
        {
            var d = TowerWholeParty.Definition(Root, Catalogs, slots, 202609210 + slots, excluded, slots == 6);
            total += TowerPartySelection.Validate(d);
            var seeds = TowerPartyProgression.CombatSeeds(d);
            Assert.Empty(seeds.Intersect(d.ExcludedCombatSeeds)); excluded.AddRange(seeds);
            Assert.Equal(slots == 6 ? 3 : 2, d.SearchSeeds.Count);
            Assert.Equal(slots == 6 ? 6 : 4, d.WholeParty!.Controls.Count);
            Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(d with { ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(seeds).ToArray() }));
            Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(d with { Finalists = 4 }));
            Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(d with { SchemaVersion = 2 }));
            Assert.Throws<InvalidDataException>(() => TowerPartySearch.Contexts(Root, Catalogs, 0, d with { WholeParty = d.WholeParty with { MaximumPartySlots = d.WholeParty.MaximumPartySlots + 5 } }));
        }
        Assert.Equal(72480, total);
    }

    [Theory]
    [InlineData(6)] [InlineData(10)]
    public async Task Whole_party_archive_freezes_controls_and_deployment_trio_and_reconstructs_every_stage(int slots)
    {
        using var temp = new Temp();
        var full = TowerWholeParty.Definition(Root, Catalogs, slots, 78000 + slots);
        var d = full with { SearchSeeds = full.SearchSeeds.Take(1).ToArray(), CandidatesPerArm = 6,
            PartyCandidates = 12, Finalists = 6, ConfirmationSamples = 1,
            WholeParty = full.WholeParty! with { Controls = full.WholeParty.Controls.Take(1).ToArray() } };
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerPartySearch.RunAsync(Root, Catalogs, output, d);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(2340, report.ActualBattles);
        Assert.Equal(10, report.Characters.Count);
        Assert.All(d.WholeParty.Controls, p => Assert.Contains(report.Selection, c => c.Id == p.Id));
        var comparisons = HarnessJson.Read<JsonElement>(Path.Combine(output, "deployment-comparisons.json"));
        Assert.Contains(comparisons.EnumerateArray(), f => f.GetProperty("variants").EnumerateArray().All(v => v.GetProperty("confirmed").GetBoolean()));
        Assert.All(comparisons.EnumerateArray(), f => Assert.Contains(f.GetProperty("variants").EnumerateArray(), v => v.GetProperty("confirmed").GetBoolean()));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerPartySearch.VerifyAsync(output)));
        Assert.Contains("Deployment comparisons", File.ReadAllText(Path.Combine(output, "party-search.md")));
        var deployed = report.Selection.First(p => p.Builds.Count > 5);
        foreach (var floor in new[] { 1, 10, 15 })
        {
            var cell = report.Confirmation.Single(p => p.Id == deployed.Id).Cells.First(c => c.Floor == floor);
            Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(output, cell.Trials[0], true)).Battle.EventLog!);
        }
        File.WriteAllText(Path.Combine(output, "deployment-comparisons.json"), "[]");
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "files.json"));
        files["deployment-comparisons.json"] = HarnessJson.FileHash(Path.Combine(output, "deployment-comparisons.json"));
        File.WriteAllText(Path.Combine(output, "files.json"), JsonSerializer.Serialize(files, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPartySearch.VerifyAsync(output));
    }

    [Fact]
    public async Task Dashboard_previews_new_whole_party_scope_and_keeps_old_scope_available()
    {
        using var temp = new Temp();
        await using var service = new TowerDashboardService(Root, Catalogs, temp.Path);
        await using var app = TowerDashboardServer.Create(service, 0);
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");
        client.DefaultRequestHeaders.Add("X-Tower-Session", session.GetProperty("token").GetString());
        foreach (var whole in new[] { false, true })
        {
            var response = await client.PostAsJsonAsync("/api/loadout-plan", new DashboardLoadoutRequest(6, "thorough", 93001, whole));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var value = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(whole ? 17760 : 15840, value.GetProperty("maximumBattles").GetInt32());
            Assert.Equal(whole ? 3 : 2, value.GetProperty("definition").GetProperty("schemaVersion").GetInt32());
        }
        await app.StopAsync();
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-whole-party-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
