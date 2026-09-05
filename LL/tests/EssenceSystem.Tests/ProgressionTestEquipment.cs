using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace EssenceSystem.Tests;

internal static class ProgressionTestEquipment
{
    public static EquipmentInstance Create(
        string? equipmentSetId = null,
        EquipmentType equipmentType = EquipmentType.Chest,
        string? activeStyleId = null,
        Rarity rarity = Rarity.Common,
        Guid? ownerId = null,
        EquipmentBase? itemBase = null,
        IReadOnlyDictionary<AttributeType, float>? stats = null)
    {
        var id = Guid.NewGuid();
        var owner = ownerId ?? Guid.NewGuid();
        itemBase ??= new EquipmentBase
        {
            Id = $"test.equipment.{id:N}",
            Name = "Test Equipment",
            EquipmentType = equipmentType
        };
        var handedness = equipmentType is EquipmentType.OneHanded or EquipmentType.TwoHanded or EquipmentType.OffHand
            ? equipmentType.ToString()
            : string.Empty;
        var state = new EquipmentStateSnapshot(
            EquipmentBalance.ModelVersion,
            id,
            $"test.definition.{id:N}",
            $"test.archetype.{id:N}",
            1,
            0,
            1,
            activeStyleId,
            activeStyleId,
            new EquipmentProvenance(EquipmentAwardKind.RandomDiscovery, "test", id.ToString()),
            new EquipmentOwnership(EquipmentOwnershipKind.UnboundPersonal, owner));
        var data = new EquipmentData(
            state,
            itemBase.Id,
            itemBase.Name,
            (EquipmentRarity)rarity,
            equipmentType,
            new EquipmentBehaviorDefinition { Handedness = handedness },
            stats ?? new Dictionary<AttributeType, float> { [AttributeType.Power] = 1f },
            equipmentSetId);
        var item = new EquipmentInstance
        {
            Id = id,
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };
        item.ApplyProgressionData(data);
        return item;
    }
}
