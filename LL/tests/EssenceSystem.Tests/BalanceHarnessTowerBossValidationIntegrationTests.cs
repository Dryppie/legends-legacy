using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossValidationIntegrationTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Theory]
    [InlineData(3, 4)]
    [InlineData(11, 5)]
    [InlineData(13, 7)]
    [InlineData(15, 6)]
    public void Every_new_boss_plan_excludes_fixed_validation_seeds_and_declares_its_frozen_source(int floor, int slots)
    {
        var validation = TowerBossValidationReferences.Read(Catalogs)!;
        var definition = TowerBossSearch.Definition(Root, Catalogs, floor, slots, 821537 + floor, "any", "coverage");
        Assert.Equal(HarnessJson.FileHash(Path.Combine(Catalogs, TowerBossValidationReferences.FileName)), definition.ValidationReferenceHash);
        Assert.Empty(validation.ExcludedCombatSeeds.Except(definition.ExcludedCombatSeeds));
        Assert.Empty(TowerBossSearch.CombatSeeds(definition).Intersect(validation.ExcludedCombatSeeds));
        var historical = TowerBossSearch.Definition(Root, Catalogs, floor, slots, 821557 + floor, "any", "coverage", refinement: false);
        Assert.Null(historical.ValidationReferenceHash);
        Assert.DoesNotContain("validationReferenceHash", JsonSerializer.Serialize(historical, HarnessJson.Options));
    }

    [Theory]
    [InlineData("missing-file")]
    [InlineData("changed-file")]
    [InlineData("omitted-exclusion")]
    public async Task Changed_or_omitted_validation_evidence_is_rejected_before_any_combat(string change)
    {
        var temp = Path.Combine(Path.GetTempPath(), "boss-validation-preflight-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var definition = TowerBossSearch.Definition(Root, Catalogs, 11, 5, 821580, "any", "coverage");
            var catalogs = Path.Combine(temp, "catalogs"); Directory.CreateDirectory(catalogs);
            File.Copy(Path.Combine(Catalogs, "tower-curve.json"), Path.Combine(catalogs, "tower-curve.json"));
            var source = Path.Combine(Catalogs, TowerBossValidationReferences.FileName);
            var frozen = Path.Combine(catalogs, TowerBossValidationReferences.FileName);
            if (change != "missing-file") File.Copy(source, frozen);
            if (change == "changed-file") File.AppendAllText(frozen, " ");
            if (change == "omitted-exclusion")
            {
                var excluded = TowerBossValidationReferences.Read(Catalogs)!.ExcludedCombatSeeds[0];
                definition = definition with { ExcludedCombatSeeds = definition.ExcludedCombatSeeds.Where(s => s != excluded).ToArray() };
            }
            var output = Path.Combine(temp, "run");
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossSearch.RunAsync(Root, catalogs, output, definition));
            Assert.Contains("validation", error.Message, StringComparison.OrdinalIgnoreCase);
            var report = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(output, "boss-search.json"));
            Assert.Equal("Invalid", report.Status);
            Assert.Equal(0, report.ActualBattles);
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(output, "battles")));
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
