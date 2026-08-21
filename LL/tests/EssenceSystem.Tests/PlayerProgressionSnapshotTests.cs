using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class PlayerProgressionSnapshotTests
{
    [Fact]
    public void Content_manifest_generates_complete_essence_free_attribute_snapshots()
    {
        var report = CreateFactory().Generate();

        Assert.Equal(1, report.Version);
        Assert.Equal(12 * 3 * 3, report.Snapshots.Count);
        Assert.All(report.Snapshots, snapshot =>
        {
            Assert.Equal(
                AttributeCatalog.All.Select(definition => definition.AttributeType).Order(),
                snapshot.Attributes.Keys.Order());
            Assert.Equal(
                snapshot.RegionNumber,
                snapshot.EquipmentTier);
            Assert.True(
                snapshot.CharacterLevel
                >= EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(snapshot.EquipmentTier));
            Assert.DoesNotContain(snapshot.EquipmentPoints.Keys, attribute =>
                !AttributeCatalog.IsEquipmentEligible(attribute));
            Assert.True(snapshot.CombatRating.Overall > 0);
            Assert.True(snapshot.UnmitigatedBasicPressure > 0);
            Assert.True(snapshot.PhysicalEffectiveDurability > 0);
            Assert.True(snapshot.MagicalEffectiveDurability > 0);
        });
    }

    [Fact]
    public void Gear_acquisition_is_explicit_and_increases_through_a_region()
    {
        var snapshots = CreateFactory().Generate().Snapshots;
        var entry = Single(snapshots, "region-02-entry", "expected", "balanced");
        var midpoint = Single(snapshots, "region-02-mid", "expected", "balanced");
        var completion = Single(snapshots, "region-02-end", "expected", "balanced");

        Assert.Equal(0.15, entry.CurrentTierShare, 6);
        Assert.Equal(0.85, completion.CurrentTierShare, 6);
        Assert.InRange(midpoint.CurrentTierShare, entry.CurrentTierShare, completion.CurrentTierShare);
        Assert.True(entry.TotalEquipmentBudget < midpoint.TotalEquipmentBudget);
        Assert.True(midpoint.TotalEquipmentBudget < completion.TotalEquipmentBudget);
    }

    [Fact]
    public void Gear_envelope_budgets_are_ordered_at_every_anchor_and_allocation()
    {
        var snapshots = CreateFactory().Generate().Snapshots;

        foreach (var group in snapshots.GroupBy(snapshot => new
                 {
                     snapshot.AnchorId,
                     snapshot.AllocationProfileId
                 }))
        {
            var minimum = group.Single(snapshot => snapshot.GearEnvelopeId == "minimum");
            var expected = group.Single(snapshot => snapshot.GearEnvelopeId == "expected");
            var optimized = group.Single(snapshot => snapshot.GearEnvelopeId == "optimized");

            Assert.True(minimum.TotalEquipmentBudget < expected.TotalEquipmentBudget);
            Assert.True(expected.TotalEquipmentBudget < optimized.TotalEquipmentBudget);
        }
    }

    [Fact]
    public void Allocation_profiles_create_the_intended_pressure_and_durability_tradeoff()
    {
        var snapshots = CreateFactory().Generate().Snapshots;
        var offensive = Single(snapshots, "region-03-end", "expected", "offensive");
        var balanced = Single(snapshots, "region-03-end", "expected", "balanced");
        var defensive = Single(snapshots, "region-03-end", "expected", "defensive-support");

        Assert.True(offensive.UnmitigatedBasicPressure > balanced.UnmitigatedBasicPressure);
        Assert.True(balanced.UnmitigatedBasicPressure > defensive.UnmitigatedBasicPressure);
        Assert.True(defensive.PhysicalEffectiveDurability > balanced.PhysicalEffectiveDurability);
        Assert.True(defensive.MagicalEffectiveDurability > balanced.MagicalEffectiveDurability);
        Assert.True(balanced.PhysicalEffectiveDurability > offensive.PhysicalEffectiveDurability);
        Assert.True(balanced.MagicalEffectiveDurability > offensive.MagicalEffectiveDurability);
    }

    [Fact]
    public void Snapshot_generation_is_deterministic()
    {
        var factory = CreateFactory();

        var first = SerializeReport(factory.Generate());
        var second = SerializeReport(factory.Generate());

        Assert.Equal(first, second);
    }

    [Fact]
    public void Manifest_covers_campaign_and_region_boundary_anchors()
    {
        var snapshots = CreateFactory().Generate().Snapshots;
        int[] requiredPositions = [1, 5, 10, 11, 15, 20, 21, 30, 41, 50, 91, 100];

        Assert.Equal(
            requiredPositions,
            snapshots.Select(snapshot => snapshot.ProgressionPosition).Distinct().Order());
    }

    private static PlayerProgressionSnapshot Single(
        IEnumerable<PlayerProgressionSnapshot> snapshots,
        string anchorId,
        string envelopeId,
        string allocationId) =>
        snapshots.Single(snapshot =>
            snapshot.AnchorId == anchorId
            && snapshot.GearEnvelopeId == envelopeId
            && snapshot.AllocationProfileId == allocationId);

    private static string SerializeReport(PlayerProgressionSnapshotReport report) =>
        JsonSerializer.Serialize(
            report.Snapshots.Select(snapshot => new
            {
                snapshot.AnchorId,
                snapshot.GearEnvelopeId,
                snapshot.AllocationProfileId,
                snapshot.CurrentTierShare,
                snapshot.TotalEquipmentBudget,
                Attributes = snapshot.Attributes.OrderBy(entry => entry.Key),
                EquipmentPoints = snapshot.EquipmentPoints.OrderBy(entry => entry.Key),
                snapshot.CombatRating,
                snapshot.UnmitigatedBasicPressure,
                snapshot.PhysicalEffectiveDurability,
                snapshot.MagicalEffectiveDurability
            }));

    private static PlayerProgressionSnapshotFactory CreateFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return new PlayerProgressionSnapshotFactory(
            configuration,
            TestContentPaths.FindApiRoot(),
            options);
    }
}
