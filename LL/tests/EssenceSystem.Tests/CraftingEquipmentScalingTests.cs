using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingEquipmentScalingTests
{
    [Fact]
    public void RollBaseStats_HigherTierBudgetCreatesStrongerEquipmentBase()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatRecipe();

        var tierOne = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var tierTwo = service.RollBaseStats(equipment, recipe, 2, ItemQuality.Standard, new FixedRandom(0.5d)).Single();

        Assert.True(tierTwo.Amount > tierOne.Amount);
    }

    [Fact]
    public void RollBaseStats_QualityMultiplierIncreasesCraftedStatBudget()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatRecipe();

        var standard = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var fine = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Fine, new FixedRandom(0.5d)).Single();

        Assert.True(fine.Amount > standard.Amount);
    }

    private static CraftingRecipeDefinition CreateSingleStatRecipe() => new()
    {
        Id = "recipe_head_armor",
        Name = "Head Armor",
        OutputItemId = "iron_helm",
        OutputItemType = EquipmentType.Head,
        BaseStatProfile = new Dictionary<AttributeType, double>
        {
            [AttributeType.Armor] = 1d
        }
    };

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;
    }
}
