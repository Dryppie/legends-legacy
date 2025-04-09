using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentBaseDto : IMapFrom<EquipmentBase>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public ICollection<AttributeModifier> AttributeModifiers { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentBase, EquipmentBaseDto>();
    }
}