using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Sets;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentSetDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentSetBonusDto> Bonuses { get; init; } = [];

    public static EquipmentSetDto? FromDefinition(EquipmentSetDefinition? definition) =>
        definition is null
            ? null
            : new EquipmentSetDto
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                Bonuses = definition.Bonuses
                    .Where(bonus => bonus.Enabled)
                    .OrderBy(bonus => bonus.RequiredEquippedItems)
                    .Select(bonus => new EquipmentSetBonusDto
                    {
                        Id = bonus.Id,
                        RequiredEquippedItems = bonus.RequiredEquippedItems,
                        Description = bonus.Description
                    })
                    .ToArray()
            };
}

public sealed class EquipmentSetBonusDto
{
    public string Id { get; init; } = string.Empty;
    public int RequiredEquippedItems { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class EquipmentSetMetadataResolver
    : IValueResolver<EquipmentInstance, EquipmentInstanceDto, EquipmentSetDto?>
{
    private readonly EquipmentCatalog? _catalog;

    public EquipmentSetMetadataResolver()
    {
    }

    public EquipmentSetMetadataResolver(EquipmentCatalog catalog)
    {
        _catalog = catalog;
    }

    public EquipmentSetDto? Resolve(
        EquipmentInstance source,
        EquipmentInstanceDto destination,
        EquipmentSetDto? destinationMember,
        ResolutionContext context)
    {
        var equipmentSetId = source.ProgressionData?.EquipmentSetId;
        if (_catalog is null || string.IsNullOrWhiteSpace(equipmentSetId))
            return null;

        return EquipmentSetDto.FromDefinition(
            _catalog.GetEquipmentSet(equipmentSetId));
    }
}
