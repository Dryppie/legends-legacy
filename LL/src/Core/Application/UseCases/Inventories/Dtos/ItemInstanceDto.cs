using System.Text.Json.Serialization;
using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Items.Resources;

namespace Application.UseCases.Inventories.Dtos;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "ItemBase.ItemType")]
[JsonDerivedType(typeof(EssenceItemInstanceDto), "Essence")]
[JsonDerivedType(typeof(EquipmentInstanceDto), "Equipment")]
[JsonDerivedType(typeof(ResourceInstanceDto), "Resource")]
public class ItemInstanceDto : IMapFrom<ItemInstance>
{
    public Guid Id { get; set; }
    public ItemBaseDto ItemBase { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ItemInstance, ItemInstanceDto>()
            .Include<EssenceItemInstance, EssenceItemInstanceDto>()
            .Include<EquipmentInstance, EquipmentInstanceDto>()
            .Include<ResourceInstance, ResourceInstanceDto>();
    }
}