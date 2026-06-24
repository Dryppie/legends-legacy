using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingMaterialCostDto : IMapFrom<ResolvedMaterialCost>
{
    public string ItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? Tier { get; init; }
    public int Required { get; init; }
    public int Owned { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ResolvedMaterialCost, CraftingMaterialCostDto>()
            .ForMember(dest => dest.Required, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.Owned, opt => opt.MapFrom((src, _, _, context) =>
                context.Items.TryGetValue("OwnedByItemId", out var value) &&
                value is IReadOnlyDictionary<string, int> ownedByItemId
                    ? ownedByItemId.GetValueOrDefault(src.ItemId)
                    : 0));
    }
}
