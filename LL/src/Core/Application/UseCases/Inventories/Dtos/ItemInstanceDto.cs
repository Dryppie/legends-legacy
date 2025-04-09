using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases.Inventories.Dtos;
public class ItemInstanceDto : IMapFrom<ItemInstance>
{
    public Guid Id { get; set; }
    public ItemBase ItemBase { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ItemInstance, ItemInstanceDto>();
    }
}