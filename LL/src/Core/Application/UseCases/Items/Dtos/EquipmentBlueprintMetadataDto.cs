using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Items.Dtos;

public sealed class EquipmentBlueprintMetadataDto
{
    public string StyleId { get; init; } = string.Empty;
    public IReadOnlyList<AttributeType> Attributes { get; init; } = [];
    public EquipmentSetDto? EquipmentSet { get; init; }
}

public sealed class EquipmentBlueprintMetadataResolver
    : IValueResolver<ItemBase, ItemBaseDto, EquipmentBlueprintMetadataDto?>
{
    private readonly EquipmentCatalog? _equipment;
    private readonly EquipmentBlueprintCatalog? _blueprints;

    public EquipmentBlueprintMetadataResolver()
    {
    }

    public EquipmentBlueprintMetadataResolver(
        EquipmentCatalog equipment,
        EquipmentBlueprintCatalog blueprints)
    {
        _equipment = equipment;
        _blueprints = blueprints;
    }

    public EquipmentBlueprintMetadataDto? Resolve(
        ItemBase source,
        ItemBaseDto destination,
        EquipmentBlueprintMetadataDto? destinationMember,
        ResolutionContext context)
    {
        if (_equipment is null || _blueprints is null)
            return null;

        var blueprint = _blueprints.Blueprints.SingleOrDefault(candidate =>
            string.Equals(candidate.ItemId, source.Id, StringComparison.OrdinalIgnoreCase));
        var style = blueprint is null
            ? null
            : _equipment.Styles.SingleOrDefault(candidate =>
                string.Equals(candidate.Id, blueprint.StyleId, StringComparison.OrdinalIgnoreCase));
        if (style is null)
            return null;

        return new EquipmentBlueprintMetadataDto
        {
            StyleId = style.Id,
            Attributes = style.StatWeights
                .OrderByDescending(attribute => attribute.Value)
                .ThenBy(
                    attribute => attribute.Key.ToString(),
                    StringComparer.Ordinal)
                .Select(attribute => attribute.Key)
                .ToArray(),
            EquipmentSet = EquipmentSetDto.FromDefinition(
                style.EquipmentSetId is null
                    ? null
                    : _equipment.GetEquipmentSet(style.EquipmentSetId))
        };
    }
}
