using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingBlueprintOptionDto : IMapFrom<BlueprintDefinition>
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? BlueprintFamily { get; init; }
    public string OutputNameTemplate { get; init; } = string.Empty;
    public IReadOnlyList<BlueprintOutputNameDefinition> SpecialOutputNames { get; init; } = [];
    public IReadOnlyList<string> CompatibleFormIds { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<BlueprintDefinition, CraftingBlueprintOptionDto>()
            .ForMember(dest => dest.OutputNameTemplate, opt => opt.MapFrom(src => src.OutputNameTemplate ?? "{BlueprintName} {FormName}"))
            .ForMember(dest => dest.CompatibleFormIds, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("CompatibleFormIds", out var value) &&
                value is IReadOnlyList<string> compatibleFormIds
                    ? compatibleFormIds
                    : Array.Empty<string>()))
            .ForMember(dest => dest.MaterialCosts, opt => opt.MapFrom((_, _, _, context) =>
                context.Items.TryGetValue("MaterialCosts", out var value) &&
                value is IReadOnlyList<CraftingMaterialCostDto> materialCosts
                    ? materialCosts
                    : Array.Empty<CraftingMaterialCostDto>()));
    }
}
