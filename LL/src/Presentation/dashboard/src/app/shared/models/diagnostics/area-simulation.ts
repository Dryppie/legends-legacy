export interface AreaSimulationOptions {
  areas: AreaSimulationAreaOption[];
  profiles: string[];
  builds: AreaSimulationBuildOption[];
  regionProjections: AreaSimulationRegionProjection[];
  maximumEncounters: number;
}

export interface AreaSimulationAreaOption {
  id: string;
  name: string;
  regionKey: string;
  levelRequirement: number;
  globalStep: number;
  regionStep: number;
  recommendedCombatRating: number;
  profileId: string;
  targetWinRateBasisPoints: number;
  defaultBuildId: string;
}

export interface AreaSimulationBuildOption {
  id: string;
  tier: number;
  quality: string;
  rarity: string;
}

export interface AreaSimulationRegionProjection {
  regionNumber: number;
  equipmentTier: number;
  endingCharacterLevel: number;
  essenceCount: number;
  recommendedEndpointCombatRating: number;
  maximumEndpointCombatRating: number;
  profiles: AreaSimulationProfileProjection[];
}

export interface AreaSimulationProfileProjection {
  profile: string;
  combatRating: number;
}

export interface AreaSimulationRequest {
  areaId: string;
  encounterCount: number;
  randomSeed: number;
  characterProfile: string;
  buildId: string;
}

export interface CreatureScalingProfile {
  profileId: string;
  regionKey: string | null;
  globalStep: number;
  regionStep: number | null;
  progressionStep: number;
  recommendedCombatRating: number | null;
  healthMultiplier: number;
  offenseMultiplier: number;
  defenseMultiplier: number;
  resistanceMultiplier: number;
  attackSpeedMultiplier: number;
  penetrationMultiplier: number;
  softDefenseMultiplier: number;
  critChanceBonus: number;
  critDamageBonus: number;
  critChanceCap: number;
  critDamageCap: number;
}

export interface AreaSimulationReport {
  areaId: string;
  areaName: string;
  levelRequirement: number;
  characterProfile: string;
  buildId: string;
  playerMaxHealth: number;
  requestedEncounters: number;
  victories: number;
  defeats: number;
  draws: number;
  winRate: number;
  averageCombatTicks: number;
  medianCombatTicks: number;
  p95CombatTicks: number;
  averageDamageTaken: number;
  p95DamageTaken: number;
  targetExperiencePerHour: number;
  targetCindersPerHour: number;
  effectiveExperiencePerHour: number;
  effectiveCindersPerHour: number;
  randomSeed: number;
  scaling: CreatureScalingProfile;
  compositions: AreaSimulationCompositionResult[];
  encounters: AreaSimulationEncounterResult[];
}

export interface AreaSimulationCompositionResult {
  composition: string;
  attempts: number;
  victories: number;
  winRate: number;
  averageCombatTicks: number;
  averageDamageTaken: number;
}

export interface AreaSimulationEncounterResult {
  encounterNumber: number;
  seed: number;
  outcome: string;
  combatTicks: number;
  damageTaken: number;
  remainingHealth: number;
  enemies: string[];
}

export interface RegionAreaBalanceRequest {
  regionKey: string;
  encountersPerProfile: number;
  randomSeed: number;
}

export interface RegionAreaBalanceReport {
  regionKey: string;
  balanceVersion: number;
  targetWinRateBasisPoints: number;
  encountersPerProfile: number;
  isSmooth: boolean;
  isWithinTolerance: boolean;
  warnings: string[];
  areas: RegionAreaBalanceResult[];
}

export interface RegionAreaBalanceResult {
  areaId: string;
  areaName: string;
  globalStep: number;
  levelRequirement: number;
  buildId: string;
  status: string;
  averageWinRate: number;
  lowestProfileWinRate: number;
  effectiveExperiencePerHour: number;
  effectiveCindersPerHour: number;
  scaling: CreatureScalingProfile;
  profiles: RegionAreaProfileBalanceResult[];
}

export interface RegionAreaProfileBalanceResult {
  profile: string;
  winRate: number;
  averageCombatTicks: number;
  p95DamageTaken: number;
}
