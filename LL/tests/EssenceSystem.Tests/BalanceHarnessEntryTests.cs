using BalanceHarness;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Engine;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessEntryTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string SourceFixture => Path.GetFullPath(Path.Combine(ApiRoot,
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "idle-first-hunt.json"));

    [Fact]
    public void Matrix_covers_every_armor_outcome_and_fury_matches_the_production_forge_quote()
    {
        var plan = BloodGroveEntryExperiment.CreatePlan(ApiRoot, SourceFixture, 100);
        Assert.Equal(108, plan.Builds.Count);
        Assert.Equal(43200, plan.PlannedBattles);
        Assert.Equal(9, plan.Builds.Select(b => b.ArmorId).Distinct().Count());
        Assert.Equal(6, plan.Builds.Select(b => b.SourceBuildId).Distinct().Count());
        Assert.Equal(new[] { 1337, 940031 }, plan.SeedSets.Select(s => s.MasterSeed));
        Assert.Equal(BalanceGoals.FixtureContractHash(plan.Suite), plan.FixtureHash);
        var content = new OfflineContent(ApiRoot, new ThreatAndTankingOptions());
        var root = Path.Combine(ApiRoot, "Data", "equipment");
        var policy = new EquipmentUpgradePolicy(content.Equipment,
            JsonEquipmentUpgradePrices.Load(Path.Combine(root, "equipment-upgrades.v1.json")),
            JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "equipment-blueprints.v1.json"), content.Equipment));
        foreach (var budget in plan.Builds.Where(b => b.Fury))
        {
            Assert.Equal(100, budget.CindersSpent);
            Assert.Equal(400, budget.CindersRemaining);
            Assert.Equal(1, budget.BlueprintsSpent);
            Assert.Equal(0, budget.BlueprintsRemaining);
            var plainBudget = plan.Builds.Single(b => !b.Fury && b.ArmorId == budget.ArmorId && b.SourceBuildId == budget.SourceBuildId);
            Assert.Equal(500, plainBudget.CindersRemaining);
            Assert.Equal(1, plainBudget.BlueprintsRemaining);
            var plain = content.CreateBuild(plan.Suite.Stages[0].Builds.Single(b => b.Id == plainBudget.BuildId));
            var styled = content.CreateBuild(plan.Suite.Stages[0].Builds.Single(b => b.Id == budget.BuildId));
            var weapon = plain.Character.EquipmentSlots.Single(s => s.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!;
            var actual = styled.Character.EquipmentSlots.Single(s => s.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!.ProgressionData!;
            plain.Character.Cinders = 500;
            var blueprint = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = "item.blueprint_fury" };
            var quote = policy.Quote(new(plain.Character, null, weapon, true, null, [],
                [new InventoryItem { ItemInstanceId = blueprint.Id, ItemInstance = blueprint, Quantity = 1 }]),
                new(EquipmentUpgradeOperationKind.ApplyVariant, weapon.Id, BlueprintStyleId: BloodGroveEntryExperiment.FuryStyle),
                Guid.NewGuid(), DateTimeOffset.UnixEpoch);
            Assert.True(quote.CanExecute, quote.UnavailableReason);
            Assert.Equal(budget.CindersSpent, quote.CinderCost);
            Assert.Equal(0, quote.PartsCost);
            Assert.Equal(HarnessJson.Hash(quote.After!.Stats), HarnessJson.Hash(actual.Stats));
            Assert.Equal(HarnessJson.Hash(quote.After.BaseStats), HarnessJson.Hash(actual.BaseStats));
            Assert.Equal(quote.After.EquipmentSetId, actual.EquipmentSetId);
            Assert.True(actual.State.AdditiveVariantBonus);
            Assert.Equal(2, styled.Equipment.Count);
            Assert.All(styled.Equipment, e => Assert.Equal(0, e.ProgressionData!.State.Rank));
        }
        foreach (var samples in new[] { 0, 101 })
            Assert.Throws<ArgumentOutOfRangeException>(() => BloodGroveEntryExperiment.CreatePlan(ApiRoot, SourceFixture, samples));
    }

    [Fact]
    public async Task Complete_experiment_archives_pairs_and_replays_without_promoting_a_baseline()
    {
        using var workspace = new Workspace();
        var run = Path.Combine(workspace.Path, "complete");
        var report = await BloodGroveEntryExperiment.RunAsync(ApiRoot, SourceFixture, run, 1, CancellationToken.None);
        Assert.Equal("Complete", report.Status);
        Assert.Equal("Advisory", report.Policy);
        Assert.Equal(432, report.ValidBattles);
        Assert.Equal(432, report.Cells.Count);
        Assert.Equal(216, report.Cells.Count(c => c.FuryClearRateChange is not null));
        Assert.Equal(24, report.Replays.Count);
        Assert.All(report.Replays, r => Assert.True(File.Exists(Path.Combine(run, r))));
        Assert.Equal(2, report.RunArtifactHashes.Count);
        Assert.False(File.Exists(Path.Combine(run, "baseline.json")));
        var before = HarnessJson.FileHash(Path.Combine(run, "summary.md"));
        await Assert.ThrowsAsync<IOException>(() => BloodGroveEntryExperiment.RunAsync(ApiRoot, SourceFixture, run, 1, CancellationToken.None));
        Assert.Equal(before, HarnessJson.FileHash(Path.Combine(run, "summary.md")));
        var saved = SavedSuite.Read(Path.Combine(run, "confirmation"));
        var a = saved.Input.Cells[0];
        var b = saved.Input.Cells[2];
        Assert.Equal(1, BloodGroveEntryExperiment.PairedClearRate(saved, a.Id, b.Id).Pairs);
        var changed = b with { Trials = [b.Trials[0] with { Seed = b.Trials[0].Seed ^ 1 }] };
        var unpaired = saved with { Input = saved.Input with { Cells = saved.Input.Cells.Select(c => c.Id == b.Id ? changed : c).ToArray() } };
        Assert.Throws<InvalidDataException>(() => BloodGroveEntryExperiment.PairedClearRate(unpaired, a.Id, b.Id));
    }

    [Fact]
    public async Task Cancellation_retains_partial_runs_without_a_completed_experiment_report()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var run = Path.Combine(workspace.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => BloodGroveEntryExperiment.RunAsync(ApiRoot, SourceFixture, run, 1,
            cancellation.Token, _ => cancellation.Cancel()));
        Assert.True(File.Exists(Path.Combine(run, "plan.json")));
        Assert.True(File.Exists(Path.Combine(run, "failure.json")));
        Assert.False(File.Exists(Path.Combine(run, "results.json")));
        Assert.False(File.Exists(Path.Combine(run, "summary.md")));
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-entry-tests"));
        public string Path { get; }
        public Workspace()
        {
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_parent, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
