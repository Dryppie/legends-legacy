import { AttributeModifier } from './Dtos/attributesDto';
import { AttributeType } from './enums/attributeType';
import { EquipmentType } from './enums/equipmentType';
import { GatheringType } from './enums/gatheringType';
import { ItemType } from './enums/itemType';
import { ItemQuality } from './enums/itemQuality';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';
import { EssenceDefinitionDto } from './essence-system';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
  displayName?: string;
  source?: string;
  category?: string;
}

export interface EquipmentInstance extends ItemInstance {
  displayName: string;
  rarity: Rarity;
  quality: ItemQuality;
  baseRecipeId?: string | null;
  blueprintId?: string | null;
  craftingDesign?: EquipmentCraftingDesignMetadata | null;
  tier: number;
  equipmentBase: Equipment;
  potential?: number;
  maxPotential?: number | null;
  temperingProgress: number;
  itemXp: number;
  baseModifiers: AttributeModifier[];
  instanceModifiers: AttributeModifier[];
  attributeModifiers: AttributeModifier[];
  toolAffixes: ToolBonusModifier[];
  effectiveToolBonuses: ToolBonusModifier[];
  affinityTags: string[];
  itemBudget: number;
  itemBudgetTier: number;
  balanceVersion: number;
}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
  isBound?: boolean;
  blueprint?: BlueprintItemMetadata | null;
}

export interface BlueprintItemMetadata {
  blueprintId: string;
  name: string;
  description: string;
  requiredRecipeTags: string[];
  anyRecipeTags: string[];
  compatibleRecipeCount: number;
  compatibleRecipes: BlueprintCompatibleRecipe[];
  sourceType?: string | null;
  sourceId?: string | null;
}

export interface BlueprintCompatibleRecipe {
  id: string;
  name: string;
}

export interface EquipmentCraftingDesignMetadata {
  recipeId: string;
  blueprintId?: string | null;
  name: string;
  description: string;
  handedness: string;
  attackCategory: string;
  rangeCategory: string;
  basicAttackIntervalMultiplier: number;
  basicAttackDamageMultiplier: number;
  role: string;
  primaryTemperingStats: string[];
  secondaryTemperingStats: string[];
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
  toolBonuses?: ToolBonusModifier[];
  attackSpeed: number;
  magnitude: number;
  magnitudeRange: number;
  gatheringType?: GatheringType | null;
  scalingAttribute: AttributeType;
  scalingAmount: number;
  itemBudget: number;
  itemBudgetTier: number;
  balanceVersion: number;
}

export interface ToolBonusModifier {
  id: string;
  equipmentBaseId?: string;
  equipmentInstanceId?: string;
  name?: string;
  bonusType: ToolBonusType;
  amount: number;
  scopeId?: string;
}

export enum ToolBonusType {
  GatheringYieldPercent = 'GatheringYieldPercent',
  RareMaterialChancePercent = 'RareMaterialChancePercent',
  DoubleGatherChancePercent = 'DoubleGatherChancePercent',
  NodeSuccessChancePercent = 'NodeSuccessChancePercent',
  SpecificNodeYieldPercent = 'SpecificNodeYieldPercent',
  SpecificRegionYieldPercent = 'SpecificRegionYieldPercent',
  SpecificResourceYieldPercent = 'SpecificResourceYieldPercent',
  SpecificToolTypeYieldPercent = 'SpecificToolTypeYieldPercent',
  BonusRollChancePercent = 'BonusRollChancePercent',
  MinimumQuantityBonus = 'MinimumQuantityBonus',
  MaximumQuantityBonus = 'MaximumQuantityBonus',
}

export interface EssenceItem extends ItemBase {
  essence?: EssenceDefinitionDto;
  essenceDefinitionId: string;
  dismantleDustAmount: number;
}

export function essenceItemToEssence(item: EssenceItem): Essence {
  const essenceDefinitionId = inferEssenceDefinitionId(item);
  return {
    id: essenceDefinitionId,
    name: item.name,
    active: {
      name: 'Unbound Essence',
      description: item.description,
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targeting: [],
      cooldown: 0,
      effects: [],
    },
    passive: {
      name: 'Soul Archive',
      description: 'Absorb this item to add it to the Soul Archive.',
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targeting: [],
      cooldown: 0,
      effects: [],
    },
    attributeModifiers: [],
  };
}

export function inferEssenceDefinitionId(item: EssenceItem): string {
  return item.essenceDefinitionId || item.id.replace(/^item\./i, '');
}
