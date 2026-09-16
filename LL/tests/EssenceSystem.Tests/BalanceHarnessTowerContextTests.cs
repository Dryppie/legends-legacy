using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerContextTests
{
    private static TowerScenario Scenario => BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with {
        StartsAt = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), Seeds = [] };

    [Theory]
    [InlineData(-12)] [InlineData(-7)] [InlineData(-1)] [InlineData(0)] [InlineData(1)] [InlineData(14)]
    public void Equivalent_instants_share_only_the_new_context_identity(int offset)
    {
        var s = Scenario; var equivalent = s with { StartsAt = s.StartsAt.ToOffset(TimeSpan.FromHours(offset)) };
        Assert.Equal(TowerConfirmationContext.Identity(s), TowerConfirmationContext.Identity(equivalent));
        Assert.Equal(TimeSpan.FromHours(offset), equivalent.StartsAt.Offset);
        Assert.NotEqual(TowerRetainedFamilyAudit.Identity(s).ContextHash, TowerConfirmationContext.Identity(s).ContextHash);
        if (offset != 0) Assert.NotEqual(TowerRetainedFamilyAudit.Identity(s).ContextHash, TowerRetainedFamilyAudit.Identity(equivalent).ContextHash);
    }

    [Theory]
    [InlineData("tick")] [InlineData("local-clock")] [InlineData("scenario")] [InlineData("floor")] [InlineData("preparation")]
    public void Different_contexts_never_collapse(string difference)
    {
        var s = Scenario;
        var changed = difference switch {
            "tick" => s with { StartsAt = s.StartsAt.AddTicks(1) },
            "local-clock" => s with { StartsAt = new DateTimeOffset(s.StartsAt.DateTime, TimeSpan.FromHours(1)) },
            "scenario" => s with { Id = "different-scenario" }, "floor" => s with { FloorNumber = 2 },
            _ => s with { PreparationState = "different-preparation" } };
        Assert.NotEqual(TowerConfirmationContext.Identity(s).ContextHash, TowerConfirmationContext.Identity(changed).ContextHash);
    }

    [Fact]
    public void Character_and_ordered_essence_identity_are_preserved()
    {
        var s = Scenario; var before = TowerConfirmationContext.Identity(s);
        var party = s.Party.ToArray(); party[0] = party[0] with { Build = party[0].Build with { Id = "different-character" } };
        Assert.NotEqual(before.CellHash, TowerConfirmationContext.Identity(s with { Party = party }).CellHash);
        party = s.Party.ToArray(); party[0] = party[0] with { Build = party[0].Build with { EssenceIds = party[0].Build.EssenceIds.Reverse().ToArray() } };
        Assert.NotEqual(before.CellHash, TowerConfirmationContext.Identity(s with { Party = party }).CellHash);
        Assert.Equal(TowerRetainedFamilyAudit.Identity(s).RecipeHash, before.RecipeHash);
    }

    private static TowerConfirmationCell Cell(int i, string context = "context", params string[] anchors)
    {
        var c = HarnessJson.Hash(context); var r = HarnessJson.Hash(i);
        return new(HarnessJson.Hash(new { i, kind = "inventory" }), HarnessJson.Hash(new { i, kind = "legacy" }),
            c, r, HarnessJson.Hash(new { Context = c, Recipe = r }), HarnessJson.Hash("participants"), anchors);
    }

    [Fact]
    public void Unified_context_keeps_every_recipe_and_every_overlapping_anchor_origin()
    {
        var cells = new[] { Cell(1, anchors: ["historical-1", "midpoint-1"]), Cell(2), Cell(3, anchors: ["midpoint-2"]) };
        var summary = TowerConfirmationContext.Validate(cells, 3, ["historical-1", "midpoint-1", "midpoint-2"]);
        Assert.Equal(3, summary.Entries); Assert.Equal(1, summary.Contexts); Assert.Equal(2, summary.Anchors); Assert.Equal(1, summary.FirstStageCells);
    }

    [Theory]
    [InlineData("missing-entry")] [InlineData("duplicate-key")] [InlineData("duplicate-legacy")]
    [InlineData("collision")] [InlineData("bad-cell-hash")] [InlineData("bad-participants")]
    [InlineData("missing-anchor")] [InlineData("duplicate-anchor")] [InlineData("extra-anchor")]
    [InlineData("uncovered-context")]
    public void Incomplete_ambiguous_or_corrupt_families_cannot_bind(string defect)
    {
        var cells = new[] { Cell(1, anchors: ["anchor"]), Cell(2) };
        cells[1] = defect switch {
            "duplicate-key" => cells[1] with { InventoryKey = cells[0].InventoryKey },
            "duplicate-legacy" => cells[1] with { LegacyCellHash = cells[0].LegacyCellHash },
            "collision" => cells[0] with { InventoryKey = cells[1].InventoryKey, LegacyCellHash = cells[1].LegacyCellHash, AnchorReasons = [] },
            "bad-cell-hash" => cells[1] with { CellHash = HarnessJson.Hash("bad") },
            "bad-participants" => cells[1] with { ParticipantsHash = "not-a-hash" },
            "duplicate-anchor" => cells[1] with { AnchorReasons = ["anchor"] },
            "extra-anchor" => cells[1] with { AnchorReasons = ["undeclared"] },
            "uncovered-context" => Cell(2, "different-context"), _ => cells[1] };
        Assert.Throws<InvalidDataException>(() => TowerConfirmationContext.Validate(cells,
            defect == "missing-entry" ? 3 : 2, defect == "missing-anchor" ? ["anchor", "missing"] : ["anchor"]));
    }

    [Fact]
    public void Cancellation_stops_validation()
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerConfirmationContext.Validate([Cell(1, anchors: ["anchor"])], 1, ["anchor"], stop.Token));
    }
}
