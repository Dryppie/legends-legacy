import { AttributeModifier } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { Rarity } from '../../models/enums/rarity';
import { Equipment, EquipmentInstance } from '../../models/item';

export interface EquipmentDisplay {
  // Common
  name: string;
  rarity: Rarity;
  equipmentType: EquipmentType;
  description?: string;
  baseModifiers?: AttributeModifier[];
  instanceModifiers: AttributeModifier[];

  // Weapon only
  magnitude?: number;
  magnitudeRange?: number;
  scalingAttribute?: AttributeType;
  scalingAmount?: number;
  attackSpeed?: number;

  // Instance-only
  potential?: number;
}

export function mapEquipmentToDisplay(e: Equipment): EquipmentDisplay {
  return {
    name: e.name,
    rarity: e.rarity,
    equipmentType: e.equipmentType,
    description: e.description,
    instanceModifiers: e.attributeModifiers,

    magnitude: e.magnitude,
    magnitudeRange: e.magnitudeRange,
    scalingAttribute: e.scalingAttribute,
    scalingAmount: e.scalingAmount,
    attackSpeed: e.attackSpeed,
  };
}

export function mapInstanceToDisplay(
  inst: EquipmentInstance,
): EquipmentDisplay {
  const base = inst.equipmentBase;
  return {
    name: base.name,
    rarity: inst.rarity ?? base.rarity,
    equipmentType: base.equipmentType,
    description: base.description,
    baseModifiers: inst.baseModifiers,
    instanceModifiers: inst.instanceModifiers,

    magnitude: base.magnitude,
    magnitudeRange: base.magnitudeRange,
    scalingAttribute: base.scalingAttribute,
    scalingAmount: base.scalingAmount,
    attackSpeed: base.attackSpeed,

    potential: inst.potential,
  };
}
