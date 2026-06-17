using Domain.Models.Items.Equipments;

namespace Services.AdminDashboard.Items;
public static class EquipmentMapper
{
    public static EquipmentToJsonDto ToDto(this EquipmentBase r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        ItemType = r.ItemType,
        Rarity = r.Rarity,
        Stackable = r.Stackable,
        EquipmentType = r.EquipmentType,
        AttributeModifiers = r.AttributeModifiers.Select(a => a.ToDto()).ToList(),
        ToolBonuses = r.ToolBonuses,
        AttackSpeed = r.AttackSpeed,
        Magnitude = r.Magnitude,
        MagnitudeRange = r.MagnitudeRange,
        GatheringType = r.GatheringType,
        YieldBonusPercent = r.YieldBonusPercent,
        RareChanceBonusPercent = r.RareChanceBonusPercent,
        DoubleGatherChancePercent = r.DoubleGatherChancePercent,
        ScalingAttribute = r.ScalingAttribute,
        ScalingAmount = r.ScalingAmount
    };

    public static EquipmentBase ToEntity(this EquipmentToJsonDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        ItemType = dto.ItemType,
        Rarity = dto.Rarity,
        Stackable = dto.Stackable,
        EquipmentType = dto.EquipmentType,
        AttributeModifiers = dto.AttributeModifiers.Select(a => a.ToEntity()).ToList(),
        ToolBonuses = dto.ToolBonuses,
        AttackSpeed = dto.AttackSpeed,
        Magnitude = dto.Magnitude,
        MagnitudeRange = dto.MagnitudeRange,
        GatheringType = dto.GatheringType,
        YieldBonusPercent = dto.YieldBonusPercent,
        RareChanceBonusPercent = dto.RareChanceBonusPercent,
        DoubleGatherChancePercent = dto.DoubleGatherChancePercent,
        ScalingAttribute = dto.ScalingAttribute,
        ScalingAmount = dto.ScalingAmount,
    };
}
