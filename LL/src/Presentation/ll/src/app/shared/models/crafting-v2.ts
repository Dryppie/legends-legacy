import { InventoryItem } from './inventoryItem';
import { AttributeType } from './enums/attributeType';
import { ItemQuality } from './enums/itemQuality';
import { EquipmentType } from './enums/equipmentType';
import { Rarity } from './enums/rarity';
import { EquipmentSetMetadata } from './item';

export interface CraftingMaterialCost {
  itemId: string;
  name: string;
  tier?: number | null;
  required: number;
  owned: number;
  sources: string[];
}

export interface EquipmentBehavior {
  handedness: string;
  attackCategory: string;
  rangeCategory: string;
  basicAttackIntervalMultiplier: number;
  basicAttackDamageMultiplier: number;
  role: string;
}

export interface CraftingAttributePreview {
  attributeType: AttributeType;
  baseAmount: number;
  minimumCraftedAmount: number;
  maximumCraftedAmount: number;
  minimumTotalAmount: number;
  maximumTotalAmount: number;
}

export interface CraftingQualityChance {
  quality: ItemQuality;
  chancePercent: number;
}

export interface CraftingItemPreview {
  name: string;
  description: string;
  equipmentType: EquipmentType;
  rarity: Rarity;
  tier: number;
  requiredLevel?: number;
  statModelVersion: number;
  attributes: CraftingAttributePreview[];
  qualityChances: CraftingQualityChance[];
  minimumStartingPotential: number;
  maximumStartingPotential: number;
}

export interface CraftingBlueprint {
  id: string;
  itemId: string;
  name: string;
  craftedItemName: string;
  isLearned: boolean;
  isLocked: boolean;
  sourceType?: string | null;
  sourceId?: string | null;
  equipmentSet?: EquipmentSetMetadata | null;
  behavior: EquipmentBehavior;
  initialStatProfile: Record<string, number>;
  bonusStatProfile?: Record<string, number>;
  bonusStatBudgetMultiplier: number;
  primaryTemperingStats: string[];
  secondaryTemperingStats: string[];
  temperingProfileSummary: string;
  tags: string[];
  materialCosts: CraftingMaterialCost[];
  itemPreview?: CraftingItemPreview | null;
}

export interface CraftingRecipe {
  id: string;
  name: string;
  icon: string;
  category: string;
  outputItemId: string;
  outputItemType: EquipmentType;
  minTier: number;
  maxTier: number;
  currentMasteryLevel: number;
  currentMasteryExperience: number;
  masteryExperienceRequiredForNextLevel: number;
  minimumProfessionLevel: number;
  behavior: EquipmentBehavior;
  initialStatProfile: Record<string, number>;
  primaryTemperingStats: string[];
  secondaryTemperingStats: string[];
  temperingProfileSummary: string;
  affinityTags: string[];
  tags: string[];
  materialCosts: CraftingMaterialCost[];
  itemPreview?: CraftingItemPreview | null;
  blueprints: CraftingBlueprint[];
}

export interface CraftItemsRequest {
  recipeId: string;
  blueprintId?: string | null;
  targetTier: number;
  quantity: number;
}

export interface CraftItemsResult {
  recipeId: string;
  blueprintId?: string | null;
  targetTier: number;
  createdItemIds: string[];
  createdItems: InventoryItem[];
  qualityCounts: Partial<Record<ItemQuality, number>>;
  masteryXpGained: number;
  newMasteryLevel: number;
}

export interface LearnBlueprintResult {
  blueprintId: string;
  blueprintName: string;
  recipeId: string;
  recipeName: string;
}
