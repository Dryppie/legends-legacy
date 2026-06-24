using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingRecipeDto : IMapFrom<CraftingRecipeDefinition>
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RecipeType RecipeType { get; init; }
    public string BaseRecipeId { get; init; } = string.Empty;
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public IReadOnlyList<CraftingRecipeFormDto> Forms { get; init; } = [];
    public IReadOnlyList<CraftingBlueprintOptionDto> Blueprints { get; init; } = [];
    public int MinTier { get; init; }
    public int MaxTier { get; init; }
    public int CurrentMasteryLevel { get; init; }
    public IReadOnlyList<string> AffinityTags { get; init; } = [];
    public IReadOnlyDictionary<AttributeType, double> BaseStatProfile { get; init; } = new Dictionary<AttributeType, double>();
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingRecipeDefinition, CraftingRecipeDto>()
            .ForMember(dest => dest.BaseRecipeId, opt => opt.MapFrom(src => src.BaseRecipeId ?? src.Id))
            .ForMember(dest => dest.MinTier, opt => opt.MapFrom(src => src.TierRange.Min))
            .ForMember(dest => dest.MaxTier, opt => opt.MapFrom(src => src.TierRange.Max))
            .ForMember(dest => dest.BaseStatProfile, opt => opt.MapFrom(src => src.BaseStatProfileOverride ?? src.BaseStatProfile))
            .ForMember(dest => dest.CurrentMasteryLevel, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("CurrentMasteryLevel", out var value) && value is int masteryLevel
                    ? masteryLevel
                    : 0))
            .ForMember(dest => dest.Blueprints, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("Blueprints", out var value) &&
                value is IReadOnlyList<CraftingBlueprintOptionDto> blueprints
                    ? blueprints
                    : Array.Empty<CraftingBlueprintOptionDto>()))
            .ForMember(dest => dest.MaterialCosts, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("MaterialCosts", out var value) &&
                value is IReadOnlyList<CraftingMaterialCostDto> materialCosts
                    ? materialCosts
                    : Array.Empty<CraftingMaterialCostDto>()));
    }
}
