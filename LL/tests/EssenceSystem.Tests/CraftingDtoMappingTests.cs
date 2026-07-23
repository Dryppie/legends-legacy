using Application.Common.Mappings;
using Application.UseCases.Crafting.Dtos;
using AutoMapper;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CraftingDtoMappingTests
{
    [Fact]
    public void CraftItemsResultMapsRecipeAndOptionalBlueprintIdentity()
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var itemId = Guid.NewGuid();
        var result = new CraftItemsResult(
            "recipe.armor.head.heavy_helm",
            "blueprint_aegis",
            1,
            [
                new InventoryItem
                {
                    InventoryId = Guid.NewGuid(),
                    ItemInstanceId = itemId,
                    ItemInstance = new EquipmentInstance
                    {
                        Id = itemId,
                        ItemBaseId = "heavy_helm",
                        ItemBase = new EquipmentBase
                        {
                            Id = "heavy_helm",
                            Name = "Heavy Helm",
                            EquipmentType = EquipmentType.Head
                        }
                    }
                }
            ],
            new Dictionary<ItemQuality, int> { [ItemQuality.Fine] = 1 },
            25,
            1);

        var dto = mapper.Map<CraftItemsResultDto>(result);

        Assert.Equal("recipe.armor.head.heavy_helm", dto.RecipeId);
        Assert.Equal("blueprint_aegis", dto.BlueprintId);
        Assert.Equal(itemId, dto.CreatedItemIds.Single());
    }
}
