import { AttributeModifier } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { GatheringType } from '../../models/enums/gatheringType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { Rarity } from '../../models/enums/rarity';
import {
  Equipment,
  EquipmentInstance,
  ToolBonusModifier,
} from '../../models/item';

export interface EquipmentDisplay {
  // Common
  name: string;
  rarity: Rarity;
  quality?: ItemQuality;
  equipmentType: EquipmentType;
  description?: string;
  baseModifiers?: AttributeModifier[];
  instanceModifiers: AttributeModifier[];
  powerScore: number;
  gatheringType?: GatheringType | null;
  toolBonuses: ToolBonusModifier[];
  toolAffixes: ToolBonusModifier[];
  baseToolBonuses: ToolBonusModifier[];

  // Weapon only
  magnitude?: number;
  magnitudeRange?: number;
  scalingAttribute?: AttributeType;
  scalingAmount?: number;
  attackSpeed?: number;

  // Instance-only
  potential?: number;
}

export function mapEquipmentToDisplay(
  e: Equipment,
  useBaseName = false,
): EquipmentDisplay {
  return {
    name:
      e.equipmentType === EquipmentType.Tool && !useBaseName
        ? getToolDisplayName(e.name, e.rarity)
        : e.name,
    rarity: e.rarity,
    equipmentType: e.equipmentType,
    description: e.description,
    instanceModifiers: sortAttributes(e.attributeModifiers),
    powerScore: calculatePowerScore(e.attributeModifiers),
    gatheringType: e.gatheringType,
    toolBonuses: e.toolBonuses ?? [],
    toolAffixes: [],
    baseToolBonuses: e.toolBonuses ?? [],

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
  const baseModifiers = sortAttributes(inst.baseModifiers ?? []);
  const instanceModifiers = sortAttributes(inst.instanceModifiers ?? []);
  const baseToolBonuses = base.toolBonuses ?? [];
  const toolAffixes = inst.toolAffixes ?? [];
  const effectiveToolBonuses = inst.effectiveToolBonuses?.length
    ? inst.effectiveToolBonuses
    : [...baseToolBonuses, ...toolAffixes];

  return {
    name: inst.displayName || base.name,
    rarity: inst.rarity ?? base.rarity,
    quality: inst.quality,
    equipmentType: base.equipmentType,
    description: base.description,
    baseModifiers,
    instanceModifiers,
    powerScore: calculatePowerScore([...baseModifiers, ...instanceModifiers]),
    gatheringType: base.gatheringType,
    toolBonuses: effectiveToolBonuses,
    toolAffixes,
    baseToolBonuses,

    magnitude: base.magnitude,
    magnitudeRange: base.magnitudeRange,
    scalingAttribute: base.scalingAttribute,
    scalingAmount: base.scalingAmount,
    attackSpeed: base.attackSpeed,

    potential: inst.potential,
  };
}

const ATTRIBUTE_ORDER = Object.values(AttributeType);

const ATTRIBUTE_WEIGHTS: Partial<Record<AttributeType, number>> = {
  [AttributeType.Power]: 8,
  [AttributeType.Fortitude]: 8,
  [AttributeType.Precision]: 8,
  [AttributeType.Spirit]: 5,
  [AttributeType.MaxHealth]: 0.18,
  [AttributeType.WeaponDamage]: 18,
  [AttributeType.Armor]: 4,
  [AttributeType.Resistance]: 4,
  [AttributeType.CritChance]: 4,
  [AttributeType.CritDamage]: 1.5,
  [AttributeType.ArmorPenetration]: 2,
  [AttributeType.MagicPenetration]: 2,
  [AttributeType.DodgeChance]: 5,
  [AttributeType.BlockChance]: 3,
  [AttributeType.DamageReduction]: 7,
  [AttributeType.HealingPowerPercent]: 2,
  [AttributeType.HealthRegeneration]: 8,
  [AttributeType.LifeSteal]: 4,
  [AttributeType.Cooldown]: 3,
  [AttributeType.StatusResistance]: 2,
  [AttributeType.CrowdControlResistance]: 2,
  [AttributeType.SummonPower]: 4,
  [AttributeType.SummonHealth]: 0.15,
  [AttributeType.AttackSpeed]: 3,
};

function sortAttributes(attributes: AttributeModifier[]): AttributeModifier[] {
  return [...attributes].sort((a, b) => {
    const orderDelta =
      getAttributeOrder(a.attributeType) - getAttributeOrder(b.attributeType);

    return orderDelta || a.attributeType.localeCompare(b.attributeType);
  });
}

function getAttributeOrder(attribute: AttributeType): number {
  const index = ATTRIBUTE_ORDER.indexOf(attribute);
  return index === -1 ? Number.MAX_SAFE_INTEGER : index;
}

function calculatePowerScore(attributes: AttributeModifier[]): number {
  const total = attributes.reduce((sum, attribute) => {
    const weight = ATTRIBUTE_WEIGHTS[attribute.attributeType] ?? 1;
    return sum + Math.max(0, attribute.amount) * weight;
  }, 0);

  return Math.max(0, Math.round(total));
}

function getToolDisplayName(baseName: string, rarity: Rarity): string {
  switch (rarity) {
    case Rarity.Common:
      return `Plain ${baseName}`;
    case Rarity.Uncommon:
      return `Sturdy ${baseName}`;
    case Rarity.Rare:
      return `Proven ${baseName}`;
    case Rarity.Epic:
      return `Exquisite ${baseName}`;
    case Rarity.Unique:
      return `Fabled ${baseName}`;
    case Rarity.Legendary:
      return `Mythic ${baseName}`;
    case Rarity.Legacy:
      return `Eternal ${baseName}`;
    default:
      return baseName;
  }
}
