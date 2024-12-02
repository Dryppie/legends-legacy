using Application.Common.Mappings;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Inventories;

namespace Application.UseCases.Inventories.Dtos;
public class InventoryItemDto : IMapFrom<InventoryItem>
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; } = null!;
    public int Quantity { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<InventoryItem, InventoryItemDto>();
    }
}