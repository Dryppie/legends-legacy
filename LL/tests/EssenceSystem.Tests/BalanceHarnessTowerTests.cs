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
using Services.LL.Interfaces.WorldTower;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixture => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"));
    private static TowerScenario Scenario => HarnessJson.Read<TowerScenario>(Fixture);
    private static ThreatAndTankingOptions Threat
    {
        get
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "appsettings.json")),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return settings.RootElement.GetProperty("Combat").GetProperty("ThreatAndTanking")
                .Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)!;
        }
    }

    [Theory]
    [InlineData(1337)]
    [InlineData(17)]
    [InlineData(-12345)]
    [InlineData(0)]
    [InlineData(1337, 1, true)]
    [InlineData(1337, 2, true)]
    [InlineData(1337, 3, true)]
    [InlineData(1337, 4, true)]
    [InlineData(17, 5, true)]
    [InlineData(1337, 6, true)]
    [InlineData(1337, 7, true)]
    [InlineData(1337, 8, true)]
    [InlineData(1337, 9, true)]
    [InlineData(1337, 10, true)]
    [InlineData(1337, 11, true)]
    [InlineData(1337, 12, true)]
    [InlineData(1337, 13, true)]
    [InlineData(1337, 14, true)]
    [InlineData(1337, 15, true)]
    [InlineData(1337, 1, true, "early")]
    [InlineData(1337, 5, true, "mid")]
    [InlineData(1337, 15, true, "late")]
    [InlineData(1337, 11, true, "essences-5", "tower-essence-slots.json")]
    [InlineData(1337, 11, true, "essences-6", "tower-essence-slots.json")]
    [InlineData(1337, 1, true, "entry-4", "tower-anchors.json")]
    [InlineData(1337, 10, true, "floor-10-6", "tower-anchors.json")]
    [InlineData(1337, 1, true, "rank-1", "tower-entry-ranks.json")]
    [InlineData(1337, 1, true, "rank-3", "tower-entry-ranks.json")]
    [InlineData(1337, 1, true, "standard-rank-1", "tower-entry-uncommon.json")]
    [InlineData(1337, 1, true, "fine-rank-2", "tower-entry-uncommon.json")]
    [InlineData(1337, 1, true, "balanced", "tower-curve.json")]
    [InlineData(1337, 10, true, "support", "tower-curve.json")]
    [InlineData(1337, 15, true, "pressure", "tower-curve.json")]
    [InlineData(1337, 1, true, "candidate-15", "tower-essence-search.json")]
    [InlineData(1337, 7, true, "candidate-03", "tower-essence-search.json")]
    [InlineData(1337, 10, true, "candidate-12", "tower-essence-search.json")]
    [InlineData(1337, 1, true, "balanced", "tower-curve.json", 1)]
    [InlineData(17, 1, true, "balanced", "tower-curve.json", 1)]
    [InlineData(1337, 1, true, "balanced", "tower-curve.json", 2)]
    [InlineData(1337, 1, true, "balanced", "tower-curve.json", 3)]
    [InlineData(1337, 1, true, "balanced", "tower-curve.json", 5)]
    [InlineData(1337, 10, true, "balanced", "tower-curve.json", 1)]
    [InlineData(1337, 15, true, "balanced", "tower-curve.json", 1)]
    [InlineData(1701, 1, false, "standard-rank-1", null, 0, true)]
    [InlineData(2903, 10, false, "fine-rank-2", null, 0, true)]
    [InlineData(-12345, 15, false, "standard-rank-2", null, 0, true)]
    [InlineData(4111, 1, false, "standard-rank-1", null, 0, true, true)]
    [InlineData(5227, 10, false, "standard-rank-1", null, 0, true, true)]
    [InlineData(7451, 15, false, "standard-rank-1", null, 0, true, true)]
    [InlineData(8563, 1, false, null, null, 0, false, false, true)]
    [InlineData(9677, 10, false, null, null, 0, false, false, true)]
    [InlineData(10871, 15, false, null, null, 0, false, false, true)]
    [InlineData(6721, 1, false, null, null, 0, false, false, false, 6)]
    [InlineData(6721, 10, false, null, null, 0, false, false, false, 6)]
    [InlineData(6721, 15, false, null, null, 0, false, false, false, 6)]
    [InlineData(10921, 1, false, null, null, 0, false, false, false, 10)]
    [InlineData(10921, 10, false, null, null, 0, false, false, false, 10)]
    [InlineData(10921, 15, false, null, null, 0, false, false, false, 10)]
    [InlineData(24071, 1, false, null, null, 0, false, false, false, 6, "repeat")]
    [InlineData(24071, 10, false, null, null, 0, false, false, false, 6, "alternating")]
    [InlineData(24071, 15, false, null, null, 0, false, false, false, 6, "repeat")]
    [InlineData(24079, 1, false, null, null, 0, false, false, false, 10, "alternating")]
    [InlineData(24079, 10, false, null, null, 0, false, false, false, 10, "repeat")]
    [InlineData(24079, 15, false, null, null, 0, false, false, false, 10, "alternating")]
    public async Task Matches_independent_persisted_normal_Tower_preparation_playback_and_outcome(int seed, int floorNumber = 1, bool benchmark = false, string? preset = null, string? catalogFile = null, int reverseSlot = 0, bool loadoutPilot = false, bool alternativeAllies = false, bool jointParty = false, int progressionSlots = 0, string? wholeDeployment = null)
    {
        var scenario = Scenario;
        if (benchmark)
        {
            var catalog = catalogFile == TowerEssenceSearch.ConfigFile
                ? TowerEssenceSearch.Generate(HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(Path.GetDirectoryName(Fixture)!, catalogFile)),
                    HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Fixture)!, "tower-curve.json")))
                : HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Fixture)!, catalogFile ?? (preset is null ? "tower-benchmark.json" : "tower-progression.json")));
            scenario = TowerBenchmark.Expand(catalog, Root, seed, 1).Single(s => s.Id == $"floor-{floorNumber}.{preset ?? "mixed"}");
            seed = scenario.Seeds[0];
        }
        if (reverseSlot > 0) scenario = scenario with { Party = scenario.Party.Select(p => p.PartySlot == reverseSlot
            ? p with { Build = p.Build with { IdentityEssenceIds = p.Build.EssenceIds, EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } } : p).ToArray() };
        if (loadoutPilot)
        {
            scenario = TowerLoadoutPilot.Scenarios(Root, Path.GetDirectoryName(Fixture)!, TowerLoadoutPilot.Default.Gear.Single(g => g.Id == preset))
                .Single(s => s.FloorNumber == floorNumber);
            scenario = TowerLoadoutPilot.Apply(scenario, 2, new("whole-loadout", "parity",
                ["essence.horned_wolf", "essence.dire_wolf", "essence.forest_spirit", "essence.lumo_wisp"]), [seed]);
            if (alternativeAllies)
            {
                scenario = TowerLoadoutReliability.Contexts(TowerLoadoutReliability.Default, Root, Path.GetDirectoryName(Fixture)!)
                    ["standard-rank-1--previous-05"].Single(s => s.FloorNumber == floorNumber);
                scenario = TowerLoadoutPilot.Apply(scenario, 2, TowerLoadoutReliability.Default.FixedCandidates![0], [seed]);
            }
        }
        if (jointParty)
        {
            scenario = TowerPartySearch.Contexts(Root, Path.GetDirectoryName(Fixture)!, 0).Values.Last().Single(s => s.FloorNumber == floorNumber);
            scenario = TowerPartySelection.Apply(scenario, new Dictionary<int, IReadOnlyList<string>>
            {
                [1] = scenario.Party[0].Build.EssenceIds.Reverse().ToArray(),
                [2] = TowerPartySelection.CandidateC.Essences,
                [3] = TowerLoadoutReliability.Default.FixedCandidates![0].Essences,
                [4] = TowerLoadoutReliability.Default.FixedCandidates![1].Essences
            }, [seed]);
        }
        if (progressionSlots > 0)
        {
            var d = TowerPartyProgression.Definition(Root, Path.GetDirectoryName(Fixture)!, progressionSlots, seed);
            scenario = TowerPartySearch.Contexts(Root, Path.GetDirectoryName(Fixture)!, 0, d.Budget).Values.Last().Single(s => s.FloorNumber == floorNumber);
            scenario = TowerPartySelection.Apply(scenario, d.ReferenceBuilds!, [seed]);
        }
        if (wholeDeployment is not null)
        {
            var d = TowerWholeParty.Definition(Root, Path.GetDirectoryName(Fixture)!, progressionSlots, seed);
            scenario = TowerPartySearch.Contexts(Root, Path.GetDirectoryName(Fixture)!, 0, d).Values.Last().Single(s => s.FloorNumber == floorNumber);
            var first = d.ReferenceBuilds!.ToDictionary(p => p.Key, p => p.Value);
            first[5] = first[5].Reverse().ToArray();
            var second = first.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Reverse().ToArray());
            scenario = TowerPartySelection.Apply(scenario, TowerWholeParty.Deploy(first, second, wholeDeployment, d.WholeParty!.MaximumPartySlots), [seed]);
        }
        var content = new OfflineContent(Root, Threat);
        var runner = new TowerBattleRunner(Root, content);
        var input = runner.CreateInput(scenario, seed, Threat, 10);
        var harnessRuntime = await runner.PrepareAsync(input);
        var harness = await runner.RunAsync(input);

        // Independent normal route: create real characters from recipes, persist snapshots and
        // reload their navigation data, then invoke the same factory/executor as WorldTowerService.
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ItemBases.AddRange(content.Equipment.EquipmentBases.Values);
        var builds = scenario.Party.OrderBy(p => p.PartySlot).Select(p => content.CreateBuild(p.Build)).ToArray();
        foreach (var build in builds)
        {
            var snapshotId = Guid.NewGuid();
            db.CharacterSnapshots.Add(new CharacterSnapshot
            {
                Id = snapshotId, CharacterId = build.Character.Id, Name = build.Character.Name, Level = build.Character.Level,
                BaseAttributes = build.Character.BaseAttributes.Select(a => new EntityAttributeSnapshot
                    { CharacterSnapshotId = snapshotId, AttributeType = a.AttributeType, Value = a.Value }).ToArray(),
                Equipment = build.Character.EquipmentSlots.Select(e => EquipmentSnapshot.From(e.EquipmentSlotType, e.EquipmentInstance!)).ToArray(),
                EquippedEssences = build.EquippedEssences.Select((e, i) => EquippedEssenceSnapshot.From(snapshotId, i, e)).ToArray()
            });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var snapshots = await db.CharacterSnapshots.AsNoTracking().Include(s => s.BaseAttributes)
            .Include(s => s.Equipment).ThenInclude(e => e.InstanceModifiers).Include(s => s.EquippedEssences).ToArrayAsync();
        var setup = content.CreateSetup(builds[0].Character, builds[0].EquippedEssences);
        var pipeline = new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup);
        var floor = new JsonWorldTowerDefinitionProvider(Path.Combine(Root, "Data/world-tower/tower-floors.json"), HarnessJson.Options).GetFloor(floorNumber)!;
        var guardian = HarnessJson.Read<JsonElement>(Path.Combine(Root, "Data/world/creatures.json"))
            .GetProperty("creatures").EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == floor.GuardianCreatureId)
            .Deserialize<Creature>(HarnessJson.Options)!;
        var requests = builds.Select((b, index) => new SnapshotCombatantRequest(snapshots.Single(s => s.CharacterId == b.Character.Id),
            new(b.Character.Id.ToString(), b.Character.Id, CombatSide.Friendly, WorldTowerPartyRules.GetPartyNumber(index + 1)))).ToArray();
        var normal = await new WorldTowerCombatRuntimeFactory(pipeline).CreateAsync(new WorldTowerCombatRuntimeRequest(
            Guid.NewGuid(), Guid.NewGuid(), floor, requests, guardian, 0, 0, 0, scenario.StartsAt, seed), default);

        Assert.Equal(floor.RequiredSlots, normal.FriendlyParticipants.Count);
        Assert.Equal(Enumerable.Range(1, floor.RequiredSlots).Select(WorldTowerPartyRules.GetPartyNumber), normal.FriendlyParticipants.Select(p => p.Slot.PartyNumber!.Value));
        Assert.Equal(CombatContentType.WorldTower, harnessRuntime.Plan.ContentType);
        Assert.Equal(normal.Plan.Mode, harnessRuntime.Plan.Mode);
        Assert.Equal(normal.Plan.RandomSeed, harnessRuntime.Plan.RandomSeed);
        Assert.Equal(HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(normal)), HarnessJson.Hash(harness.Battle.PreparedParticipants));
        Assert.Equal(HarnessJson.Hash(normal.HostileParticipants.Single().Combatant.StaggerDefinition),
            HarnessJson.Hash(harnessRuntime.HostileParticipants.Single().Combatant.StaggerDefinition));
        Assert.Equal(floor.RequiredSlots, harnessRuntime.HostileParticipants.Single().Combatant.StaggerParticipantCount);

        var execution = await content.CreateExecutor().ExecuteTowerPlaybackAsync(normal, 10, default);
        var resolution = new CombatEncounterResultFactory().Create(normal, execution.Result);
        Assert.Equal(HarnessJson.Hash(BattleSummary.From(resolution.CombatResult, 6000)), HarnessJson.Hash(harness.Battle.Summary));
        Assert.Equal(resolution.Outcome == BattleOutcome.Victory, harness.Succeeded);
        var state = resolution.HostilePostState.Single();
        Assert.Equal(state.MaxHealth <= 0 ? 0 : Math.Round(100m * state.Health / state.MaxHealth, 2), harness.GuardianHealthRemainingPercent);
        Assert.Equal((int)Math.Ceiling(execution.Result.Duration / (double)FastCombatEngine.TicksPerSecond), harness.DisplayDurationSeconds);
        Assert.Equal(HarnessJson.Hash(harness), HarnessJson.Hash(await runner.RunAsync(input)));
        var detailed = await runner.RunAsync(input, detailed: true);
        Assert.NotEmpty(detailed.Battle.EventLog!);
        Assert.Equal(HarnessJson.Hash(harness.Battle.Summary), HarnessJson.Hash(detailed.Battle.Summary));
        Assert.Equal(harness.Succeeded, detailed.Succeeded);
    }

    [Theory]
    [InlineData("floor")]
    [InlineData("slots")]
    [InlineData("duplicate-slot")]
    [InlineData("duplicate-character")]
    [InlineData("locked-essences")]
    [InlineData("unknown-equipment")]
    [InlineData("scouting")]
    [InlineData("seeds")]
    public void Rejects_invalid_recipes(string defect)
    {
        var scenario = Scenario;
        var party = scenario.Party.ToArray();
        switch (defect)
        {
            case "floor": scenario = scenario with { FloorNumber = 999 }; break;
            case "slots": scenario = scenario with { Party = party[..4] }; break;
            case "duplicate-slot": party[1] = party[1] with { PartySlot = 1 }; break;
            case "duplicate-character": party[1] = party[1] with { Build = party[0].Build }; break;
            case "locked-essences": party[0] = party[0] with { Build = party[0].Build with { CharacterLevel = 1 } }; break;
            case "unknown-equipment": party[0] = party[0] with { Build = party[0].Build with { Equipment = [new(Domain.Models.Items.Equipments.Slots.EquipmentSlotType.MainHand, "unknown")] } }; break;
            case "scouting": scenario = scenario with { PreparationState = "cleared" }; break;
            case "seeds": scenario = scenario with { Seeds = [1337, 1337] }; break;
        }
        if (defect is not ("floor" or "slots" or "scouting" or "seeds")) scenario = scenario with { Party = party };
        var content = new OfflineContent(Root, Threat);
        Assert.ThrowsAny<Exception>(() => new TowerBattleRunner(Root, content).CreateInput(scenario, 1337, Threat, 10));
    }

    [Fact]
    public async Task Party_recipe_order_preserves_slot_order_identity_and_gameplay()
    {
        var content = new OfflineContent(Root, Threat);
        var runner = new TowerBattleRunner(Root, content);
        var scenario = Scenario;
        var ordered = runner.CreateInput(scenario, 1337, Threat, 10);
        var reversed = runner.CreateInput(scenario with { Party = scenario.Party.Reverse().ToArray() }, 1337, Threat, 10);
        Assert.Equal(HarnessJson.Hash(ordered.Party), HarnessJson.Hash(reversed.Party));
        Assert.Equal(HarnessJson.Hash(await runner.RunAsync(ordered)), HarnessJson.Hash(await runner.RunAsync(reversed)));
    }

    [Fact]
    public async Task Rejects_altered_materialized_party_and_rules()
    {
        var content = new OfflineContent(Root, Threat);
        var runner = new TowerBattleRunner(Root, content);
        var input = runner.CreateInput(Scenario, 1337, Threat, 10);
        await Assert.ThrowsAsync<InvalidDataException>(() => runner.RunAsync(input with { Rules = input.Rules with { StartActiveAbilitiesOnCooldown = false } }));
        var party = input.Party.ToArray();
        party[0] = party[0] with { PartyNumber = 2 };
        await Assert.ThrowsAsync<InvalidDataException>(() => runner.RunAsync(input with { Party = party }));
    }

    [Theory]
    [InlineData("input")]
    [InlineData("content")]
    [InlineData("result")]
    [InlineData("missing-result")]
    [InlineData("execution")]
    public async Task Bundle_reports_replays_and_rejects_damaged_evidence(string defect)
    {
        using var temp = new TemporaryDirectory();
        var fixture = Path.Combine(temp.Path, "scenario.json");
        HarnessJson.WriteNew(fixture, Scenario with { Seeds = [1337] });
        var run = Path.Combine(temp.Path, "run");
        var score = await TowerBundle.CreateAsync(Root, fixture, run);
        Assert.Equal("Complete", score.Status);
        Assert.Equal(1, score.Valid);
        Assert.Equal(1, score.Wins + score.Defeats + score.Draws);
        Assert.Contains("starter 50–90% band does not apply", File.ReadAllText(Path.Combine(run, "scorecard.md")));
        Assert.Equal(HarnessJson.Hash(score), HarnessJson.Hash(HarnessJson.Read<TowerScorecard>(Path.Combine(run, "scorecard.json"))));
        var replay = await TowerBundle.ReplayAsync(run, "tower.0001", true);
        Assert.NotEmpty(replay.Battle.EventLog!);
        Assert.Equal(HarnessJson.Hash(score.Trials[0].Report.Battle.Summary), HarnessJson.Hash(replay.Battle.Summary));
        await Assert.ThrowsAsync<IOException>(() => TowerBundle.CreateAsync(Root, fixture, run));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBundle.ReplayAsync(run, "../tower.0001", false));
        var path = Path.Combine(run, defect switch
        {
            "input" => "tower-input.json", "content" => "content/Data/world-tower/tower-floors.json",
            "execution" => "tower-manifest.json", _ => "battles/tower.0001.json"
        });
        if (defect == "missing-result") File.Delete(path);
        else if (defect == "input") File.WriteAllText(path, File.ReadAllText(path).Replace("starter-party-v1", "starter-party-v2"));
        else if (defect == "execution")
        {
            var manifest = HarnessJson.Read<TowerManifest>(path);
            File.WriteAllText(path, JsonSerializer.Serialize(manifest with { Execution = manifest.Execution with { Runtime = "different" } }, HarnessJson.Options));
        }
        else File.AppendAllText(path, " ");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBundle.ReplayAsync(run, "tower.0001", true));
    }

    [Fact]
    public async Task Cancellation_preserves_partial_results_without_completing_the_run()
    {
        using var temp = new TemporaryDirectory();
        var run = Path.Combine(temp.Path, "run");
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerBundle.CreateAsync(Root, Fixture, run,
            cancellation.Token, _ => cancellation.Cancel()));
        var score = HarnessJson.Read<TowerScorecard>(Path.Combine(run, "scorecard.json"));
        Assert.Equal("Cancelled", score.Status);
        Assert.Equal(1, score.Valid);
        Assert.Equal(1, score.Cancelled);
        Assert.Equal(18, score.NotRun);
        await TowerBundle.ReplayAsync(run, "tower.0001", true);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "balance-tower-tests-" + Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
