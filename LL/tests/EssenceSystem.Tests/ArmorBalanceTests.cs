using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class ArmorBalanceTests
{
    [Theory]
    [InlineData(1, 0, ItemQuality.Standard)]
    [InlineData(2, 5, ItemQuality.Masterpiece)]
    public void Armor_catalog_materializes_the_three_profiles_in_every_slot(
        int tier, int rank, ItemQuality quality)
    {
        var catalog = LoadCatalog();
        var armor = catalog.Options.Where(option => option.EquipmentType is
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs).ToArray();
        Assert.Equal(9, armor.Length);

        var expectedProfiles = new Dictionary<string, double[]>
        {
            ["Heavy"] = [0, .40, .30, .30],
            ["Medium"] = [.35, .25, .20, .20],
            ["Light"] = [.70, .10, .10, .10]
        };
        AttributeType[] attributes =
            [AttributeType.Power, AttributeType.MaxHealth, AttributeType.Armor, AttributeType.Resistance];

        foreach (var role in expectedProfiles.Keys)
        {
            var roleItems = armor.Where(option =>
                catalog.Evaluator.GetArchetype(option.DefinitionId).Behavior.Role == role).ToArray();
            Assert.Equal(3, roleItems.Length);
            Assert.Equal(3, roleItems.Select(option => option.EquipmentType).Distinct().Count());

            foreach (var option in roleItems)
            {
                var evaluation = catalog.Evaluator.Evaluate(
                    option.DefinitionId, tier, rank, null, quality, 1d);
                Assert.Equal(role == "Heavy" ? 3 : 4, evaluation.Stats.Count);
                for (var index = 0; index < attributes.Length; index++)
                {
                    var attribute = attributes[index];
                    var weight = expectedProfiles[role][index];
                    Assert.Equal(weight, evaluation.Archetype.StatWeights.GetValueOrDefault(attribute), 10);
                    var expected = AttributeValueQuantizer.Quantize(attribute,
                        evaluation.TargetBudget * weight
                        / EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(attribute, tier));
                    Assert.Equal((float)expected, evaluation.Stats.GetValueOrDefault(attribute), precision: 2);
                }
                Assert.Equal(evaluation.Stats[AttributeType.Armor], evaluation.Stats[AttributeType.Resistance]);
            }
        }
    }

    [Fact]
    public void Cloth_is_absent_from_bases_definitions_options_and_variant_compatibility()
    {
        var catalog = LoadCatalog();
        Assert.DoesNotContain(catalog.EquipmentBases.Keys, id => id.Contains("cloth", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(catalog.Evaluator.Definitions, definition =>
            definition.ArchetypeId.Contains("cloth", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(catalog.Options, option => option.DefinitionId.Contains("cloth", StringComparison.OrdinalIgnoreCase));
        Assert.All(catalog.Styles, style => Assert.DoesNotContain(style.CompatibleArchetypeIds,
            id => id.Contains("cloth", StringComparison.OrdinalIgnoreCase)));
        Assert.All(catalog.Evaluator.Definitions, definition =>
            Assert.NotNull(catalog.GetEquipmentBase(catalog.Evaluator.GetArchetype(definition.ArchetypeId).ItemBaseId)));
    }

    [Theory]
    [InlineData(-1d, 0f)]
    [InlineData(0d, 0f)]
    [InlineData(55d, 40f)]
    [InlineData(165d, 60f)]
    [InlineData(1045d, 76f)]
    public void Both_defenses_use_the_same_tier_normalized_damage_reduction_curve(
        double normalizedRating, float expectedReduction)
    {
        foreach (var attribute in new[] { AttributeType.Armor, AttributeType.Resistance })
        {
            foreach (var tier in new[] { 1, 2, 10, 100 })
            {
                var rating = normalizedRating * EquipmentTierBudgetCurve.GetScale(tier);
                Assert.Equal(expectedReduction,
                    EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(attribute, rating, tier), precision: 4);
            }
            Assert.Equal(expectedReduction,
                EquipmentStatBudgetCatalog.ConvertNormalizedRatingToEffectiveValue(attribute, normalizedRating), precision: 4);
            Assert.Equal(Math.Max(0d, normalizedRating),
                EquipmentStatBudgetCatalog.ConvertEffectiveValueToNormalizedRating(attribute, expectedReduction), precision: 4);
            Assert.Equal(80f, EquipmentStatBudgetCatalog.ConvertNormalizedRatingToEffectiveValue(attribute, double.PositiveInfinity));
        }
    }

    private static StarterEquipmentCatalog LoadCatalog() => JsonStarterEquipmentCatalog.Load(
        Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));
}
