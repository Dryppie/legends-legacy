using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossReferenceTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Theory]
    [InlineData(7, 5, 25, "e568dfaabbb8", 2)]
    [InlineData(8, 4, 35, "b4b8971c6c8f", 20)]
    [InlineData(13, 7, 36, "a4eaec8b4a9a", 0)]
    public void Portable_catalog_retains_every_finalist_and_original_observations(int floor, int slots, int count, string anchor, int wins)
    {
        using var temp = new Temp();
        CopyCatalogs(temp.Path);
        var reference = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(Root, temp.Path, floor, slots));
        Assert.Equal(count, reference.Controls.Count);
        Assert.Equal("control", reference.Controls[0].Source);
        Assert.Equal(reference.Controls.Select(c => c.Id), reference.Evidence.Select(c => c.Id));
        Assert.StartsWith(anchor, reference.AnchorId);
        Assert.Contains("Historical pilot", reference.AnchorLabel);
        Assert.Contains("Historical pilot observations", reference.EvidenceStatus);
        Assert.Equal(TowerBossReferences.PilotManifest, reference.Provenance.ManifestSha256);
        Assert.Equal(15512, reference.ExcludedCombatSeeds.Count);
        Assert.Equal(TowerPartyProgression.Budget(slots) with { PriorityFloor = floor }, reference.Budget);
        var frozenAnchor = reference.Evidence.Single(e => e.Id == reference.AnchorId);
        Assert.Contains("focused-progress", frozenAnchor.StrategyLabels);
        Assert.All(frozenAnchor.Confirmation.Cells.Where(c => c.Floor == floor), c => Assert.Equal(wins, c.Clears.Count(x => x)));
        Assert.All(reference.Evidence, e => {
            Assert.Equal(30, e.Confirmation.Cells.Count);
            Assert.Equal(Enumerable.Range(1, 15), e.Confirmation.Cells.Select(c => c.Floor).Distinct().Order());
            Assert.All(e.TargetRecipe.Seeds, seed => Assert.Contains(seed, reference.ExcludedCombatSeeds));
            Assert.Equal(e.TargetRecipeHash, HarnessJson.Hash(e.TargetRecipe));
            Assert.Equal(reference.FixedPartyFingerprint, TowerBossReferences.FixedPartyFingerprint(e.TargetRecipe));
            Assert.Equal(reference.MutablePartySlots, e.TargetRecipe.Party.Select(p => p.PartySlot));
            Assert.NotEmpty(e.DiscoveryOrigins);
        });
    }

    [Theory]
    [InlineData(7, 5, 42, "68bcf1597a0c", 40, 25)]
    [InlineData(13, 7, 53, "7f48c238712a", 1, 36)]
    public void New_catalog_preserves_refinement_and_pilot_as_separate_complete_experiments(
        int floor, int slots, int count, string anchor, int wins, int priorCount)
    {
        var old = HarnessJson.Read<TowerBossReferenceCatalog>(Path.Combine(Catalogs, TowerBossReferences.FileName));
        var pilot = old.Entries.Single(e => e.Floor == floor);
        var reference = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(Root, Catalogs, floor, slots));
        Assert.Equal(count, reference.Controls.Count);
        Assert.StartsWith(anchor, reference.AnchorId);
        Assert.Equal(TowerBossReferences.RefinementManifest, reference.Provenance.ManifestSha256);
        Assert.Equal(TowerBossReferences.PilotManifest, Assert.Single(reference.PriorProvenance!).ManifestSha256);
        Assert.Equal(16760, reference.ExcludedCombatSeeds.Count);
        Assert.Empty(old.ExcludedCombatSeeds.Except(reference.ExcludedCombatSeeds));
        Assert.Contains("Historical refinement observations (40 samples", reference.EvidenceStatus);
        Assert.Contains("earlier pilot observations (20 samples) remain separate", reference.EvidenceStatus);
        Assert.Equal(reference.FixedPartyFingerprint, pilot.FixedPartyFingerprint);
        Assert.Equal(reference.Budget, pilot.Budget);
        Assert.Equal(2, reference.Contexts!.Count);
        Assert.All(reference.Contexts.Values, contexts => Assert.Equal(Enumerable.Range(1, 15), contexts.Select(s => s.FloorNumber).Order()));
        Assert.Equal(reference.ContextsHash, HarnessJson.Hash(reference.Contexts));
        Assert.Contains(reference.ContextsSourcePath!, reference.Provenance.SourceHashes.Keys);
        Assert.Equal(priorCount, reference.Evidence.Count(e => e.PriorObservations is not null));
        Assert.Equal(reference.AnchorId, Assert.Single(reference.Evidence, e => e.WasDiscoveryPrimary is true).Id);
        Assert.All(reference.Evidence, row => {
            Assert.Equal(40, row.TargetRecipe.Seeds.Count);
            Assert.Equal(30, row.Confirmation.Cells.Count);
            Assert.All(row.Confirmation.Cells, cell => Assert.Equal(40, cell.Clears.Count));
            Assert.Equal(row.TargetRecipeHash, HarnessJson.Hash(row.TargetRecipe));
            Assert.Equal(reference.FixedPartyFingerprint, TowerBossReferences.FixedPartyFingerprint(row.TargetRecipe));
            Assert.All(reference.Contexts.Values, contexts => Assert.Equal(row.TargetRecipeHash, HarnessJson.Hash(
                TowerPartySelection.Apply(contexts.Single(s => s.FloorNumber == floor),
                    reference.Controls.Single(c => c.Id == row.Id).Builds, row.TargetRecipe.Seeds))));
            if (row.PriorObservations is null) return;
            var prior = Assert.Single(row.PriorObservations);
            Assert.Equal(TowerBossReferences.PilotPackage, prior.PackageId);
            // Full original record equality protects labels, origins, recipes and negative transfer cells as well as wins.
            Assert.Equal(HarnessJson.Hash(pilot.Evidence.Single(e => e.Id == row.Id)), HarnessJson.Hash(prior.Evidence));
            Assert.Equal(20, prior.Evidence.TargetRecipe.Seeds.Count);
            Assert.Empty(prior.Evidence.TargetRecipe.Seeds.Intersect(row.TargetRecipe.Seeds));
        });
        Assert.All(reference.Evidence.Single(e => e.Id == reference.AnchorId).Confirmation.Cells.Where(c => c.Floor == floor),
            cell => Assert.Equal(wins, cell.Clears.Count(c => c)));
    }

    [Fact]
    public void Kodoku_keeps_pilot_scope_and_evidence_in_the_mixed_catalog()
    {
        var old = HarnessJson.Read<TowerBossReferenceCatalog>(Path.Combine(Catalogs, TowerBossReferences.FileName));
        var pilot = old.Entries.Single(e => e.Floor == 8);
        var reference = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(Root, Catalogs, 8, 4,
            old.Provenance.Scope.Settings, old.Provenance.Scope.Execution));
        Assert.Equal(35, reference.Controls.Count);
        Assert.Equal(HarnessJson.Hash(pilot.Evidence), HarnessJson.Hash(reference.Evidence));
        Assert.Equal(HarnessJson.Hash(old.Provenance), HarnessJson.Hash(reference.Provenance));
        Assert.True(reference.ExecutionMatchesCurrent);
        Assert.True(reference.SettingsMatchCurrent);
        Assert.Contains("Historical pilot observations", reference.EvidenceStatus);
        Assert.Null(reference.PriorProvenance);
    }

    [Fact]
    public void Exploratory_confirmation_maximum_does_not_replace_the_frozen_primary_and_transfer_losses_remain()
    {
        var nhalia = TowerBossReferences.Load(Root, Catalogs, 13, 7)!;
        var exploratory = nhalia.Evidence.Single(e => e.Id == "813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1");
        Assert.False(exploratory.WasDiscoveryPrimary);
        Assert.NotEqual(exploratory.Id, nhalia.AnchorId);
        Assert.All(exploratory.Confirmation.Cells.Where(c => c.Floor == 13), cell => Assert.Equal(6, cell.Clears.Count(c => c)));
        var eydis = TowerBossReferences.Load(Root, Catalogs, 7, 5)!;
        var primary = eydis.Evidence.Single(e => e.Id == eydis.AnchorId);
        var retained = eydis.Evidence.Single(e => e.Id.StartsWith("126553034bac", StringComparison.Ordinal));
        Assert.All(primary.Confirmation.Cells.Where(c => c.Floor == 11), cell => Assert.Equal(0, cell.Clears.Count(c => c)));
        Assert.Equal(36, retained.Confirmation.Cells.Single(c => c.Floor == 11 && c.Context.EndsWith("previous-05", StringComparison.Ordinal)).Clears.Count(c => c));
    }

    [Theory]
    [InlineData("missing-pilot")]
    [InlineData("changed-prior-recipe")]
    [InlineData("reselected-anchor")]
    [InlineData("wrong-package")]
    [InlineData("changed-transfer-ally")]
    public void New_catalog_rejects_lost_history_changed_prior_inputs_and_confirmation_reselection(string mutation)
    {
        var original = HarnessJson.Read<TowerBossReferenceCatalog>(Path.Combine(Catalogs, TowerBossReferences.RefinementFileName));
        var nhalia = original.Entries.Single(e => e.Floor == 13);
        var row = nhalia.Evidence.First(e => e.PriorObservations is not null);
        var prior = row.PriorObservations![0];
        var changed = mutation switch {
            "reselected-anchor" => nhalia with { AnchorId = "813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1" },
            "wrong-package" => nhalia with { PackageId = TowerBossReferences.PilotPackage },
            "changed-transfer-ally" => nhalia with { Contexts = nhalia.Contexts!.ToDictionary(context => context.Key,
                context => (IReadOnlyList<TowerScenario>)context.Value.Select(s => s.FloorNumber != 15 ? s : s with {
                    Party = s.Party.Select((p, i) => i == s.Party.Count - 1 ? p with { Build = p.Build with { CharacterLevel = p.Build.CharacterLevel + 1 } } : p).ToArray()
                }).ToArray()) },
            _ => nhalia with { Evidence = nhalia.Evidence.Select(e => e.Id != row.Id ? e : mutation == "missing-pilot"
                ? e with { PriorObservations = null }
                : e with { PriorObservations = [prior with { Evidence = prior.Evidence with {
                    TargetRecipe = prior.Evidence.TargetRecipe with { Seeds = e.TargetRecipe.Seeds.Take(20).ToArray() }
                } }] }).ToArray() }
        };
        if (mutation == "changed-transfer-ally") changed = changed with { ContextsHash = HarnessJson.Hash(changed.Contexts) };
        using var temp = new Temp();
        File.Copy(Path.Combine(Catalogs, "tower-curve.json"), Path.Combine(temp.Path, "tower-curve.json"));
        // A new catalog frozen under the original name is still schema2; filenames do not select schema meaning.
        HarnessJson.WriteNew(Path.Combine(temp.Path, TowerBossReferences.FileName), original with {
            Entries = original.Entries.Select(e => e.Floor == 13 ? changed : e).ToArray()
        });
        Assert.Throws<InvalidDataException>(() => TowerBossReferences.Load(Root, temp.Path, 13, 7));
    }

    [Fact]
    public void Missing_catalog_or_unrepresented_budget_does_not_invent_reference_evidence()
    {
        using var temp = new Temp();
        Assert.Null(TowerBossReferences.Load(Root, temp.Path, 7, 5));
        Assert.Null(TowerBossReferences.Load(Root, Catalogs, 3, 4));
        Assert.Null(TowerBossReferences.Load(Root, Catalogs, 7, 6));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Changed_fixed_identity_or_gear_is_rejected_before_using_historical_recipes(bool changeGear)
    {
        using var temp = new Temp();
        CopyCatalogs(temp.Path);
        var path = Path.Combine(temp.Path, "tower-curve.json");
        var curve = HarnessJson.Read<TowerBenchmarkDefinition>(path);
        var changed = curve with { Profiles = curve.Profiles.Select(p => p.Id == "slots-5-guardian" ? p with {
            Build = changeGear
                ? p.Build with { Equipment = p.Build.Equipment.Select((item, i) => i == 0
                    ? item with { DefinitionId = "plain.staff.rarity.uncommon" } : item).ToArray() }
                : p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() }
        } : p).ToArray() };
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(changed, HarnessJson.Options));
        var error = Assert.Throws<InvalidDataException>(() => TowerBossReferences.Load(Root, temp.Path, 7, 5));
        Assert.Contains("identities, gear", error.Message);
    }

    [Fact]
    public void Frozen_content_without_appsettings_uses_explicit_settings_and_flags_changed_content_as_historical()
    {
        using var temp = new Temp();
        var catalogs = Path.Combine(temp.Path, "catalogs");
        Directory.CreateDirectory(catalogs);
        CopyCatalogs(catalogs);
        var original = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(Root, catalogs, 8, 4));
        var frozen = Path.Combine(temp.Path, "content");
        foreach (var file in original.Provenance.Scope.ContentHashes.Keys)
        {
            var destination = Path.Combine(frozen, "Data", file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(Root, "Data", file), destination);
        }
        Assert.False(File.Exists(Path.Combine(frozen, "appsettings.json")));
        var matching = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(frozen, catalogs, 8, 4, original.Provenance.Scope.Settings));
        Assert.True(matching.SettingsMatchCurrent);
        // Semantically identical content with a different source hash must still be reported as changed.
        File.AppendAllText(Path.Combine(frozen, "Data", "combat", "abilities.json"), "\n");
        var changed = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(frozen, catalogs, 8, 4, original.Provenance.Scope.Settings));
        Assert.False(changed.ContentMatchesCurrent);
        Assert.Contains("historical wins are not current evidence", changed.EvidenceStatus);
        Assert.Equal(matching.Controls.Select(c => c.Id), changed.Controls.Select(c => c.Id));
        var unknown = Assert.IsType<TowerBossReferenceSet>(TowerBossReferences.Load(frozen, catalogs, 8, 4));
        Assert.Null(unknown.SettingsMatchCurrent);
        Assert.Contains("unavailable", unknown.EvidenceStatus);
    }

    [Fact]
    public void Reference_catalog_rejects_truncated_portfolio_missing_exclusions_and_changed_recipes()
    {
        var original = HarnessJson.Read<TowerBossReferenceCatalog>(Path.Combine(Catalogs, TowerBossReferences.FileName));
        var first = original.Entries[0];
        var observedSeed = first.Evidence[0].TargetRecipe.Seeds[0];
        var recipe = first.Evidence[0].TargetRecipe;
        var changes = new[] {
            original with { Entries = original.Entries.Select(e => e == first ? e with { Controls = e.Controls.SkipLast(1).ToArray() } : e).ToArray() },
            original with { ExcludedCombatSeeds = original.ExcludedCombatSeeds.Where(seed => seed != observedSeed).ToArray() },
            original with { Entries = original.Entries.Select(e => e == first ? e with { Evidence = e.Evidence.Select((r, i) => i == 0
                ? r with { TargetRecipe = recipe with { Party = recipe.Party.Select((p, j) => j == 0
                    ? p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } } : p).ToArray() } } : r).ToArray() } : e).ToArray() }
        };
        foreach (var changed in changes)
        {
            using var temp = new Temp();
            File.Copy(Path.Combine(Catalogs, "tower-curve.json"), Path.Combine(temp.Path, "tower-curve.json"));
            HarnessJson.WriteNew(Path.Combine(temp.Path, TowerBossReferences.FileName), changed);
            Assert.Throws<InvalidDataException>(() => TowerBossReferences.Load(Root, temp.Path, 7, 5));
        }
    }

    private static void CopyCatalogs(string destination)
    {
        foreach (var file in new[] { "tower-curve.json", TowerBossReferences.FileName })
            File.Copy(Path.Combine(Catalogs, file), Path.Combine(destination, file));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-boss-reference-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
