using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingRecipeFormDto : IMapFrom<CraftingRecipeFormDefinition>
{
    public string FormId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string OutputItemId { get; init; } = string.Empty;
    public EquipmentType OutputItemType { get; init; }
    public string? ArmorWeight { get; init; }
    public string? StatProfileId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingRecipeFormDefinition, CraftingRecipeFormDto>();
    }
}
