import { EquipmentType } from '../../models/enums/equipmentType';
import { GatheringType } from '../../models/enums/gatheringType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { Rarity } from '../../models/enums/rarity';
import {
  Equipment,
  EquipmentInstance,
  EquipmentCraftingDesignMetadata,
  ToolBonusModifier,
} from '../../models/item';
import { aggregateAttributes } from '../../utils/attributes/attribute-order.utils';
import { AttributeModifier } from '../../models/Dtos/attributesDto';

export interface EquipmentDisplay {
  // Common
  name: string;
  rarity: Rarity;
  quality?: ItemQuality;
  equipmentType: EquipmentType;
  description?: string;
  attributes: AttributeModifier[];
  itemBudget: number;
  itemBudgetTier: number;
  gatheringType?: GatheringType | null;
  toolBonuses: ToolBonusModifier[];
  toolAffixes: ToolBonusModifier[];
  baseToolBonuses: ToolBonusModifier[];

  // Instance-only
  potential?: number;
  craftingDesign?: EquipmentCraftingDesignMetadata | null;
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
    attributes: aggregateAttributes(e.attributeModifiers),
    itemBudget: e.itemBudget ?? 0,
    itemBudgetTier: e.itemBudgetTier ?? 1,
    gatheringType: e.gatheringType,
    toolBonuses: e.toolBonuses ?? [],
    toolAffixes: [],
    baseToolBonuses: e.toolBonuses ?? [],
  };
}

export function mapInstanceToDisplay(
  inst: EquipmentInstance,
): EquipmentDisplay {
  const base = inst.equipmentBase;
  const attributes = aggregateAttributes([
    ...(inst.baseModifiers ?? []),
    ...(inst.instanceModifiers ?? []),
  ]);
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
    attributes,
    itemBudget: inst.itemBudget ?? 0,
    itemBudgetTier: inst.itemBudgetTier ?? inst.tier ?? 1,
    gatheringType: base.gatheringType,
    toolBonuses: effectiveToolBonuses,
    toolAffixes,
    baseToolBonuses,

    potential: inst.potential,
    craftingDesign: inst.craftingDesign,
  };
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
