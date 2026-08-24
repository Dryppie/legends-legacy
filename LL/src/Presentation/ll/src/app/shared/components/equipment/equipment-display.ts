import { EquipmentType } from '../../models/enums/equipmentType';
import { GatheringType } from '../../models/enums/gatheringType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { Rarity } from '../../models/enums/rarity';
import {
  Equipment,
  EquipmentInstance,
  EquipmentCraftingDesignMetadata,
  EquipmentSetMetadata,
  ToolBonusModifier,
} from '../../models/item';
import {
  aggregateAttributes,
  sortAttributes,
} from '../../utils/attributes/attribute-order.utils';
import { AttributeModifier } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';

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
  requiredLevel: number;
  statModelVersion: number;
  gatheringType?: GatheringType | null;
  toolBonuses: ToolBonusModifier[];
  toolAffixes: ToolBonusModifier[];
  baseToolBonuses: ToolBonusModifier[];

  // Instance-only
  potential?: number;
  minimumPotential?: number;
  maximumPotential?: number;
  attributeRollRanges?: EquipmentAttributeRollRange[];
  craftingDesign?: EquipmentCraftingDesignMetadata | null;
  equipmentSet?: EquipmentSetMetadata | null;
}

export interface EquipmentAttributeRollRange {
  attributeType: AttributeType;
  minimumAmount: number;
  maximumAmount: number;
  rarityBonusAmount: number;
  hasCraftedRange: boolean;
}

export interface EquipmentAttributeComparison {
  attributeType: AttributeType;
  equippedAmount: number;
  hoveredAmount: number;
}

export interface ToolBonusComparison {
  bonusType: ToolBonusModifier['bonusType'];
  scopeId?: string;
  equippedAmount: number;
  hoveredAmount: number;
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
    requiredLevel: 1,
    statModelVersion: 15,
    gatheringType: e.gatheringType,
    toolBonuses: e.toolBonuses ?? [],
    toolAffixes: [],
    baseToolBonuses: e.toolBonuses ?? [],
    attributeRollRanges: [],
  };
}

export function mapInstanceToDisplay(
  inst: EquipmentInstance,
): EquipmentDisplay {
  const base = inst.equipmentBase;
  const splitModifiers = [
    ...(inst.baseModifiers ?? []),
    ...(inst.instanceModifiers ?? []),
  ];
  const attributes = aggregateAttributes(
    splitModifiers.length > 0
      ? splitModifiers
      : (inst.attributeModifiers ?? []),
  );
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
    requiredLevel:
      base.equipmentType === EquipmentType.Tool ? 1 : (inst.requiredLevel ?? 1),
    statModelVersion: inst.statModelVersion ?? 15,
    gatheringType: base.gatheringType,
    toolBonuses: effectiveToolBonuses,
    toolAffixes,
    baseToolBonuses,

    potential: inst.potential,
    minimumPotential: inst.rollRange?.minimumPotential,
    maximumPotential: inst.rollRange?.maximumPotential,
    attributeRollRanges: inst.rollRange?.attributes ?? [],
    craftingDesign: inst.craftingDesign,
    equipmentSet: inst.equipmentSet,
  };
}

export function buildAttributeComparisons(
  hovered: EquipmentDisplay,
  equipped: EquipmentDisplay,
): EquipmentAttributeComparison[] {
  const hoveredByType = sumAttributesByType(hovered.attributes);
  const equippedByType = sumAttributesByType(equipped.attributes);
  const attributeTypes = new Set([
    ...hoveredByType.keys(),
    ...equippedByType.keys(),
  ]);

  return sortAttributes(
    [...attributeTypes].map((attributeType) => {
      const hoveredAmount = hoveredByType.get(attributeType) ?? 0;
      const equippedAmount = equippedByType.get(attributeType) ?? 0;

      return {
        attributeType,
        equippedAmount,
        hoveredAmount,
      };
    }),
  );
}

export function buildToolBonusComparisons(
  hovered: EquipmentDisplay,
  equipped: EquipmentDisplay,
): ToolBonusComparison[] {
  const hoveredByKey = sumToolBonusesByKey(hovered.toolBonuses);
  const equippedByKey = sumToolBonusesByKey(equipped.toolBonuses);
  const keys = new Set([...hoveredByKey.keys(), ...equippedByKey.keys()]);

  return [...keys]
    .map((key) => {
      const hoveredBonus = hoveredByKey.get(key);
      const equippedBonus = equippedByKey.get(key);

      return {
        bonusType: (hoveredBonus ?? equippedBonus)!.bonusType,
        scopeId: (hoveredBonus ?? equippedBonus)!.scopeId,
        equippedAmount: equippedBonus?.amount ?? 0,
        hoveredAmount: hoveredBonus?.amount ?? 0,
      };
    })
    .sort((first, second) =>
      `${first.bonusType}:${first.scopeId ?? ''}`.localeCompare(
        `${second.bonusType}:${second.scopeId ?? ''}`,
      ),
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

function sumToolBonusesByKey(
  bonuses: readonly ToolBonusModifier[],
): Map<string, ToolBonusModifier> {
  const totals = new Map<string, ToolBonusModifier>();

  for (const bonus of bonuses) {
    const key = `${bonus.bonusType}\u0000${bonus.scopeId ?? ''}`;
    const existing = totals.get(key);
    totals.set(key, {
      ...bonus,
      amount: existing
        ? combinePercentageBonuses(existing.amount, bonus.amount)
        : Math.max(0, bonus.amount),
    });
  }

  return totals;
}

function combinePercentageBonuses(first: number, second: number): number {
  return (
    (1 + Math.max(0, first) / 100) * (1 + Math.max(0, second) / 100) * 100 - 100
  );
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
