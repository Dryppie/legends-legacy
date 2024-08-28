using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases.Items.Dtos;
public class ItemDto : IMapFrom<Item>
{
    public string Name { get; set; } = string.Empty;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Item, ItemDto>();
    }
}