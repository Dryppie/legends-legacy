using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;

namespace Application.UseCases.Items.Dtos;
public class EquipmentBaseDto : ItemBaseDto, IMapFrom<EquipmentBase>
{
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentBase, EquipmentBaseDto>();
    }
}