using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{
    public string DisplayName { get; set; } = string.Empty;
    public Rarity Rarity { get; set; } = Rarity.Common;
    public int? Potential { get; set; } = null;
    public EquipmentBase EquipmentBase { get; set; } = null!;
    public int ItemXp { get; set; } = 0;
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers { get; set; } = [];
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<AttributeModifierBase> AttributeModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolAffixes { get; set; } = [];
    public IReadOnlyList<ToolBonusModifier> EffectiveToolBonuses { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>();
    }
}
