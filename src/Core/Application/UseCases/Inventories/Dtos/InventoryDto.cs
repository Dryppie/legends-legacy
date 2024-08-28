using Application.Common.Mappings;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Inventories;

namespace Application.UseCases.Inventories.Dtos;
public class InventoryDto : IMapFrom<Inventory>
{
    public List<ItemDto> Items { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Inventory, InventoryDto>();
    }
}