using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLoadoutFoundationTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerLoadoutDefinition Definition => TowerLoadoutFoundation.Default(Root, Catalogs);
    private static TowerSettings Settings => new(new(), 10);

    [Fact]
    public void Default_covers_every_floor_and_slot_count_with_unchanged_character_and_equipment_identities()
    {
        var definition = Definition;
        TowerLoadoutFoundation.ValidateDefinition(definition);
        Assert.Equal(165, definition.Contexts.Count);
        Assert.Equal(Enumerable.Range(1, 15), definition.Contexts.Select(c => c.Scenario.FloorNumber).Distinct().Order());
        var content = new OfflineContent(Root, Settings.Threat);
        var runner = new TowerBattleRunner(Root, content);
        foreach (var context in definition.Contexts)
        {
            var candidates = TowerLoadoutFoundation.Generate(context, content, runner, Settings, "scope", 17, 4);
            foreach (var candidate in candidates.Where(c => c.Rejection is null))
            {
                var original = context.Scenario;
                var altered = candidate.Scenario!;
                Assert.Equal(original.Id, altered.Id);
                Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(altered with { Party = altered.Party.Select(p =>
                    p.PartySlot == context.TargetPartySlot ? p with { Build = p.Build with { EssenceIds = original.Party.Single(q => q.PartySlot == p.PartySlot).Build.EssenceIds } } : p).ToArray() }));
                var before = runner.CreateInput(original, original.Seeds[0], Settings.Threat, 10);
                var after = runner.CreateInput(altered, altered.Seeds[0], Settings.Threat, 10);
                Assert.Equal(before.Party.Select(p => p.Character.Id), after.Party.Select(p => p.Character.Id));
                Assert.Equal(HarnessJson.Hash(before.Party.Select(p => p.Character.Equipment)), HarnessJson.Hash(after.Party.Select(p => p.Character.Equipment)));
            }
        }
        foreach (var group in definition.Contexts.Where(c => c.Id.StartsWith("controlled-", StringComparison.Ordinal)).GroupBy(c => c.Scenario.FloorNumber))
            Assert.Equal(Enumerable.Range(4, 7), group.Select(c => c.Scenario.Party[0].Build.EssenceIds.Count).Order());
    }

    [Fact]
    public void Opt_in_identity_preserves_the_historical_reference_without_changing_default_serialization()
    {
        var recipe = Definition.Contexts[0].Scenario.Party[0].Build with { IdentityEssenceIds = null };
        var legacy = JsonSerializer.Serialize(new { recipe.Id, recipe.CharacterLevel, recipe.Tier, recipe.Rank,
            recipe.Equipment, recipe.EssenceIds, recipe.Quality, recipe.AttributeRollMultiplier });
        Assert.Equal(legacy, JsonSerializer.Serialize(recipe));
        var content = new OfflineContent(Root, Settings.Threat);
        var original = content.CreateBuild(recipe);
        var pinned = content.CreateBuild(recipe with { IdentityEssenceIds = recipe.EssenceIds });
        Assert.Equal(HarnessJson.Hash(FixtureCharacter.From(original)), HarnessJson.Hash(FixtureCharacter.From(pinned)));
        var reversed = content.CreateBuild(recipe with { IdentityEssenceIds = recipe.EssenceIds, EssenceIds = recipe.EssenceIds.Reverse().ToArray() });
        Assert.Equal(original.Character.Id, reversed.Character.Id);
        Assert.Equal(original.Equipment.Select(e => e.Id), reversed.Equipment.Select(e => e.Id));
        Assert.Equal(original.EquippedEssences.Select(e => e.Id), reversed.EquippedEssences.Select(e => e.Id));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(recipe with { IdentityEssenceIds = [] }));
    }

    [Fact]
    public void Ordered_candidates_have_scoped_identity_and_record_pins_and_family_rejections()
    {
        var context = Definition.Contexts[0];
        var content = new OfflineContent(Root, Settings.Threat);
        var runner = new TowerBattleRunner(Root, content);
        var first = TowerLoadoutFoundation.Generate(context, content, runner, Settings, "content-v1", 17, 20);
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(TowerLoadoutFoundation.Generate(context, content, runner, Settings, "content-v1", 17, 20)));
        Assert.NotEqual(first[0].Id, first[1].Id);
        Assert.Equal(first[0].Essences.Reverse(), first[1].Essences);
        Assert.NotEqual(first[0].Id, TowerLoadoutFoundation.Generate(context, content, runner, Settings, "content-v2", 17, 20)[0].Id);
        var original = context.Scenario.Party[0].Build.EssenceIds;
        var pinned = context with { AllowedEssences = original, PinnedSlots = new Dictionary<int, string> { [0] = original[0] } };
        var proposals = TowerLoadoutFoundation.Generate(pinned, content, runner, Settings, "content-v1", 17, 100);
        Assert.Contains(proposals, c => c.Rejection?.Contains("pinned", StringComparison.Ordinal) == true);
        Assert.Contains(proposals, c => c.Rejection?.Contains("families", StringComparison.Ordinal) == true);
        Assert.All(proposals.Where(c => c.Rejection is null), c => Assert.Equal(original[0], c.Essences[0]));
        Assert.All(proposals.Where(c => c.Rejection is not null), c => Assert.Null(c.Scenario));
    }

    [Fact]
    public void Invalid_budgets_pools_pins_and_unsupported_contract_fields_fail_explicitly()
    {
        var definition = Definition;
        var context = definition.Contexts[0];
        var content = new OfflineContent(Root, Settings.Threat);
        var runner = new TowerBattleRunner(Root, content);
        void Invalid(TowerLoadoutContext c) => Assert.ThrowsAny<Exception>(() => TowerLoadoutFoundation.Generate(c, content, runner, Settings, "scope", 17, 4));
        Invalid(context with { AllowedEssences = ["unknown"] });
        Invalid(context with { AllowedEssences = [.. context.AllowedEssences, context.AllowedEssences[0].ToUpperInvariant()] });
        Invalid(context with { AllowedEssences = context.AllowedEssences.Skip(1).Where(e => !context.Scenario.Party[0].Build.EssenceIds.Contains(e)).ToArray() });
        Invalid(context with { TargetPartySlot = 999 });
        Invalid(context with { PinnedSlots = new Dictionary<int, string> { [10] = context.AllowedEssences[0] } });
        Invalid(context with { OwnershipAssumption = "" });
        Invalid(context with { Scenario = context.Scenario with { Party = context.Scenario.Party.Select(p => p with { Build = p.Build with { CharacterLevel = 1 } }).ToArray() } });
        var variantGroup = content.Essences.GetAll().GroupBy(e => e.SourceMonsterId).First(g => g.Count() > 1);
        var variants = variantGroup.Take(2).Select(e => e.Id).ToArray();
        var others = content.Essences.GetAll().Where(e => e.SourceMonsterId != variantGroup.Key).GroupBy(e => e.SourceMonsterId).Take(2).Select(g => g.First().Id);
        Invalid(context with { Scenario = context.Scenario with { Party = context.Scenario.Party.Select(p => p.PartySlot == context.TargetPartySlot
            ? p with { Build = p.Build with { EssenceIds = [.. variants, .. others] } } : p).ToArray() } });
        Assert.Throws<InvalidDataException>(() => TowerLoadoutFoundation.ValidateDefinition(definition with { Progression = "level-10" }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutFoundation.ValidateDefinition(definition with { OrderPolicy = "unordered" }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutFoundation.ValidateDefinition(definition with { AuditSeeds = [1, 1] }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutFoundation.ValidateDefinition(definition with { MaxProposalsPerContext = 100 }));
        using var temp = new Temp();
        var json = JsonSerializer.SerializeToNode(definition, HarnessJson.Options)!;
        json["essenceLevel"] = 99;
        File.WriteAllText(Path.Combine(temp.Path, "invalid.json"), json.ToJsonString());
        Assert.Throws<JsonException>(() => TowerLoadoutFoundation.ReadDefinition(Path.Combine(temp.Path, "invalid.json")));
    }

    [Fact]
    public async Task Frozen_foundation_exports_replayable_order_probes_and_structured_mechanics()
    {
        using var temp = new Temp();
        var full = Definition;
        var definition = full with { Contexts = [full.Contexts[0]], AuditContexts = [full.Contexts[0].Id], AuditSeeds = [1337, 17] };
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerLoadoutFoundation.CreateAsync(Root, definition, output);
        Assert.Equal("Complete", report.Status); Assert.Equal(2, report.OrderAudit.Count);
        var mechanics = HarnessJson.Read<EssenceMechanicsReport>(Path.Combine(output, "mechanics.json"));
        Assert.Equal(new OfflineContent(Root, Settings.Threat).Essences.GetAll().Count, mechanics.Essences.Count);
        Assert.All(mechanics.Essences, e => { Assert.Equal(2, e.Abilities.Count); Assert.NotEmpty(e.Coverage); });
        Assert.Contains("operation:Damage", mechanics.Essences.Single(e => e.Id == "essence.dire_wolf").Signals);
        Assert.NotEmpty(mechanics.Statuses.EnumerateArray()); Assert.NotEmpty(mechanics.Summons.EnumerateArray());
        var auditRoot = Path.Combine(output, "order-audit", HarnessJson.Hash(full.Contexts[0].Id));
        foreach (var orientation in new[] { "original", "reversed" })
        {
            var saved = TowerBundle.ReadSaved(Path.Combine(auditRoot, orientation));
            Assert.Equal("Complete", saved.Scorecard.Status);
            Assert.NotEmpty((await TowerBundle.ReplayAsync(Path.Combine(auditRoot, orientation), "tower.0001", true)).Battle.EventLog!);
        }
        foreach (var candidate in report.Contexts[0].Candidates.Where(c => c.Rejection is null))
            Assert.Equal(HarnessJson.Hash(candidate.Scenario), HarnessJson.Hash(HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", candidate.Id + ".json"))));
        var hashes = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "foundation-files.json"));
        Assert.All(hashes, p => Assert.Equal(p.Value, HarnessJson.FileHash(Path.Combine(output, p.Key))));
        await Assert.ThrowsAsync<IOException>(() => TowerLoadoutFoundation.CreateAsync(Root, definition, output));
        File.AppendAllText(Path.Combine(output, "content/Data/combat/abilities.json"), " ");
        Assert.NotEqual(mechanics.SourceHashes["combat/abilities.json"], EssenceMechanicsInventory.Create(Path.Combine(output, "content"), Settings.Threat).SourceHashes["combat/abilities.json"]);
    }

    [Fact]
    public async Task Cancellation_preserves_prepared_contexts_without_claiming_complete_audit()
    {
        using var temp = new Temp();
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(temp.Path, "cancelled");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerLoadoutFoundation.CreateAsync(Root, Definition, output,
            cancellation.Token, _ => cancellation.Cancel()));
        var report = HarnessJson.Read<TowerLoadoutFoundationReport>(Path.Combine(output, "foundation.json"));
        Assert.Equal("Cancelled", report.Status); Assert.Single(report.Contexts); Assert.Empty(report.OrderAudit);
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(output, "recipes")));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-foundation-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
