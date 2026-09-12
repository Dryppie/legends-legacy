using System.Text.Json;
using BalanceHarness;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Engine;
using Services.LL.Items;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessHandoffTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixture => Path.GetFullPath(Path.Combine(ApiRoot, "..", "..", "..", "tools", "BalanceHarness", "Fixtures", CrystalCreekHandoff.Fixture));

    [Fact]
    public void Checkpoint_matches_reward_gates_and_production_Forge_conversion()
    {
        var plan = CrystalCreekHandoff.CreatePlan(ApiRoot, Fixture, 500);
        Assert.Equal(16000, plan.PlannedBattles);
        Assert.Equal(new[] { 618091, 618092 }, plan.SeedSets.Select(s => s.MasterSeed));
        Assert.Equal(new[] { 618093, 618094 }, CrystalCreekHandoff.CreatePlan(ApiRoot, Fixture, 1).SeedSets.Select(s => s.MasterSeed));
        Assert.Equal(new HandoffBudget(500, 100, 400, 1, 1, 1, 11150, 5), plan.Budget);
        var world = HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", "world", "regions.json"));
        var creek = world.GetProperty("regions").EnumerateArray().SelectMany(r => r.GetProperty("areas").EnumerateArray())
            .Single(a => a.GetProperty("id").GetString() == "region_01_area_03");
        var quest = HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", "quests", "region-01", "blood-in-the-grove.v4.json"));
        Assert.Equal(10, creek.GetProperty("levelRequirement").GetInt32());
        Assert.Equal(quest.GetProperty("id").GetString(), creek.GetProperty("requiredCompletedQuestId").GetString());
        var objectives = quest.GetProperty("objectives").EnumerateArray().ToArray();
        Assert.Contains(objectives, o => o.GetProperty("type").GetString() == "CharacterLevelReached" && o.GetProperty("requiredAmount").GetInt32() == 10);
        Assert.Contains(objectives, o => o.GetProperty("type").GetString() == "CombatEncounterCompleted"
            && o.GetProperty("requiredAmount").GetInt32() == 4 && o.GetProperty("filters").GetProperty("requiresVictory").GetBoolean());
        var content = new OfflineContent(ApiRoot, new ThreatAndTankingOptions());
        var builds = plan.Suite.Stages[0].Builds;
        var plain = content.CreateBuild(builds.Single(b => b.Id == "quest-rewards"));
        var styled = content.CreateBuild(builds.Single(b => b.Id == "fury"));
        Assert.Equal(3, plain.Equipment.Count);
        Assert.Equal(2, plain.EquippedEssences.Count);
        var weapon = plain.Character.EquipmentSlots.Single(s => s.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!;
        var actual = styled.Character.EquipmentSlots.Single(s => s.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!.ProgressionData!;
        var equipmentRoot = Path.Combine(ApiRoot, "Data", "equipment");
        var policy = new EquipmentUpgradePolicy(content.Equipment,
            JsonEquipmentUpgradePrices.Load(Path.Combine(equipmentRoot, "equipment-upgrades.v1.json")),
            JsonEquipmentBlueprintCatalog.Load(Path.Combine(equipmentRoot, "equipment-blueprints.v1.json"), content.Equipment));
        plain.Character.Cinders = plan.Budget.StartingCinders;
        var blueprint = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = "item.blueprint_fury" };
        var quote = policy.Quote(new(plain.Character, null, weapon, true, null, [],
                [new InventoryItem { ItemInstanceId = blueprint.Id, ItemInstance = blueprint, Quantity = 1 }]),
            new(EquipmentUpgradeOperationKind.ApplyVariant, weapon.Id, BlueprintStyleId: BloodGroveEntryExperiment.FuryStyle),
            Guid.NewGuid());
        Assert.True(quote.CanExecute, quote.UnavailableReason);
        Assert.Equal(plan.Budget.FuryCinders, quote.CinderCost);
        Assert.Equal(0, quote.PartsCost);
        Assert.Equal(HarnessJson.Hash(quote.After!.Stats), HarnessJson.Hash(actual.Stats));
        Assert.Equal(HarnessJson.Hash(quote.After.BaseStats), HarnessJson.Hash(actual.BaseStats));
        Assert.Equal(quote.After.EquipmentSetId, actual.EquipmentSetId);
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var combat = settings.RootElement.GetProperty("Combat");
        var threat = combat.GetProperty("ThreatAndTanking").Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)!;
        var cadence = combat.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble();
        var resolved = IdleSuite.Resolve(plan.Suite, content, threat, cadence, 618093);
        Assert.Equal(16, resolved.Cells.Count);
        foreach (var group in resolved.Cells.GroupBy(c => (c.Stage, c.Encounter)))
        {
            Assert.Equal(4, group.Count());
            Assert.All(group, c => Assert.Equal(group.First().Trials.Select(t => t.Seed), c.Trials.Select(t => t.Seed)));
        }
        var starter = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(Path.GetDirectoryName(Fixture)!, "idle-blood-grove-starter.json"));
        Assert.Equal("e820b30cf1bdd8763c4ea8c9fdbf138a870611bdfebe728f71454bb348e8a87f", BalanceGoals.FixtureContractHash(starter));
        foreach (var samples in new[] { 0, 501 }) Assert.Throws<ArgumentOutOfRangeException>(() => CrystalCreekHandoff.CreatePlan(ApiRoot, Fixture, samples));
    }

    [Fact]
    public async Task Complete_review_preserves_paired_findings_and_replays_without_a_gameplay_gate()
    {
        using var workspace = new Workspace();
        var output = Path.Combine(workspace.Path, "complete");
        var report = await CrystalCreekHandoff.RunAsync(ApiRoot, Fixture, output, 1, CancellationToken.None);
        Assert.Equal("Complete", report.Status);
        Assert.Equal("Advisory", report.Policy);
        Assert.Equal(32, report.ValidBattles);
        Assert.Equal(32, report.Cells.Count);
        Assert.Equal(24, report.Cells.Count(c => c.ClearRateChange is not null));
        Assert.Equal(8, report.Replays.Count);
        Assert.All(report.Replays, r => Assert.True(File.Exists(Path.Combine(output, r))));
        Assert.Equal(2, report.RunArtifactHashes.Count);
        Assert.False(File.Exists(Path.Combine(output, "baseline.json")));
        Assert.False(File.Exists(Path.Combine(output, "evaluation.json")));
        var hash = HarnessJson.FileHash(Path.Combine(output, "results.json"));
        await Assert.ThrowsAsync<IOException>(() => CrystalCreekHandoff.RunAsync(ApiRoot, Fixture, output, 1, CancellationToken.None));
        Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(output, "results.json")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancelled_or_changed_source_never_produces_a_completed_review(bool cancel)
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var fixture = Path.Combine(workspace.Path, "fixture.json");
        File.Copy(Fixture, fixture);
        var output = Path.Combine(workspace.Path, "partial");
        async Task Run() => await CrystalCreekHandoff.RunAsync(ApiRoot, fixture, output, 1, cancellation.Token, _ =>
        {
            if (cancel) cancellation.Cancel();
            else File.AppendAllText(fixture, " ");
        });
        if (cancel) await Assert.ThrowsAsync<OperationCanceledException>(Run);
        else await Assert.ThrowsAsync<InvalidDataException>(Run);
        Assert.True(File.Exists(Path.Combine(output, "plan.json")));
        Assert.True(File.Exists(Path.Combine(output, "failure.json")));
        Assert.False(File.Exists(Path.Combine(output, "results.json")));
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-handoff-tests"));
        public string Path { get; } = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-handoff-tests", Guid.NewGuid().ToString("N")));
        public Workspace() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
