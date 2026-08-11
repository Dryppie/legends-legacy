import { AttributeType } from '../enums/attributeType';
import { ItemQuality } from '../enums/itemQuality';
import { Rarity } from '../enums/rarity';

export interface TemperingSessionDto {
  from: Date;
  to: Date;

  temperingSummary: TemperingSummary;
  outcomes?: TemperingOutcomeEntry[];
}

export type TemperingOutcome = 'Critical' | 'Positive' | 'Neutral' | 'Negative';

export interface TemperingOutcomeEntry {
  id: string;
  queueItemId: string;
  equipmentInstanceId: string;
  equipmentName: string;
  occurredAt: string;
  outcome: TemperingOutcome;
  potentialSpent: number;
  previousPotential: number;
  newPotential: number;
  previousItemXp: number;
  newItemXp: number;
  becameMasterpiece: boolean;
  becameLevelingItem: boolean;
  previousRarity: Rarity;
  newRarity: Rarity;
  rarityUpgraded: boolean;
  qualityIncreased: boolean;
  previousQuality?: ItemQuality | null;
  newQuality?: ItemQuality | null;
  improvedStat?: AttributeType | null;
  previousStatValue?: number | null;
  newStatValue?: number | null;
}

export interface TemperingSummary {
  totalItemsCrafted: number;
  masterpieces: number;
  levelingItems: number;
  cursedOutcomes: number;
  qualityIncreases: number;
  totalActions: number;
  totalSoulstones: number;
  craftingExperience: number;
  totalExperience: number;
}
