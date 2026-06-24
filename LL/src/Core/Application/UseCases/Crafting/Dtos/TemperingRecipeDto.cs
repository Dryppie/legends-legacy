using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class TemperingRecipeDto : IMapFrom<TemperingRecipeDefinition>
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentType> ApplicableItemTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredItemAffinityTags { get; init; } = [];
    public IReadOnlyList<string> DirectionTags { get; init; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TemperingRecipeDefinition, TemperingRecipeDto>();
    }
}
