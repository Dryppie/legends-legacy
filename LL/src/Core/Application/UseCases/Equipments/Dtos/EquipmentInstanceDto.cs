using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{
    public Rarity Rarity { get; set; } = Rarity.Common;
    public int? Potential { get; set; } = null;
    public EquipmentBase EquipmentBase { get; set; } = null!;
    public List<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>();
    }
}