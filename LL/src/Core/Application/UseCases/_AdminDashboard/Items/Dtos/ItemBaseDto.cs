using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Application.UseCases._AdminDashboard.Items.Dtos;
public class ItemBaseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public List<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolBonuses { get; set; } = [];
    public int AttackSpeed { get; set; } = 0;
    public int Magnitude { get; set; } = 0;
    public int MagnitudeRange { get; set; } = 0;
    public GatheringType? GatheringType { get; set; }
    public double YieldBonusPercent { get; set; }
    public double RareChanceBonusPercent { get; set; }
    public double DoubleGatherChancePercent { get; set; }
    public AttributeType ScalingAttribute { get; set; } = AttributeType.Power;
    public float ScalingAmount { get; set; } = 0.0f;
}
