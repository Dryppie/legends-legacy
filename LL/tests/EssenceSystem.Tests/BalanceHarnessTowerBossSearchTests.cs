using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossSearchTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerBossSearchDefinition Small(int seed = 771921) {
        var d = TowerBossSearch.Definition(Root, Catalogs, 3, 4, seed, "any", "coverage", refinement: false);
        return d with { GenerationSeeds = d.GenerationSeeds.Take(1).ToArray(), Controls = d.Controls.Take(1).ToArray(),
            CandidatesPerArm = 2, DiscoverySamples = 1, ConfirmationSamples = 1, DiagnosticSamples = 1, Finalists = 10 };
    }

    [Theory]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public async Task Every_slot_budget_preserves_production_party_size_identity_and_gear_on_all_transfer_floors(int slots)
    {
        var d = TowerBossSearch.Definition(Root, Catalogs, 3, slots, 771900 + slots, "add-clearing", "coverage");
        var contexts = TowerBossSearch.Contexts(Root, Catalogs, d);
        var threat = new ThreatAndTankingOptions(); var content = new OfflineContent(Root, threat); var runner = new TowerBattleRunner(Root, content);
        var families = content.Essences.GetAll().ToDictionary(e => e.Id, e => e.SourceMonsterId);
        Assert.Equal(TowerPartyProgression.Budget(slots) with { PriorityFloor = 3 }, d.Budget);
        Assert.True(TowerBossSearch.Validate(d) > 0);
        Assert.Equal(Enumerable.Range(1, 15), contexts.Values.First().Select(s => s.FloorNumber));
        foreach (var control in d.Controls)
            foreach (var scenario in contexts.Values.First())
            {
                var recipe = TowerPartySelection.Apply(scenario, control.Builds, [77]);
                Assert.Equal(scenario.Party.Count, recipe.Party.Count);
                foreach (var pair in scenario.Party.Zip(recipe.Party))
                {
                    Assert.Equal(pair.First.Build.Id, pair.Second.Build.Id);
                    Assert.Equal(pair.First.Build.IdentityEssenceIds, pair.Second.Build.IdentityEssenceIds);
                    Assert.Equal(pair.First.Build.Equipment, pair.Second.Build.Equipment);
                    Assert.Equal(slots, pair.Second.Build.EssenceIds.Count);
                    Assert.Equal(slots, pair.Second.Build.EssenceIds.Select(id => families[id]).Distinct().Count());
                    if (!d.MutablePartySlots.Contains(pair.First.PartySlot)) Assert.Equal(pair.First.Build.EssenceIds, pair.Second.Build.EssenceIds);
                }
                var input = runner.CreateInput(recipe, 77, threat, 10);
                var prepared = await runner.PrepareAsync(input);
                Assert.Equal(input.Floor.RequiredSlots, prepared.FriendlyParticipants.Count);
                Assert.Equal(6000, input.Rules.MaxTicks);
            }
    }

    [Fact]
    public void Context_aliases_count_identical_full_parties_once_and_keep_different_transfer_allies()
    {
        var first = TowerBossSearch.Definition(Root, Catalogs, 1, 4, 771911, "any", "coverage");
        var contexts = TowerBossSearch.Contexts(Root, Catalogs, first);
        var aliases = TowerBossSearch.Aliases(contexts, first.Controls[0]);
        Assert.Equal(30, aliases.Count);
        Assert.Equal(1, first.ContextCounts[1]);
        Assert.Contains(first.ContextCounts.Values, n => n == 2);
        Assert.Equal(aliases.Where(a => a.Floor == 1).First().EvaluatedContext, aliases.Where(a => a.Floor == 1).Last().EvaluatedContext);
        foreach (var group in aliases.GroupBy(a => a.Floor))
            Assert.Equal(first.ContextCounts[group.Key], group.Select(a => a.EvaluatedContext).Distinct().Count());
        var full = TowerBossSearch.Definition(Root, Catalogs, 15, 4, 771912, "any", "coverage");
        Assert.All(full.ContextCounts.Values, n => Assert.Equal(1, n));
        var expected = first.Methods.Count * first.GenerationSeeds.Count * first.CandidatesPerArm * first.DiscoverySamples
            + first.Finalists * first.ConfirmationSamples * first.ContextCounts.Values.Sum() + 4 * first.DiagnosticSamples;
        Assert.Equal(expected, TowerBossSearch.Validate(first));
        var incorrect = first with { ContextCounts = first.ContextCounts.ToDictionary(p => p.Key, p => 2) };
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Contexts(Root, Catalogs, incorrect));
    }

    [Fact]
    public void Expanded_retained_control_portfolio_preserves_confirmation_allocation_and_hard_caps()
    {
        var d = Small();
        var baseline = d.Controls[0];
        // Rotate legal ordered loadouts independently across characters to retain distinct
        // complete parties without adding Essences or changing their source-family legality.
        var controls = Enumerable.Range(0, 33).Select(index => {
            var remainder = index;
            var builds = baseline.Builds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => {
                var offset = remainder % p.Value.Count;
                remainder /= p.Value.Count;
                return (IReadOnlyList<string>)p.Value.Skip(offset).Concat(p.Value.Take(offset)).ToArray();
            });
            return TowerPartySelection.Choice(index == 0 ? "control" : $"retained-{index}", builds);
        }).ToArray();
        Assert.Equal(controls.Length, controls.Select(p => p.Id).Distinct().Count());
        d = d with { Controls = controls.Take(32).ToArray(), CandidatesPerArm = 33,
            Finalists = 32 + d.Methods.Count * d.GenerationSeeds.Count + 5, MaximumBattles = 100000 };
        var cost = TowerBossSearch.Validate(d);
        Assert.Equal(d.Methods.Count * d.GenerationSeeds.Count * d.CandidatesPerArm * d.DiscoverySamples
            * d.ContextCounts[d.Budget.PriorityFloor]
            + d.Finalists * d.ConfirmationSamples * d.ContextCounts.Values.Sum()
            + 4 * d.DiagnosticSamples * d.ContextCounts[d.Budget.PriorityFloor], cost);
        Assert.Equal(cost, TowerBossSearch.Validate(d with { MaximumBattles = cost }));
        Assert.True(TowerBossSearch.Validate(d with { Finalists = 64 }) > cost);
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(d with {
            Controls = controls, CandidatesPerArm = 34, Finalists = d.Finalists + 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(d with { Finalists = d.Finalists - 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(d with { Finalists = 65 }));
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(d with { MaximumBattles = cost - 1 }));
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(d with { MaximumBattles = 100001 }));
    }

    [Fact]
    public void Contract_rejects_cap_seed_pool_budget_and_scope_changes_before_combat()
    {
        var d = Small(); var cost = TowerBossSearch.Validate(d); var schedule = TowerBossSearch.CombatSeeds(d);
        Assert.Equal(schedule.Length, schedule.Distinct().Count());
        Assert.Empty(schedule.Intersect(d.ExcludedCombatSeeds));
        var invalid = new[] {
            d with { MaximumBattles = cost - 1 },
            d with { ConfirmationSeed = d.DiscoverySeed },
            d with { DiagnosticSeed = d.ConfirmationSeed },
            d with { ExcludedCombatSeeds = d.ExcludedCombatSeeds.Append(schedule[0]).ToArray() },
            d with { GenerationSeeds = [d.GenerationSeeds[0], d.GenerationSeeds[0]] },
            d with { Methods = ["random", "joint", "graph"] },
            d with { Finalists = 9 },
            d with { CandidatesPerArm = 1 },
            d with { AllowedEssences = d.AllowedEssences.Skip(1).Append(d.AllowedEssences[1]).ToArray() },
            d with { MutablePartySlots = d.MutablePartySlots.Reverse().ToArray() },
            d with { Controls = [d.Controls[0] with { Id = "tampered" }] },
            d with { Budget = d.Budget with { CharacterLevel = d.Budget.CharacterLevel + 1 } },
            d with { Progression = "level-10" },
            d with { Objective = d.Objective with { ContextPolicy = "pool-identical-contexts" } },
            d with { Objective = d.Objective with { StrategyIntent = "guaranteed-win" } },
            d with { Objective = d.Objective with { TargetFloorWeights = new Dictionary<int, double> { [3] = double.MaxValue, [4] = double.MaxValue } } }
        };
        Assert.All(invalid, changed => Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(changed)));
        var first = TowerBossSearch.Definition(Root, Catalogs, 1, 4, 771913, "any", "coverage");
        var extra = first.Controls.Select(c => TowerPartySelection.Choice(c.Source,
            c.Builds.Append(new KeyValuePair<int, IReadOnlyList<string>>(6, c.Builds[1])).ToDictionary(p => p.Key, p => p.Value))).ToArray();
        Assert.Throws<InvalidDataException>(() => TowerBossSearch.Contexts(Root, Catalogs,
            first with { MutablePartySlots = first.MutablePartySlots.Append(6).ToArray(), Controls = extra }));
        using var temp = new Temp();
        var path = Path.Combine(temp.Path, "invalid.json");
        File.WriteAllText(path, JsonSerializer.Serialize(d, HarnessJson.Options).TrimEnd().TrimEnd('}') + ",\"combatStyle\":\"enabled\"}");
        Assert.Throws<JsonException>(() => TowerBossSearch.Read(path));
    }

    [Fact]
    public async Task Frozen_search_reconstructs_equal_cost_selection_replays_and_matches_independent_normal_Tower()
    {
        using var temp = new Temp(); var output = Path.Combine(temp.Path, "complete");
        // Multiple seeds also exercise the verifier's seed-independent input template against
        // inputs created independently by the normal archive writer for every combat.
        var d = Small() with { DiscoverySamples = 2, ConfirmationSamples = 2, DiagnosticSamples = 2 };
        var report = await TowerBossSearch.RunAsync(Root, Catalogs, output, d);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(0, report.CacheHits);
        Assert.Equal(4, report.Arms.Count);
        Assert.All(report.Arms, a => {
            Assert.Equal(2, a.Evaluations.Count);
            Assert.Equal(d.Controls[0].Id, a.Parties[0].Id);
        });
        Assert.Equal(d.Controls[0].Id, report.Selection[0].Id);
        Assert.All(report.Confirmation, c => Assert.Equal(Enumerable.Range(1, 15), c.Cells.Select(x => x.Floor).Distinct().Order()));
        var trials = TowerLoadoutArchive.Verify(output);
        Assert.Equal(report.ActualBattles, trials.Count);
        Assert.Equal(8 * d.ContextCounts[3] * d.DiscoverySamples, trials.Count(t => t.Stage == "discovery"));
        Assert.Equal(report.Selection.Count * d.ContextCounts.Values.Sum() * d.ConfirmationSamples, trials.Count(t => t.Stage == "confirmation"));
        Assert.Empty(trials.Where(t => t.Stage == "discovery").Select(t => t.Seed).Intersect(trials.Where(t => t.Stage != "discovery").Select(t => t.Seed)));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossSearch.VerifyAsync(output)));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        var trialId = report.Confirmation.First().Cells.First(c => c.Floor == 3).Trials[0];
        var trial = trials.Single(t => t.Id == trialId);
        var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", trial.Recipe + ".json"));
        var saved = TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage);
        await AssertNormalParity(Path.Combine(output, "content"), recipe, trial.Seed, saved, scope.Settings);
        var detailed = await TowerLoadoutArchive.ReplayAsync(output, trial.Id, true);
        Assert.NotEmpty(detailed.Battle.EventLog!);
        Assert.Equal(HarnessJson.Hash(saved.Battle.Summary), HarnessJson.Hash(detailed.Battle.Summary));

        // A modified file manifest cannot hide changed frozen selection from reconstruction.
        File.WriteAllText(Path.Combine(output, "selection.json"), "[]");
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "files.json"));
        files["selection.json"] = HarnessJson.FileHash(Path.Combine(output, "selection.json"));
        File.WriteAllText(Path.Combine(output, "files.json"), JsonSerializer.Serialize(files, HarnessJson.Options));
        Assert.Equal(trials.Count, TowerLoadoutArchive.Verify(output).Count);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossSearch.VerifyAsync(output));
    }

    [Fact]
    public async Task Cancellation_preserves_completed_trials_and_replay_without_confirmation_claims()
    {
        using var temp = new Temp(); using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(temp.Path, "cancelled"); var d = Small(771922);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerBossSearch.RunAsync(Root, Catalogs, output, d,
            cancellation.Token, message => { if (message.Contains("discovery candidate", StringComparison.Ordinal)) cancellation.Cancel(); }));
        var report = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(output, "boss-search.json"));
        Assert.Equal("Cancelled", report.Status);
        Assert.Single(report.Discovery);
        Assert.Empty(report.Selection); Assert.Empty(report.Confirmation);
        var trials = TowerLoadoutArchive.Verify(output);
        Assert.Equal(d.ContextCounts[3], trials.Count);
        Assert.Equal(trials.Count, report.ActualBattles);
        Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(output, trials[0].Id, true)).Battle.EventLog!);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossSearch.VerifyAsync(output));
    }

    private static async Task AssertNormalParity(string root, TowerScenario recipe, int seed, TowerBattleReport saved, TowerSettings settings)
    {
        // Independent persisted snapshot route from the normal Tower service. No harness snapshot
        // converter or preparation method is used to construct these participants.
        var content = new OfflineContent(root, settings.Threat);
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ItemBases.AddRange(content.Equipment.EquipmentBases.Values);
        var builds = recipe.Party.OrderBy(p => p.PartySlot).Select(p => content.CreateBuild(p.Build)).ToArray();
        foreach (var build in builds)
        {
            var id = Guid.NewGuid();
            db.CharacterSnapshots.Add(new CharacterSnapshot {
                Id = id, CharacterId = build.Character.Id, Name = build.Character.Name, Level = build.Character.Level,
                BaseAttributes = build.Character.BaseAttributes.Select(a => new EntityAttributeSnapshot {
                    CharacterSnapshotId = id, AttributeType = a.AttributeType, Value = a.Value }).ToArray(),
                Equipment = build.Character.EquipmentSlots.Select(e => EquipmentSnapshot.From(e.EquipmentSlotType, e.EquipmentInstance!)).ToArray(),
                EquippedEssences = build.EquippedEssences.Select((e, i) => EquippedEssenceSnapshot.From(id, i, e)).ToArray()
            });
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var snapshots = await db.CharacterSnapshots.AsNoTracking().Include(s => s.BaseAttributes).Include(s => s.Equipment)
            .ThenInclude(e => e.InstanceModifiers).Include(s => s.EquippedEssences).ToArrayAsync();
        var setup = content.CreateSetup(builds[0].Character, builds[0].EquippedEssences);
        var pipeline = new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup);
        var floor = new JsonWorldTowerDefinitionProvider(Path.Combine(root, "Data/world-tower/tower-floors.json"), HarnessJson.Options).GetFloor(recipe.FloorNumber)!;
        var guardian = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/world/creatures.json")).GetProperty("creatures").EnumerateArray()
            .Single(c => c.GetProperty("id").GetGuid() == floor.GuardianCreatureId).Deserialize<Creature>(HarnessJson.Options)!;
        var requests = builds.Select((b, index) => new SnapshotCombatantRequest(snapshots.Single(s => s.CharacterId == b.Character.Id),
            new(b.Character.Id.ToString(), b.Character.Id, CombatSide.Friendly, WorldTowerPartyRules.GetPartyNumber(index + 1)))).ToArray();
        var normal = await new WorldTowerCombatRuntimeFactory(pipeline).CreateAsync(new WorldTowerCombatRuntimeRequest(
            Guid.NewGuid(), Guid.NewGuid(), floor, requests, guardian, 0, 0, 0, recipe.StartsAt, seed), default);
        Assert.Equal(floor.RequiredSlots, normal.FriendlyParticipants.Count);
        Assert.Equal(HarnessJson.Hash(saved.Battle.PreparedParticipants), HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(normal)));
        var playback = await content.CreateExecutor().ExecuteTowerPlaybackAsync(normal, settings.CheckpointIntervalTicks, default);
        var resolution = new CombatEncounterResultFactory().Create(normal, playback.Result);
        Assert.Equal(HarnessJson.Hash(saved.Battle.Summary), HarnessJson.Hash(BattleSummary.From(resolution.CombatResult, 6000)));
        Assert.Equal(saved.Succeeded, resolution.Outcome == BattleOutcome.Victory);
        var state = resolution.HostilePostState.Single();
        Assert.Equal(saved.GuardianHealthRemainingPercent, state.MaxHealth <= 0 ? 0 : Math.Round(100m * state.Health / state.MaxHealth, 2));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-boss-search-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
