using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossValidationReferenceTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public void Fixed_validation_preserves_all_paired_outcomes_recovery_and_exact_existing_recipes()
    {
        var catalog = Assert.IsType<TowerBossValidationCatalog>(TowerBossValidationReferences.Read(Catalogs));
        Assert.Equal(TowerBossValidationReferences.ManifestSha256, catalog.Provenance.ManifestSha256);
        Assert.Equal(100, catalog.Seeds.Count);
        Assert.Equal(18687, catalog.ExcludedCombatSeeds.Count);
        Assert.Equal(new[] { "anchor", "previous-primary", "previous-exploratory" }, catalog.Evidence.Select(e => e.Role));
        Assert.Equal(new[] { 0, 5, 8 }, catalog.Evidence.Select(e => e.Wins));
        var references = HarnessJson.Read<TowerBossReferenceCatalog>(Path.Combine(Catalogs, TowerBossReferences.RefinementFileName));
        Assert.Equal(130, references.Entries.Sum(e => e.Controls.Count));
        var nhalia = references.Entries.Single(e => e.Floor == 13);
        Assert.Equal(catalog.Evidence[1].Id, nhalia.AnchorId);
        Assert.Equal(1, nhalia.Evidence.Single(e => e.Id == nhalia.AnchorId).Confirmation.Cells.First(e => e.Floor == 13).Clears.Count(c => c));
        foreach (var row in catalog.Evidence)
        {
            var earlier = nhalia.Evidence.Single(e => e.Id == row.Id);
            Assert.Equal(40, earlier.TargetRecipe.Seeds.Count);
            Assert.Equal(30, earlier.Confirmation.Cells.Count);
            Assert.Equal(earlier.TargetRecipeHash, HarnessJson.Hash(row.TargetRecipe with { Seeds = earlier.TargetRecipe.Seeds }));
            Assert.Equal(row.TargetRecipeHash, HarnessJson.Hash(row.TargetRecipe));
            Assert.Equal(catalog.FixedPartyFingerprint, TowerBossReferences.FixedPartyFingerprint(row.TargetRecipe));
            Assert.Equal(catalog.Seeds, row.TargetRecipe.Seeds);
            Assert.Equal(catalog.Seeds, row.Outcomes.Select(outcome => outcome.Seed));
            Assert.Empty(catalog.Seeds.Intersect(earlier.TargetRecipe.Seeds));
            Assert.All(row.Outcomes, outcome => Assert.Equal(outcome.Win, outcome.Outcome == "Victory"));
            var summary = TowerBossValidationReferences.Summary(row);
            Assert.Equal(100, summary.Samples);
            Assert.Equal(row.Wins, summary.Wins);
            Assert.Equal(100 - row.Wins, summary.Defeats);
            Assert.Equal(0, summary.Draws);
            Assert.Equal(row.Wins / 100d, summary.Wilson95.Rate);
        }
        var anchor = TowerBossValidationReferences.Summary(catalog.Evidence[0]);
        Assert.Null(anchor.MeanVictoryDurationSeconds);
        Assert.Equal(34.9883, anchor.MeanGuardianHealthPercent, 4);
        Assert.Equal(6225.05, anchor.MeanRecovery.FriendlyReportedHealing, 2);
        Assert.Equal(8081.04, anchor.MeanRecovery.FriendlyEffectiveRegeneration, 2);
        Assert.Equal(2884.83, anchor.MeanRecovery.GuardianReportedHealing, 2);
        Assert.Equal(11.46, anchor.MeanRecovery.GuardianEffectiveRegeneration, 2);
    }

    [Fact]
    public void Paired_comparisons_preserve_disjoint_victories_and_do_not_pool_historical_or_replay_trials()
    {
        var catalog = TowerBossValidationReferences.Read(Catalogs)!;
        var pairs = TowerBossValidationReferences.PairedComparisons(catalog);
        Assert.Equal(new[] { (5, 0), (8, 0), (8, 5) }, pairs.Select(pair => (pair.Gained, pair.Lost)));
        Assert.All(pairs, pair => {
            Assert.Equal(100, pair.Samples);
            Assert.Equal(0, pair.BothWin);
            Assert.Equal(100, pair.Gained + pair.Lost + pair.BothWin + pair.BothDoNotWin);
        });
        var changed = catalog with { Evidence = catalog.Evidence.Select((e, i) => i == 0 ? e : e with {
            Outcomes = e.Outcomes.Reverse().ToArray()
        }).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerBossValidationReferences.PairedComparisons(changed));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(15)]
    public void Every_new_boss_plan_excludes_fixed_validation_seeds_even_without_a_boss_reference_set(int floor)
    {
        var validation = TowerBossValidationReferences.Read(Catalogs)!;
        var definition = TowerBossSearch.Definition(Root, Catalogs, floor, 7, 961130 + floor, "any", "coverage");
        Assert.Equal(TowerBossValidationReferences.FixtureSha256, definition.ValidationReferenceHash);
        Assert.Empty(validation.ExcludedCombatSeeds.Except(definition.ExcludedCombatSeeds));
        Assert.Empty(validation.Seeds.Intersect(TowerBossSearch.CombatSeeds(definition)));
        if (floor != 13) Assert.Null(definition.Refinement!.ReferenceSetId);
    }

    [Fact]
    public void Legacy_definitions_and_catalogs_without_validation_keep_their_original_meaning()
    {
        using var temp = new Temp();
        Assert.Null(TowerBossValidationReferences.Read(temp.Path));
        Assert.Null(TowerBossValidationReferences.Load(Root, temp.Path));
        var legacy = TowerBossSearch.Definition(Root, Catalogs, 13, 7, 961150, "any", "coverage", refinement: false);
        Assert.Equal(1, legacy.SchemaVersion);
        Assert.Null(legacy.ValidationReferenceHash);
        Assert.DoesNotContain("validationReferenceHash", JsonSerializer.Serialize(legacy, HarnessJson.Options));
        Assert.Empty(TowerBossValidationReferences.Read(Catalogs)!.Seeds.Intersect(legacy.ExcludedCombatSeeds));
    }

    [Fact]
    public void Pinned_import_rejects_rewritten_observations_even_when_summary_is_also_rewritten()
    {
        using var temp = new Temp();
        var original = TowerBossValidationReferences.Read(Catalogs)!;
        var changed = original with { Evidence = original.Evidence.Select((e, i) => i != 0 ? e : e with {
            Wins = 100, Defeats = 0, Outcomes = e.Outcomes.Select(row => row with { Win = true, Outcome = "Victory" }).ToArray()
        }).ToArray() };
        HarnessJson.WriteNew(Path.Combine(temp.Path, TowerBossValidationReferences.FileName), changed);
        var error = Assert.Throws<InvalidDataException>(() => TowerBossValidationReferences.Read(temp.Path));
        Assert.Contains("pinned sealed import", error.Message);
    }

    [Fact]
    public void Historical_status_uses_explicit_archived_execution_without_promoting_observations_to_current_guarantees()
    {
        var catalog = TowerBossValidationReferences.Read(Catalogs)!;
        var loaded = TowerBossValidationReferences.Load(Root, Catalogs, catalog.Provenance.Scope.Settings, catalog.Provenance.Scope.Execution)!;
        // An explicit archived execution only controls execution comparison. Later
        // content tuning must still be reported against the immutable catalog hashes.
        var currentHashes = catalog.Provenance.Scope.ContentHashes.ToDictionary(p => p.Key,
            p => HarnessJson.FileHash(Path.Combine(Root, "Data", p.Key)));
        var contentMatches = HarnessJson.Hash(currentHashes) == HarnessJson.Hash(catalog.Provenance.Scope.ContentHashes);
        Assert.Equal(contentMatches, loaded.ContentMatchesCurrent);
        if (!contentMatches) Assert.Contains("historical wins are not current evidence", loaded.EvidenceStatus);
        Assert.True(loaded.SettingsMatchCurrent);
        Assert.True(loaded.ExecutionMatchesCurrent);
        Assert.Contains("Historical fixed validation", loaded.EvidenceStatus);
        Assert.Contains("floor 13 only", loaded.EvidenceStatus);
        Assert.Contains("anchors remain separate and unchanged", loaded.EvidenceStatus);
        var changedExecution = catalog.Provenance.Scope.Execution with { Runtime = "different-runtime" };
        var changed = TowerBossValidationReferences.Load(Root, Catalogs, catalog.Provenance.Scope.Settings, changedExecution)!;
        Assert.False(changed.ExecutionMatchesCurrent);
        Assert.Contains("historical wins require fresh confirmation", changed.EvidenceStatus);
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-boss-validation-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
