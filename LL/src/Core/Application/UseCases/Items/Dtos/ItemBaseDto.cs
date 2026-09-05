using System.Text.Json.Serialization;
using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Items.Dtos;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "ItemType")]
[JsonDerivedType(typeof(EssenceItemBaseDto), "Essence")]
[JsonDerivedType(typeof(EquipmentBaseDto), "Equipment")]
public class ItemBaseDto : IMapFrom<ItemBase>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Stackable { get; set; } = true;
    public bool IsBound { get; set; }
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public SelectionCrateMetadataDto? SelectionCrate { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ItemBase, ItemBaseDto>()
            .ForMember(
                destination => destination.SelectionCrate,
                options => options.MapFrom<SelectionCrateMetadataResolver>())
            .Include<EssenceItemBase, EssenceItemBaseDto>()
            .Include<EquipmentBase, EquipmentBaseDto>();
    }
}
