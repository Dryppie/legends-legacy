using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Crafting.Dtos;
using AutoMapper;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CraftingDtoMappingTests
{
    [Fact]
    public void CraftingDtoProfiles_MapCommandAndQueryResults()
    {
        var mapper = CreateMapper();
        var itemInstanceId = Guid.NewGuid();
        var equipment = new EquipmentInstance
        {
            Id = itemInstanceId,
            ItemBaseId = "stoneguard_helm",
            ItemBase = new EquipmentBase
            {
                Id = "stoneguard_helm",
                Name = "Stoneguard Helm",
                EquipmentType = EquipmentType.Head
            },
            Tier = 2,
            Quality = ItemQuality.Fine,
            Potential = 8,
            MaxPotential = 8
        };

        var craftResult = new CraftItemsResult(
            "stoneguard_helm",
            2,
            [
                new InventoryItem
                {
                    InventoryId = Guid.NewGuid(),
                    ItemInstanceId = itemInstanceId,
                    ItemInstance = equipment
                }
            ],
            new Dictionary<ItemQuality, int> { [ItemQuality.Fine] = 1 },
            25,
            1);

        var craftDto = mapper.Map<CraftItemsResultDto>(craftResult);

        Assert.Equal(["stoneguard_helm", "2"], [craftDto.RecipeId, craftDto.TargetTier.ToString()]);
        Assert.Equal(itemInstanceId, craftDto.CreatedItemIds.Single());
        Assert.Single(craftDto.CreatedItems);
        Assert.Equal(1, craftDto.QualityCounts[ItemQuality.Fine]);

        var temperDto = mapper.Map<TemperItemResultDto>(new TemperingAttemptResult(
            equipment,
            TemperingOutcomeType.Success,
            2,
            10,
            Rarity.Common,
            Rarity.Uncommon,
            true));

        Assert.Equal(TemperingOutcomeType.Success, temperDto.Outcome);
        Assert.Equal(Rarity.Uncommon, temperDto.NewRarity);
        Assert.Equal(itemInstanceId, temperDto.Equipment.Id);

        var recipeDto = mapper.Map<CraftingRecipeDto>(
            new CraftingRecipeDefinition
            {
                Id = "stoneguard_helm",
                Name = "Stoneguard Helm",
                OutputItemId = "stoneguard_helm",
                OutputItemType = EquipmentType.Head,
                TierRange = new TierRangeDefinition { Min = 1, Max = 3 },
                Forms =
                [
                    new CraftingRecipeFormDefinition
                    {
                        FormId = "helmet",
                        DisplayName = "Helmet",
                        OutputItemId = "stoneguard_helm",
                        OutputItemType = EquipmentType.Head
                    }
                ]
            },
            options =>
            {
                options.Items["CurrentMasteryLevel"] = 4;
                options.Items["Blueprints"] = Array.Empty<CraftingBlueprintOptionDto>();
                options.Items["MaterialCosts"] = Array.Empty<CraftingMaterialCostDto>();
            });

        Assert.Equal(4, recipeDto.CurrentMasteryLevel);
        Assert.Equal(1, recipeDto.MinTier);
        Assert.Equal(3, recipeDto.MaxTier);
        Assert.Equal("helmet", recipeDto.Forms.Single().FormId);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
