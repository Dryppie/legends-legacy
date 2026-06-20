export interface AbilityCatalogV2BehaviorDiagnosticReport {
  scenarioCount: number;
  passedCount: number;
  failedCount: number;
  scenarios: AbilityCatalogV2BehaviorScenarioResult[];
  abilityCount: number;
  coveredAbilityCount: number;
  missingAbilityIds: string[];
  isComplete: boolean;
  hasFullAbilityCoverage: boolean;
}

export interface AbilityCatalogV2BehaviorScenarioResult {
  behaviorId: string;
  abilityId: string;
  passed: boolean;
  outcome: string | null;
  duration: number;
  eventCount: number;
  failures: string[];
}

export interface AbilityCatalogV2DiagnosticReport {
  abilityCount: number;
  statusCount: number;
  summonCount: number;
  indexedAbilityTags: number;
  indexedStatusTags: number;
  indexedSummonTags: number;
  indexedTriggerEvents: number;
  timedSummonCount: number;
  persistentSummonCount: number;
  summonAbilityReferenceCount: number;
  summons: AbilityCatalogV2SummonDiagnostic[];
  outcome: string;
  duration: number;
  eventLogCount: number;
  directDamageObserved: boolean;
  barrierObserved: boolean;
  damageOverTimeObserved: boolean;
  reflectObserved: boolean;
  failures: string[];
}

export interface AbilityCatalogV2SummonDiagnostic {
  id: string;
  name: string;
  imagePath: string;
  durationTicks: number;
  maxActive: number;
  hasTimedDuration: boolean;
  expiresOnOwnerDeath: boolean;
  abilityIds: string[];
  tags: string[];
}

export interface AbilityCatalogV2CoverageReport {
  essenceCount: number;
  requiredSlotCount: number;
  coveredSlotCount: number;
  currentReferenceCoveredSlotCount: number;
  slots: AbilityCatalogV2SlotCoverage[];
  gaps: AbilityCatalogV2CoverageGap[];
  unownedAbilityIds: string[];
  runtimeLoadoutChecks: AbilityCatalogV2RuntimeLoadoutCheck[];
  isComplete: boolean;
}

export interface AbilityCatalogV2SlotCoverage {
  essenceId: string;
  slot: string;
  legacyAbilityId: string;
  v2AbilityId: string | null;
  hasOwnedV2Ability: boolean;
  currentReferenceExistsInV2: boolean;
  kindMatches: boolean;
}

export interface AbilityCatalogV2CoverageGap {
  essenceId: string;
  slot: string;
  legacyAbilityId: string;
  reason: string;
}

export interface AbilityCatalogV2RuntimeLoadoutCheck {
  essenceId: string;
  abilityIds: string[];
  isReady: boolean;
  outcome: string | null;
  duration: number;
  eventCount: number;
  failure: string | null;
}
