using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Application.UseCases.Items.Dtos;
public class EquipmentBaseDto : ItemBaseDto, IMapFrom<EquipmentBase>
{
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public GatheringType? GatheringType { get; set; }
    public double YieldBonusPercent { get; set; }
    public double RareChanceBonusPercent { get; set; }
    public double DoubleGatherChancePercent { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentBase, EquipmentBaseDto>();
    }
}
