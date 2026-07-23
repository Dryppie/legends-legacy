using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Inventories;
using Domain.Models.Items;

namespace Application.UseCases.Crafting.Dtos;

public sealed record CraftItemsResult(
    string RecipeId,
    string? BlueprintId,
    int TargetTier,
    IReadOnlyList<InventoryItem> CreatedItems,
    IReadOnlyDictionary<ItemQuality, int> QualityCounts,
    int MasteryXpGained,
    int NewMasteryLevel);

public sealed class CraftItemsResultDto : IMapFrom<CraftItemsResult>
{
    public string RecipeId { get; init; } = string.Empty;
    public string? BlueprintId { get; init; }
    public int TargetTier { get; init; }
    public IReadOnlyList<Guid> CreatedItemIds { get; init; } = [];
    public IReadOnlyList<InventoryItemDto> CreatedItems { get; init; } = [];
    public IReadOnlyDictionary<ItemQuality, int> QualityCounts { get; init; } = new Dictionary<ItemQuality, int>();
    public int MasteryXpGained { get; init; }
    public int NewMasteryLevel { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftItemsResult, CraftItemsResultDto>()
            .ForMember(dest => dest.CreatedItemIds, opt => opt.MapFrom(src => src.CreatedItems.Select(x => x.ItemInstanceId)));
    }
}
