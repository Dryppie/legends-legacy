using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class BlueprintLearningOptionDto : IMapFrom<CraftingRecipeDefinition>
{
    public string RecipeId { get; init; } = string.Empty;
    public string RecipeName { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public IReadOnlyList<string> CompatibleFormIds { get; init; } = [];
    public IReadOnlyList<string> CompatibleFormNames { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingRecipeDefinition, BlueprintLearningOptionDto>()
            .ForMember(dest => dest.RecipeId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RecipeName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CompatibleFormIds, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("CompatibleFormIds", out var value) &&
                value is IReadOnlyList<string> compatibleFormIds
                    ? compatibleFormIds
                    : Array.Empty<string>()))
            .ForMember(dest => dest.CompatibleFormNames, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("CompatibleFormNames", out var value) &&
                value is IReadOnlyList<string> compatibleFormNames
                    ? compatibleFormNames
                    : Array.Empty<string>()));
    }
}
