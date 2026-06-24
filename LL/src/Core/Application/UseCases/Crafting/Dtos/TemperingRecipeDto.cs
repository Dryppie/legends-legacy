using Domain.Models.Items.Equipments;

namespace Application.UseCases.Crafting.Dtos;

public sealed class TemperingRecipeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentType> ApplicableItemTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredItemAffinityTags { get; init; } = [];
    public IReadOnlyList<string> DirectionTags { get; init; } = [];
    public int PotentialCost { get; init; }
}
