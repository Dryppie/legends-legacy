import { InventoryItem } from './inventoryItem';
import { ItemQuality } from './enums/itemQuality';
import { EquipmentType } from './enums/equipmentType';

export enum RecipeType {
  Base = 'Base',
  Variant = 'Variant',
}

export interface CraftingMaterialCost {
  itemId: string;
  name: string;
  tier?: number | null;
  required: number;
  owned: number;
}

export interface CraftingRecipe {
  id: string;
  name: string;
  recipeType: RecipeType;
  baseRecipeId: string;
  outputItemId: string;
  outputItemType: EquipmentType;
  forms: CraftingRecipeForm[];
  blueprints: CraftingBlueprintOption[];
  minTier: number;
  maxTier: number;
  currentMasteryLevel: number;
  affinityTags: string[];
  baseStatProfile: Record<string, number>;
  materialCosts: CraftingMaterialCost[];
}

export interface CraftingBlueprintOption {
  id: string;
  name: string;
  blueprintFamily?: string | null;
  outputNameTemplate: string;
  specialOutputNames: CraftingBlueprintOutputName[];
  compatibleFormIds: string[];
  tags: string[];
  materialCosts: CraftingMaterialCost[];
}

export interface CraftingBlueprintOutputName {
  baseRecipeId: string;
  formId: string;
  outputName: string;
}

export interface CraftingRecipeForm {
  formId: string;
  displayName: string;
  outputItemId: string;
  outputItemType: EquipmentType;
  armorWeight?: string | null;
  statProfileId?: string | null;
  tags: string[];
}

export interface CraftItemsRequest {
  recipeId: string;
  formId?: string | null;
  blueprintId?: string | null;
  targetTier: number;
  quantity: number;
}

export interface CraftItemsResult {
  recipeId: string;
  targetTier: number;
  createdItemIds: string[];
  createdItems: InventoryItem[];
  qualityCounts: Partial<Record<ItemQuality, number>>;
  masteryXpGained: number;
  newMasteryLevel: number;
}

export interface LearnBlueprintResult {
  blueprintId: string;
  unlockedRecipeId: string;
  unlockedRecipeName: string;
}

export interface BlueprintLearningOption {
  recipeId: string;
  recipeName: string;
  outputItemType: EquipmentType;
  compatibleFormIds: string[];
  compatibleFormNames: string[];
}
