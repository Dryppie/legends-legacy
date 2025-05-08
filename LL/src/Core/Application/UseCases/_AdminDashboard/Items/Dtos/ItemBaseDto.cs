using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases._AdminDashboard.Items.Dtos;
public class ItemBaseDto : IMapFrom<ItemBase>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }

    public void UpdateProperties(ItemBase item)
    {
        item.Id = Id;
        item.Name = Name;
        item.Description = Description;
        item.ItemType = ItemType;
        item.Rarity = Rarity;
    }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ItemBase, ItemBaseDto>();
    }
}