using BalanceHarness;
using Domain.Models.Attributes;

namespace EssenceSystem.Tests;

public sealed class AttributeAllocationStudyTests
{
    [Fact]
    public async Task Ability_profile_is_frozen_and_selected_independently_of_equipment_release()
    {
        var root = TestContentPaths.FindApiRoot();
        var source = HarnessJson.Read<AttributeAllocationRequest>(Path.Combine(root, "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "attribute-allocation-matched.json"));
        var output = Path.Combine(Path.GetTempPath(), "ll-healing-comparison-" + Guid.NewGuid().ToString("N"));
        var cell = source.Cells[0] with { ReferenceDiagnosticPoints = null, CandidateDiagnosticPoints = null, ComparisonGroup = null };
        var healer = cell.Reference with { EssenceIds = ["essence.lizardfolk_shaman", "essence.treant_sapling"] };
        cell = cell with { Reference = healer, Candidate = healer };
        var request = source with { ContentRoot = root, OutputDirectory = output, Cells = [cell], ExplorationSeeds = [1], ConfirmationSeeds = [2], MaximumBattles = 8,
            ReferenceEquipmentBalanceVersion = 3, CandidateEquipmentBalanceVersion = 3, CandidateAbilityBalanceProfile = "healing-v1" };
        try
        {
            Assert.Equal(0, await AttributeAllocationStudy.RunAsync(request, default));
            var frozenRoot = Path.Combine(output, "content");
            var frozen = Path.Combine(frozenRoot, "Data", "combat", "ability-balance.healing-v1.json");
            Assert.Equal(HarnessJson.FileHash(Path.Combine(root, "Data", "combat", "ability-balance.healing-v1.json")), HarnessJson.FileHash(frozen));
            Assert.Contains("healing-v1", File.ReadAllText(Path.Combine(output, "manifest.json")));
            var trials = HarnessJson.Read<AttributeAllocationTrial[]>(Path.Combine(output, "trials.json"));
            Assert.Contains(trials, trial => trial.Reference.Statistics.Sum(x => x.HealingDone) != trial.Candidate.Statistics.Sum(x => x.HealingDone));
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public async Task Equipment_release_comparison_freezes_both_catalogs_and_uses_their_prices()
    {
        var root = TestContentPaths.FindApiRoot();
        var source = HarnessJson.Read<AttributeAllocationRequest>(Path.Combine(root, "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "attribute-allocation-matched.json"));
        var output = Path.Combine(Path.GetTempPath(), "ll-equipment-comparison-" + Guid.NewGuid().ToString("N"));
        var cell = source.Cells[0] with { ReferenceDiagnosticPoints = null, CandidateDiagnosticPoints = null, ComparisonGroup = null };
        var request = source with { ContentRoot = root, OutputDirectory = output, Cells = [cell], ExplorationSeeds = [1], ConfirmationSeeds = [2], MaximumBattles = 8,
            ReferenceEquipmentBalanceVersion = 2, CandidateEquipmentBalanceVersion = 3 };
        try
        {
            Assert.Equal(0, await AttributeAllocationStudy.RunAsync(request, default));
            var frozen = Path.Combine(output, "content", "Data", "equipment", "equipment-starters.v3.json");
            Assert.Equal(HarnessJson.FileHash(Path.Combine(root, "Data", "equipment", "equipment-starters.v3.json")), HarnessJson.FileHash(frozen));
            var builds = HarnessJson.Read<System.Text.Json.JsonElement>(Path.Combine(output, $"{cell.Id}.builds.json"));
            Assert.Contains("\"balanceVersion\": 3", builds.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("equipment-starters.v3.json", File.ReadAllText(Path.Combine(output, "manifest.json")));
            Assert.Equal(4, HarnessJson.Read<AttributeAllocationTrial[]>(Path.Combine(output, "trials.json")).Length);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public async Task All_party_doctrines_are_validated_before_any_fights_start()
    {
        var root = TestContentPaths.FindApiRoot();
        var source = HarnessJson.Read<AttributeAllocationRequest>(Path.Combine(root, "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "attribute-allocation-matched.json"));
        var output = Path.Combine(Path.GetTempPath(), "ll-attribute-preflight-" + Guid.NewGuid().ToString("N"));
        var valid = source.Cells[0];
        var invalid = valid with { Id = "invalid-party-doctrine", ComparisonGroup = null, Allies = [new(valid.Reference, new("missing-doctrine"))] };
        var request = source with { ContentRoot = root, OutputDirectory = output, Cells = [valid, invalid], ExplorationSeeds = [1], ConfirmationSeeds = [2], MaximumBattles = 16 };
        try
        {
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => AttributeAllocationStudy.RunAsync(request, default));
            Assert.Contains("missing-doctrine", error.Message);
            Assert.Empty(Directory.GetFiles(output, "*.replay.json"));
            Assert.False(File.Exists(Path.Combine(output, "trials.json")));
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Party_members_require_pvp_and_respect_the_five_actor_limit()
    {
        var root = TestContentPaths.FindApiRoot();
        var source = HarnessJson.Read<AttributeAllocationRequest>(Path.Combine(root, "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "attribute-allocation-matched.json"));
        var cell = source.Cells[0];
        var tooMany = cell with { Allies = Enumerable.Repeat(new AttributeAllocationPartyMember(cell.Reference), 5).ToArray() };
        Assert.Throws<InvalidDataException>(() => AttributeAllocationStudy.Validate(source with { Cells = [tooMany] }));
        Assert.Throws<InvalidDataException>(() => AttributeAllocationStudy.Validate(source with { Cells = [cell with { Opponent = null, Allies = [new(cell.Reference)] }] }));
    }

    [Fact]
    public void Diagnostic_exchanges_price_normalized_defense_and_materialized_core_separately()
    {
        AttributeAllocationStudy.ValidateDiagnostics(new Dictionary<AttributeType, float>
            { [AttributeType.Power] = -4, [AttributeType.ArmorRating] = 100 }, 1, AttributeRules.CurrentVersion);
        AttributeAllocationStudy.ValidateDiagnostics(new Dictionary<AttributeType, float>
            { [AttributeType.AttackSpeed] = -20, [AttributeType.AbilityHaste] = 10 }, 10, AttributeRules.CurrentVersion);
    }

    [Theory]
    [InlineData(AttributeType.Armor, 10)]
    [InlineData(AttributeType.LifeSteal, 10)]
    [InlineData(AttributeType.Power, 1)]
    [InlineData(AttributeType.Power, float.NaN)]
    public void Diagnostic_rejects_ambiguous_units_retired_purchases_nonfinite_and_free_budget(AttributeType type, float value) =>
        Assert.Throws<InvalidDataException>(() => AttributeAllocationStudy.ValidateDiagnostics(
            new Dictionary<AttributeType, float> { [type] = value }, 1, AttributeRules.CurrentVersion));

    [Fact]
    public void Historical_formulas_cannot_be_silently_used_for_current_raw_point_diagnostics() =>
        Assert.Throws<InvalidDataException>(() => AttributeAllocationStudy.ValidateDiagnostics(
            new Dictionary<AttributeType, float> { [AttributeType.AttackSpeed] = -20, [AttributeType.AbilityHaste] = 10 },
            1, AttributeRules.LegacyVersion));
}
