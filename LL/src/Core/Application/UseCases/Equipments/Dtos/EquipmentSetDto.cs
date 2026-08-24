using Application.Interfaces.Services.LL.Professions;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Sets;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentSetDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public static EquipmentSetDto? FromDefinition(EquipmentSetDefinition? definition) =>
        definition is null
            ? null
            : new EquipmentSetDto
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description
            };
}

public sealed class EquipmentSetMetadataResolver
    : IValueResolver<EquipmentInstance, EquipmentInstanceDto, EquipmentSetDto?>
{
    private readonly ICraftingDefinitionProvider? _definitions;

    public EquipmentSetMetadataResolver()
    {
    }

    public EquipmentSetMetadataResolver(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public EquipmentSetDto? Resolve(
        EquipmentInstance source,
        EquipmentInstanceDto destination,
        EquipmentSetDto? destinationMember,
        ResolutionContext context)
    {
        if (_definitions is null || string.IsNullOrWhiteSpace(source.EquipmentSetId))
            return null;

        return EquipmentSetDto.FromDefinition(
            _definitions.GetEquipmentSet(source.EquipmentSetId));
    }
}
