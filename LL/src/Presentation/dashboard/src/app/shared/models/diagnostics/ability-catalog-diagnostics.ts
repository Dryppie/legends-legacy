export interface AbilityCatalogBehaviorDiagnosticReport {
  scenarioCount: number;
  passedCount: number;
  failedCount: number;
  scenarios: AbilityCatalogBehaviorScenarioResult[];
  abilityCount: number;
  coveredAbilityCount: number;
  missingAbilityIds: string[];
  isComplete: boolean;
  hasFullAbilityCoverage: boolean;
}

export interface AbilityCatalogBehaviorScenarioResult {
  behaviorId: string;
  abilityId: string;
  passed: boolean;
  outcome: string | null;
  duration: number;
  eventCount: number;
  failures: string[];
}

export interface AbilityCatalogDiagnosticReport {
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
  summons: AbilityCatalogSummonDiagnostic[];
  outcome: string;
  duration: number;
  eventLogCount: number;
  directDamageObserved: boolean;
  barrierObserved: boolean;
  damageOverTimeObserved: boolean;
  reflectObserved: boolean;
  failures: string[];
}

export interface AbilityCatalogSummonDiagnostic {
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

export interface AbilityCatalogCoverageReport {
  essenceCount: number;
  requiredSlotCount: number;
  coveredSlotCount: number;
  currentReferenceCoveredSlotCount: number;
  slots: AbilityCatalogSlotCoverage[];
  gaps: AbilityCatalogCoverageGap[];
  unownedAbilityIds: string[];
  runtimeLoadoutChecks: AbilityCatalogRuntimeLoadoutCheck[];
  isComplete: boolean;
}

export interface AbilityCatalogSlotCoverage {
  essenceId: string;
  slot: string;
  legacyAbilityId: string;
  abilityId: string | null;
  hasOwnedAbility: boolean;
  currentReferenceExists: boolean;
  kindMatches: boolean;
}

export interface AbilityCatalogCoverageGap {
  essenceId: string;
  slot: string;
  legacyAbilityId: string;
  reason: string;
}

export interface AbilityCatalogRuntimeLoadoutCheck {
  essenceId: string;
  abilityIds: string[];
  isReady: boolean;
  outcome: string | null;
  duration: number;
  eventCount: number;
  failure: string | null;
}

export interface RegionOneContentDiagnosticReport {
  manifestEntryCount: number;
  completeEntryCount: number;
  missingEntryCount: number;
  awaitingManaCount: number;
  staleAreaCount: number;
  isComplete: boolean;
  entries: RegionOneContentEntryDiagnostic[];
  warnings: string[];
}

export interface RegionOneContentEntryDiagnostic {
  name: string;
  creatureKey: string;
  sourceType: string;
  sourceName: string;
  expectedTier: string;
  essenceId: string | null;
  activeAbilityId: string | null;
  passiveAbilityId: string | null;
  requiresMana: boolean;
  creatureResolved: boolean;
  essenceResolved: boolean;
  activeAbilityResolved: boolean;
  passiveAbilityResolved: boolean;
  essenceItemResolved: boolean;
  sourcePlacementResolved: boolean;
  behaviorCovered: boolean;
  isComplete: boolean;
  missing: string[];
}
