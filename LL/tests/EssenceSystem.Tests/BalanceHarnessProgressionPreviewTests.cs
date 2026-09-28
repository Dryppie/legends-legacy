using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProgressionPreviewTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static string Draft => Path.Combine(Catalogs, TowerProgressionPreview.Fixture);

    [Fact]
    public async Task Preview_prepares_every_floor_and_gear_choice_without_combat_or_an_executable_schedule()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preview must never fight.")).Activate();
        var directory = Path.Combine(Path.GetTempPath(), "tower-progression-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var output = Path.Combine(directory, "preview.json");
            string[] args = ["tower-progression-budget-preview", Draft, Root, Catalogs,
                BalanceHarnessGearProfileTests.Profiles, output];
            Assert.Equal(0, await BalanceHarness.Program.Main(args));
            var report = HarnessJson.Read<TowerProgressionPreviewReport>(output);
            Assert.Equal("PreparedDraftNotBalanceEvidence", report.Status);
            Assert.Equal("Provisional", report.Draft.EquipmentStatus);
            Assert.Equal(0, report.Fights); Assert.Equal(0, report.ReservedSeeds);
            Assert.Equal(new[] { 5, 5, 5, 5, 10, 5, 5, 10, 10, 15, 10 }, report.Floors.Select(r => r.Characters));
            Assert.Equal(HarnessJson.FileHash(Draft), report.SourceHashes["draft"]);
            foreach (var row in report.Floors)
            {
                Assert.Equal(7, row.Parties.Count);
                Assert.Equal(row.Characters * 7, row.Items);
                // Seven items include a two-handed weapon occupying two of the eight slots.
                Assert.Equal(row.Characters * 8, row.OccupiedEquipmentSlots);
                Assert.All(row.Parties, p => {
                    Assert.Empty(p.Scenario.Seeds);
                    Assert.Equal(row.Budget.PriorityFloor, p.Scenario.FloorNumber);
                    Assert.All(p.Scenario.Party, c => Assert.Equal(row.Budget.EssenceSlots, c.Build.EssenceIds.Count));
                });
            }
            var first = report.Floors[0];
            Assert.Equal(new TowerReinforcementCost(0, 200, 446_000), first.FromRankZero);
            Assert.Equal(new TowerReinforcementCost(1, 0, 0), first.FromRankOne);
            Assert.Equal(new TowerReinforcementCost(1, 2_400, 5_352_000), report.Floors[9].FromRankOne);
            Assert.Equal(new TowerReinforcementCost(0, 3_600, 8_028_000), report.Floors[9].FromRankZero);
            Assert.Equal(new TowerReinforcementCost(1, 4_800, 10_704_000), report.Floors[10].FromRankOne);
            var hash = HarnessJson.FileHash(output);
            Assert.NotEqual(0, await BalanceHarness.Program.Main(args));
            Assert.Equal(hash, HarnessJson.FileHash(output));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("slot-unlock")] [InlineData("tier-unlock")] [InlineData("anchor")]
    [InlineData("missing-floor")] [InlineData("duplicate-floor")] [InlineData("approval-claim")]
    public void Invalid_or_incomplete_budgets_cannot_be_previewed_as_the_progression_draft(string fault)
    {
        var draft = HarnessJson.Read<TowerProgressionDraft>(Draft);
        var budgets = draft.Budgets.ToArray();
        if (fault == "slot-unlock") budgets[10] = budgets[10] with { CharacterLevel = 59 };
        if (fault == "tier-unlock") budgets[0] = budgets[0] with { Tier = 2 };
        if (fault == "anchor") budgets[4] = budgets[4] with { EssenceSlots = 4 };
        if (fault == "missing-floor") budgets = budgets.Skip(1).ToArray();
        if (fault == "duplicate-floor") budgets[1] = budgets[0];
        draft = draft with { Budgets = budgets, EquipmentStatus = fault == "approval-claim" ? "Approved" : draft.EquipmentStatus };
        Assert.Throws<InvalidDataException>(() => TowerProgressionPreview.Validate(draft));
    }
}
