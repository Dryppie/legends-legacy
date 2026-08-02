using Application.UseCases.Inventories.SelectionCrates;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases.Items.Dtos;

public sealed class SelectionCrateMetadataDto
{
    public IReadOnlyList<SelectionCrateOptionDto> Options { get; init; } = [];
}

public sealed class SelectionCrateOptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class SelectionCrateMetadataResolver
    : IValueResolver<ItemBase, ItemBaseDto, SelectionCrateMetadataDto?>
{
    public SelectionCrateMetadataDto? Resolve(
        ItemBase source,
        ItemBaseDto destination,
        SelectionCrateMetadataDto? destinationMember,
        ResolutionContext context)
    {
        if (!source.Id.Equals(
                CatalystSelectionCrateCatalog.ItemBaseId,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SelectionCrateMetadataDto
        {
            Options = CatalystSelectionCrateCatalog.Options
                .Select(option => new SelectionCrateOptionDto
                {
                    Id = option.Id,
                    Name = option.Name,
                    Quantity = option.Quantity
                })
                .ToList()
        };
    }
}
