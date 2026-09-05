import { AttributeModifier } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { Rarity } from '../../models/enums/rarity';
import { EquipmentProgression } from '../../models/equipment-progression';
import { Equipment, EquipmentInstance, EquipmentSetMetadata } from '../../models/item';
import { aggregateAttributes, sortAttributes } from '../../utils/attributes/attribute-order.utils';

export interface EquipmentDisplay {
  progression?: EquipmentProgression | null;
  name: string;
  rarity: Rarity;
  quality?: ItemQuality;
  equipmentType: EquipmentType;
  description?: string;
  attributes: AttributeModifier[];
  itemBudget: number;
  itemBudgetTier: number;
  requiredLevel: number;
  equipmentSet?: EquipmentSetMetadata | null;
}

export interface EquipmentAttributeComparison {
  attributeType: AttributeType;
  equippedAmount: number;
  hoveredAmount: number;
}

export function mapEquipmentToDisplay(equipment: Equipment): EquipmentDisplay {
  return {
    name: equipment.name,
    rarity: equipment.rarity,
    equipmentType: equipment.equipmentType,
    description: equipment.description,
    attributes: aggregateAttributes(equipment.attributeModifiers),
    itemBudget: equipment.itemBudget ?? 0,
    itemBudgetTier: equipment.itemBudgetTier ?? 1,
    requiredLevel: 1,
  };
}

export function mapInstanceToDisplay(instance: EquipmentInstance): EquipmentDisplay {
  const splitModifiers = [
    ...(instance.baseModifiers ?? []),
    ...(instance.instanceModifiers ?? []),
  ];
  return {
    name: instance.displayName || instance.equipmentBase.name,
    rarity: instance.rarity ?? instance.equipmentBase.rarity,
    quality: instance.quality,
    progression: instance.progression,
    equipmentType: instance.equipmentBase.equipmentType,
    description: instance.equipmentBase.description,
    attributes: aggregateAttributes(
      splitModifiers.length > 0 ? splitModifiers : (instance.attributeModifiers ?? []),
    ),
    itemBudget: instance.itemBudget ?? 0,
    itemBudgetTier: instance.itemBudgetTier ?? instance.tier ?? 1,
    requiredLevel: instance.requiredLevel ?? 1,
    equipmentSet: instance.equipmentSet,
  };
}

export function buildAttributeComparisons(
  hovered: EquipmentDisplay,
  equipped: EquipmentDisplay,
): EquipmentAttributeComparison[] {
  const hoveredByType = sumAttributesByType(hovered.attributes);
  const equippedByType = sumAttributesByType(equipped.attributes);
  const attributeTypes = new Set([...hoveredByType.keys(), ...equippedByType.keys()]);
  return sortAttributes(
    [...attributeTypes].map((attributeType) => ({
      attributeType,
      equippedAmount: equippedByType.get(attributeType) ?? 0,
      hoveredAmount: hoveredByType.get(attributeType) ?? 0,
    })),
  );
}

function sumAttributesByType(
  attributes: readonly AttributeModifier[],
): Map<AttributeType, number> {
  const totals = new Map<AttributeType, number>();
  for (const attribute of attributes) {
    totals.set(
      attribute.attributeType,
      (totals.get(attribute.attributeType) ?? 0) + attribute.amount,
    );
  }
  return totals;
}
