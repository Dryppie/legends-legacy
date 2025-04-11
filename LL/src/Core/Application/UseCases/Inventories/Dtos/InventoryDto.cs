using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Inventories;

namespace Application.UseCases.Inventories.Dtos;
public class InventoryDto : IMapFrom<Inventory>
{
    public List<InventoryItemDto> InventoryItems { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Inventory, InventoryDto>();
    }
}