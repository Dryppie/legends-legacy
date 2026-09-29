using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Inventories.SelectionCrates;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Items.Dtos;

public sealed class SelectionCrateMetadataDto
{
    public string SelectionLabel { get; init; } = string.Empty;
    public bool IsRandom { get; init; }
    public IReadOnlyList<SelectionCrateOptionDto> Options { get; init; } = [];
}

public sealed class SelectionCrateOptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public EssenceDefinitionDto? Essence { get; init; }
}

public sealed class SelectionCrateMetadataResolver
    : IValueResolver<ItemBase, ItemBaseDto, SelectionCrateMetadataDto?>
{
    private readonly IEssenceDefinitionRepository? _definitions;
    private readonly TowerEquipmentSupplyCatalog? _towerSupplies;

    public SelectionCrateMetadataResolver()
    {
    }

    public SelectionCrateMetadataResolver(IEssenceDefinitionRepository definitions,
        TowerEquipmentSupplyCatalog? towerSupplies = null)
    {
        _definitions = definitions;
        _towerSupplies = towerSupplies;
    }

    public SelectionCrateMetadataDto? Resolve(
        ItemBase source,
        ItemBaseDto destination,
        SelectionCrateMetadataDto? destinationMember,
        ResolutionContext context)
    {
        if (_towerSupplies?.Find(source.Id) is not null)
            return new SelectionCrateMetadataDto
            {
                SelectionLabel = "Equipment",
                Options = _towerSupplies.Choices(source.Id).Select(choice => new SelectionCrateOptionDto
                {
                    Id = choice.Id, Name = choice.Name, Description = choice.Description, Quantity = 1
                }).ToArray()
            };
        var definition = SelectionContainerCatalog.Find(source.Id);
        if (definition is null) return null;

        return Map(definition, context);
    }

    private SelectionCrateMetadataDto Map(SelectionContainerDefinition definition, ResolutionContext context) =>
        new()
        {
            SelectionLabel = definition.SelectionLabel,
            IsRandom = definition.RandomEquipment is not null,
            Options = definition.Options
                .Select(option => new SelectionCrateOptionDto
                {
                    Id = option.Id,
                    Name = option.Name,
                    Quantity = option.Quantity,
                    Essence = MapEssence(option, context)
                })
                .ToList()
        };

    private EssenceDefinitionDto? MapEssence(SelectionContainerOptionDefinition option, ResolutionContext context)
    {
        if (_definitions is null || !option.ItemId.StartsWith("item.essence.", StringComparison.OrdinalIgnoreCase))
            return null;

        var essence = _definitions.GetById(option.ItemId["item.".Length..]);
        return essence is null ? null : context.Mapper.Map<EssenceDefinitionDto>(essence);
    }
}
