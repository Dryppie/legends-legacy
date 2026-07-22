export type PowerRatingConfidence = 'Low' | 'Medium' | 'High';
export type PowerAnalysisState =
  | 'Available'
  | 'Unsupported'
  | 'InsufficientCombatData'
  | 'LowConfidence'
  | 'CalculationFailed';

export interface PowerRatingSnapshot {
  algorithmVersion: number;
  buildFingerprint: string;
  overall: number;
  singleTargetOffense: number;
  multiTargetOffense: number;
  physicalDurability: number;
  magicalDurability: number;
  sustain: number;
  controlUtility: number;
  computedAtUtc: string;
  confidence: PowerRatingConfidence;
  state: PowerAnalysisState;
  statusMessage?: string | null;
}

export interface PowerRequirementProfile {
  singleTarget: number;
  areaDamage: number;
  physicalDurability: number;
  magicalDurability: number;
  sustain: number;
  control: number;
  bossBurst: number;
  attrition: number;
}

export interface DungeonPowerRecommendation {
  recommendedPartyPower: number;
  lowerRecommendedPower: number;
  upperRecommendedPower: number;
  requirements: PowerRequirementProfile;
  algorithmVersion: number;
  dungeonContentHash: string;
  confidence: PowerRatingConfidence;
  state: PowerAnalysisState;
  simulationCount: number;
  estimatedRunDuration: string;
  canonicalPartyCompletionRates: Record<string, number>;
  statusMessage?: string | null;
}

export type DungeonReadinessBand =
  | 'VeryUnlikely'
  | 'Risky'
  | 'Uncertain'
  | 'Favored'
  | 'Comfortable';

export interface ReadinessInsight {
  code: string;
  message: string;
  severity: number;
}

export interface DungeonReadinessResult {
  partyPower: PowerRatingSnapshot;
  recommendation: DungeonPowerRecommendation;
  band: DungeonReadinessBand;
  estimatedCompletionProbability: number;
  completionProbabilityLowerBound: number;
  completionProbabilityUpperBound: number;
  checkpointReachProbability?: number | null;
  strengths: ReadinessInsight[];
  weaknesses: ReadinessInsight[];
  simulationCount: number;
  confidence: PowerRatingConfidence;
  state: PowerAnalysisState;
  statusMessage?: string | null;
}
