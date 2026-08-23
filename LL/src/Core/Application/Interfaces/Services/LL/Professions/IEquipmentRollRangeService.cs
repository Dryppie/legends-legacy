using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Professions;

public interface IEquipmentRollRangeService
{
    EquipmentRollRange? Resolve(EquipmentInstance equipment);
}

public sealed record EquipmentRollRange(
    int MinimumPotential,
    int MaximumPotential,
    IReadOnlyList<EquipmentAttributeRollRange> Attributes);

public sealed record EquipmentAttributeRollRange(
    AttributeType AttributeType,
    float MinimumAmount,
    float MaximumAmount,
    float RarityBonusAmount,
    bool HasCraftedRange);
