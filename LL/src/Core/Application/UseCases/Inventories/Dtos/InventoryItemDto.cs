using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Inventories;

namespace Application.UseCases.Inventories.Dtos;
public class InventoryItemDto : IMapFrom<InventoryItem>
{
    public Guid ItemInstanceId { get; set; }
    public ItemInstanceDto ItemInstance { get; set; } = null!;
    public int Quantity { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<InventoryItem, InventoryItemDto>();
    }
}