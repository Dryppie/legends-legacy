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

export interface AbilityBalanceSimulationRequest {
  battleCount: number;
  teamSize: number;
  essencesPerParticipant: number;
  randomSeed: number;
  topResults: number;
  candidatePoolSize: number;
  candidateTeams: AbilityBalanceTeamLoadout[] | null;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentProfile: string;
  useCanonicalRoles?: boolean;
}

export interface AbilityBalanceTeamLoadout {
  participants: AbilityBalanceParticipantLoadout[];
}

export interface AbilityBalanceParticipantLoadout {
  essenceIds: string[];
  role?: string;
}

export interface AbilityBalanceSimulationReport {
  mode: string;
  requestedBattleCount: number;
  battlesRun: number;
  teamSize: number;
  essencesPerParticipant: number;
  randomSeed: number;
  candidateTeamCount: number;
  candidatePoolSize: number;
  availableEssenceCount: number;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentProfile: string;
  participantAttributes: Record<string, number>;
  participantAttributesByRole?: Record<string, Record<string, number>> | null;
  availableEssences: AbilityBalanceEssenceDefinition[];
  rankedCombinations: AbilityBalanceCombinationResult[];
  essenceResults: AbilityBalanceEssenceResult[];
  battleSummaries: AbilityBalanceBattleSummary[];
  matchupResults: AbilityBalanceMatchupResult[];
}

export interface AbilityBalanceEssenceDefinition {
  essenceId: string;
  sourceMonsterId: string;
  abilityIds: string[];
}

export interface AbilityBalanceEssenceResult {
  essenceId: string;
  displayName: string;
  teamAppearances: number;
  battles: number;
  wins: number;
  losses: number;
  draws: number;
  score: number;
  scoreDelta: number;
  adjustedScoreDelta: number;
  confidenceLower: number;
  confidenceUpper: number;
  averageDuration: number;
  averageDamageDone: number;
  averageDamageTaken: number;
  classification: string;
}

export interface AbilityBalanceAuditRequest {
  teamSize: number;
  essencesPerParticipant: number;
  candidatePoolSize: number;
  screeningBattleCount: number;
  finalistCount: number;
  finalistBattleCount: number;
  validationBattleCount: number;
  randomSeeds: number[];
  equipmentTier: number;
  equipmentRarity: string;
  equipmentProfile: string;
  useCanonicalRoles?: boolean;
}

export interface AbilityBalanceAuditHistoryEntry {
  id: string;
  request: AbilityBalanceAuditRequest;
  report: AbilityBalanceAuditReport;
  completedAtUtc: string;
}

export interface AbilityBalanceAuditReport {
  contentHash: string;
  screeningBattlesRun: number;
  validationBattlesRun: number;
  finalistBattlesRun: number;
  totalBattlesRun: number;
  candidateTeamsTested: number;
  finalistTeamCount: number;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentProfile: string;
  participantAttributes: Record<string, number>;
  participantAttributesByRole?: Record<string, Record<string, number>> | null;
  essenceResults: AbilityBalanceEssenceResult[];
  finalistEssenceResults: AbilityBalanceEssenceResult[];
  validationResults: AbilityBalanceValidationResult[];
  finalists: AbilityBalanceCombinationResult[];
  randomSeeds: number[];
  finalistMatchups: AbilityBalanceMatchupResult[];
}

export interface AbilityBalanceValidationResult {
  essenceId: string;
  displayName: string;
  replacementEssenceId: string;
  replacementDisplayName: string;
  battles: number;
  originalScore: number;
  replacementScore: number;
  scoreDelta: number;
  contextCount?: number;
  replacementCount?: number;
}

export interface AbilityBalanceCombinationResult {
  signature: string;
  displayName: string;
  participants: AbilityBalanceParticipantLoadout[];
  battles: number;
  wins: number;
  losses: number;
  draws: number;
  winRate: number;
  lossRate: number;
  drawRate: number;
  averageDuration: number;
  averageDamageDone: number;
  averageDamageTaken: number;
  seedResults: AbilityBalanceSeedResult[];
}

export interface AbilityBalanceSeedResult {
  randomSeed: number;
  battles: number;
  score: number;
}

export interface AbilityBalanceMatchupResult {
  firstSignature: string;
  secondSignature: string;
  battles: number;
  firstWins: number;
  secondWins: number;
  draws: number;
  firstScore: number;
}

export interface AbilityBalanceBattleSummary {
  index: number;
  friendlySignature: string;
  friendlyDisplayName: string;
  hostileSignature: string;
  hostileDisplayName: string;
  outcome: string;
  duration: number;
  friendlyDamageDone: number;
  friendlyDamageTaken: number;
  hostileDamageDone: number;
  hostileDamageTaken: number;
}

export interface CombatCharacterProfileGenerationRequest {
  auditId: string;
  audit: AbilityBalanceAuditReport;
  contentType: string;
  equipmentQuality: string;
  teamsPerFamily: number;
  randomSeed: number;
  portfolioMode: string;
  minimumSourceBattles: number;
  minimumMatchupBattles: number;
  maximumConfidenceWidth95: number;
  maximumSeedScoreSpread: number;
  maximumEssenceOverlap: number;
  requireMultiSeedStability: boolean;
  targetTeamSize?: number | null;
  targetEquipmentTier?: number | null;
  targetEquipmentRarity?: string | null;
  targetFloorNumbers?: number[] | null;
  contextQualificationSampleCount?: number;
}

export interface CombatCharacterProfileGenerationReport {
  schemaVersion: number;
  generatorVersion: number;
  powerRatingAlgorithmVersion: number;
  combatRulesVersion: number;
  equipmentBalanceVersion: number;
  canonicalRosterVersion: number;
  auditId: string;
  sourceContentHash: string;
  scenarioId: string;
  contentType: string;
  randomSeed: number;
  teams: CombatCharacterProfileTeam[];
  portfolioMode: string;
  minimumSourceBattles: number;
  minimumMatchupBattles: number;
  maximumConfidenceWidth95: number;
  maximumSeedScoreSpread: number;
  maximumEssenceOverlap: number;
  requireMultiSeedStability: boolean;
  scenario: CombatCharacterProfileScenario | null;
}

export interface CombatCharacterProfileScenario {
  id: string;
  teamSize: number;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentQuality: string;
  auditEquipmentProfile: string;
  essencesPerParticipant: number;
  partySize: number;
  partyCount: number;
  discoveryTeamSize: number;
  floorNumbers?: number[] | null;
}

export interface CombatCharacterProfileBatchGenerationRequest {
  requests: CombatCharacterProfileGenerationRequest[];
}

export interface CombatCharacterProfileBatchGenerationReport {
  requestedScenarioCount: number;
  catalogValidation: CombatCharacterProfileCatalogValidationReport;
}

export interface CombatCharacterProfileTeam {
  id: string;
  family: string;
  sourceSignature: string;
  sourceDisplayName: string;
  sourceBattles: number;
  sourceWins: number;
  sourceLosses: number;
  sourceDraws: number;
  sourceScore: number;
  confidenceLower95: number;
  confidenceUpper95: number;
  profiles: CombatCharacterProfile[];
  selectionReason: string;
  isSyntheticControl: boolean;
  adversarySourceSignature: string | null;
  seedScoreMinimum: number | null;
  seedScoreMaximum: number | null;
  nearestSelectedEssenceOverlap: number | null;
  adversaryBattles: number | null;
  adversaryScore: number | null;
  adversaryConfidenceLower95: number | null;
  adversaryConfidenceUpper95: number | null;
  parties: CombatCharacterProfileParty[];
  isComposedExpedition: boolean;
}

export interface CombatCharacterProfileParty {
  id: string;
  sourcePartyProfileId: string;
  partyNumber: number;
  profileIds: string[];
  evidence: CombatCharacterProfilePartyEvidence;
}

export interface CombatCharacterProfilePartyEvidence {
  family: string;
  sourceSignature: string;
  sourceDisplayName: string;
  sourceBattles: number;
  sourceWins: number;
  sourceLosses: number;
  sourceDraws: number;
  sourceScore: number;
  confidenceLower95: number;
  confidenceUpper95: number;
  selectionReason: string;
  isSyntheticControl: boolean;
  adversarySourceSignature: string | null;
  seedScoreMinimum: number | null;
  seedScoreMaximum: number | null;
  nearestSelectedEssenceOverlap: number;
  adversaryBattles: number | null;
  adversaryScore: number | null;
  adversaryConfidenceLower95: number | null;
  adversaryConfidenceUpper95: number | null;
  contextEvidence?: CombatCharacterProfileContextEvidence[] | null;
}

export interface CombatCharacterProfileContextEvidence {
  scenarioId: string;
  floorNumber: number;
  targetTeamSize: number;
  sampleCount: number;
  wins: number;
  losses: number;
  draws: number;
  winRate: number;
  timeoutRate: number;
  averageDurationTicks: number;
  seedManifestId: string;
  seedManifestHash: string;
  usesProductionRuntime: boolean;
  abilitiesStartOnCooldown: boolean;
}

export interface CombatCharacterProfile {
  id: string;
  teamId: string;
  slotIndex: number;
  name: string;
  family: string;
  role: string;
  contentType: string;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentQuality: string;
  equipmentProfile: string;
  essenceIds: string[];
  rawPowerRating: number;
  displayPowerRating: number;
  prepared: CombatCharacterPreparedPreview;
  partyNumber: number;
  partySlotIndex: number;
  sourcePartyProfileId: string | null;
}

export interface CombatCharacterPreparedPreview {
  isProductionReady: boolean;
  level: number;
  currentHealth: number;
  maxHealth: number;
  attributes: Record<string, number>;
  abilityIds: string[];
  tags: string[];
  essenceIds: string[];
  equipment: CombatCharacterPreparedEquipment[];
}

export interface CombatCharacterPreparedEquipment {
  itemBaseId: string;
  slot: string;
  tier: number;
  rarity: string;
  quality: string;
  recipeId: string | null;
  blueprintId: string | null;
  equipmentSetId: string | null;
}

export interface CombatCharacterProfileCatalogDocument {
  schemaVersion: number;
  catalogVersion: number;
  profileSets: CombatCharacterProfileGenerationReport[];
}

export interface CombatCharacterProfileCatalogValidationReport {
  isValid: boolean;
  currentContentHash: string;
  normalizedCatalog: CombatCharacterProfileCatalogDocument;
  issues: CombatCharacterProfileCatalogValidationIssue[];
}

export interface CombatCharacterProfileCatalogValidationIssue {
  severity: string;
  code: string;
  path: string;
  message: string;
}

export interface WorldTowerProfileWeightPolicy {
  meta: number;
  typical: number;
  roleSpecialist: number;
  resilience: number;
}

export interface WorldTowerProfileShadowCalibrationOptions {
  minimumFloor: number;
  maximumFloor: number;
  sampleCount: number;
  requireExpandedPortfolio: boolean;
  weightPolicy: WorldTowerProfileWeightPolicy;
  baseRandomSeed?: number;
  seedManifestId?: string;
  useSharedCohortSeeds?: boolean;
}

export interface WorldTowerProfileScenarioRequirement {
  scenarioId: string;
  floorNumbers: number[];
  teamSize: number;
  equipmentTier: number;
  equipmentRarity: string;
  equipmentQuality: string;
  auditEquipmentProfile: string;
  essencesPerParticipant: number;
  averagePowerRating: number;
  minimumRecommendedPowerRating: number;
  maximumRecommendedPowerRating: number;
}

export interface WorldTowerProfileShadowCalibrationIssue {
  severity: string;
  code: string;
  floorNumber: number | null;
  message: string;
}

export interface WorldTowerProfileShadowFloorSummary {
  floorNumber: number;
  requiredSlots: number;
  recommendedPowerRating: number;
  selectedAuditId: string | null;
  selectedScenarioId: string | null;
  selectedProfileSetPowerRating: number | null;
  weightedTeamCount: number;
  diagnosticTeamCount: number;
  weightedProfileWinRate: number | null;
  weightedProfileTimeoutRate: number | null;
  canonicalRecommendedWinRate: number;
  winRateDeltaFromCanonicalRecommended: number | null;
}

export interface WorldTowerProfileShadowCalibrationResult {
  floorNumber: number;
  auditId: string;
  sourceContentHash: string;
  scenarioId: string;
  profileSchemaVersion: number;
  generatorVersion: number;
  powerRatingAlgorithmVersion: number;
  combatRulesVersion: number;
  equipmentBalanceVersion: number;
  canonicalRosterVersion: number;
  teamId: string;
  family: string;
  weightBucket: string;
  normalizedPopulationWeight: number;
  rosterSize: number;
  averagePowerRating: number;
  recommendedPowerRating: number;
  sampleCount: number;
  winRate: number;
  timeoutRate: number;
  averageDurationTicks: number;
  closestCanonicalCohort: string;
  closestCanonicalPowerRating: number;
  winRateDeltaFromClosestCanonical: number;
  usesProductionRuntime: boolean;
  abilitiesStartOnCooldown: boolean;
}

export interface WorldTowerProfileShadowCalibrationReport {
  schemaVersion: number;
  status: string;
  recommendationsChanged: boolean;
  catalogContentHash: string;
  catalogVersion: number;
  minimumFloor: number;
  maximumFloor: number;
  sampleCount: number;
  requireExpandedPortfolio: boolean;
  weightPolicy: WorldTowerProfileWeightPolicy;
  canonicalCalibration: unknown;
  profileResults: WorldTowerProfileShadowCalibrationResult[];
  floorSummaries: WorldTowerProfileShadowFloorSummary[];
  issues: WorldTowerProfileShadowCalibrationIssue[];
  catalogSource: string;
  catalogIdentity: string | null;
}

export interface WorldTowerCalibrationCertificationOptions {
  minimumFloor: number;
  maximumFloor: number;
  sampleCount: number;
  minimumSampleCount: number;
  monotonicTolerance: number;
  maximumTimeoutRate: number;
  requireExpandedPortfolio: boolean;
  weightPolicy: WorldTowerProfileWeightPolicy;
  baseRandomSeed: number;
  seedManifestId: string;
}

export interface WorldTowerCalibrationConfidenceInterval {
  estimate: number;
  lower95: number;
  upper95: number;
  effectiveSampleCount: number;
}

export interface WorldTowerCalibrationCohortCertification {
  cohort: string;
  targetMinimumWinRate: number;
  targetMaximumWinRate: number;
  confidence: WorldTowerCalibrationConfidenceInterval;
  hasMinimumSamples: boolean;
  confidenceWithinTarget: boolean;
  passed: boolean;
}

export interface WorldTowerCalibrationPopulationCertification {
  targetMinimumWinRate: number;
  targetMaximumWinRate: number;
  weightedConfidence: WorldTowerCalibrationConfidenceInterval | null;
  teamCount: number;
  qualifyingTeamCount: number;
  teams: WorldTowerCalibrationProfileTeamCertification[];
  winRateSpread: number | null;
  weightedTimeoutRate: number | null;
  hasQualifyingTeam: boolean;
  passed: boolean;
}

export interface WorldTowerCalibrationProfileTeamCertification {
  teamId: string;
  family: string;
  weightBucket: string;
  confidence: WorldTowerCalibrationConfidenceInterval;
  timeoutRate: number;
  hasMinimumSamples: boolean;
  estimateWithinTarget: boolean;
  timeoutWithinLimit: boolean;
  productionContractSatisfied: boolean;
  passed: boolean;
}

export interface WorldTowerCalibrationFloorCertification {
  floorNumber: number;
  isCertified: boolean;
  expectedScenarioId: string | null;
  selectedScenarioId: string | null;
  scenarioMatchesProductionRequirement: boolean;
  canonicalMonotonic: boolean;
  canonicalCohorts: WorldTowerCalibrationCohortCertification[];
  profiles: WorldTowerCalibrationPopulationCertification;
}

export interface WorldTowerCalibrationCertificationIssue {
  severity: string;
  code: string;
  floorNumber: number | null;
  message: string;
}

export interface WorldTowerCalibrationCertificationProvenance {
  inputFingerprint: string;
  canonicalInputHash: string;
  profileInputHash: string;
  catalogContentHash: string;
  catalogVersion: number;
  certificationContractVersion: number;
  seedManifest: {
    id: string;
    baseRandomSeed: number;
    seeds: number[];
    hash: string;
    sharedAcrossCohorts: boolean;
  } | null;
  preparationSchemaVersion: number;
  powerRatingAlgorithmVersion: number;
  combatRulesVersion: number;
  equipmentBalanceVersion: number;
  canonicalRosterVersion: number;
  profileSchemaVersions: number[];
  profileGeneratorVersions: number[];
  serviceAssemblyVersion: string;
  runtime: string;
  runtimeIdentifier: string;
  processArchitecture: string;
  buildConfiguration: string;
  catalogSource: string;
  catalogIdentity: string | null;
}

export interface WorldTowerCalibrationCertificationReport {
  schemaVersion: number;
  status: string;
  isCertified: boolean;
  recommendationsChanged: boolean;
  options: WorldTowerCalibrationCertificationOptions;
  provenance: WorldTowerCalibrationCertificationProvenance;
  floors: WorldTowerCalibrationFloorCertification[];
  issues: WorldTowerCalibrationCertificationIssue[];
  shadowCalibration: WorldTowerProfileShadowCalibrationReport;
}

export interface WorldTowerAuditCampaignOptions {
  minimumFloor: number;
  maximumFloor: number;
  candidatePoolSize: number;
  screeningBattleCount: number;
  finalistCount: number;
  finalistBattleCount: number;
  validationBattleCount: number;
  randomSeeds: number[];
  teamsPerFamily: number;
  profileRandomSeed: number;
  minimumSourceBattles: number;
  minimumMatchupBattles: number;
  maximumConfidenceWidth95: number;
  maximumSeedScoreSpread: number;
  maximumEssenceOverlap: number;
  requireMultiSeedStability: boolean;
  discoveryEquipmentTier: number;
  discoveryEquipmentRarity: string;
  discoveryEquipmentProfile: string;
  runCandidateVerification: boolean;
  smokeSampleCount: number;
  certificationSampleCount: number;
}

export interface WorldTowerAuditCampaignWork {
  id: string;
  description: string;
  request: AbilityBalanceAuditRequest;
  scenarioIds: string[];
  status: string;
  attemptCount: number;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  totalBattlesRun: number | null;
  contentHash: string | null;
  error: string | null;
  reusedFromCampaignId: string | null;
  reusedSourceContentHash: string | null;
}

export interface WorldTowerAuditCampaignScenario {
  requirement: WorldTowerProfileScenarioRequirement;
  auditWorkId: string;
}

export interface WorldTowerAuditCampaign {
  schemaVersion: number;
  id: string;
  status: string;
  options: WorldTowerAuditCampaignOptions;
  createdAtUtc: string;
  updatedAtUtc: string;
  scenarios: WorldTowerAuditCampaignScenario[];
  audits: WorldTowerAuditCampaignWork[];
  cancelRequested: boolean;
  catalogIsValid: boolean;
  catalogProfileSetCount: number;
  catalogIssueCount: number;
  catalogContentHash: string | null;
  error: string | null;
  discoveryFingerprint: string | null;
  materializationFingerprint: string | null;
  reusedCatalogFromCampaignId: string | null;
  reusedAuditCount: number;
  candidateSmokePassed: boolean;
  candidateCertificationCompleted: boolean;
  candidateCertificationPassed: boolean;
  candidateCertificationIssueCount: number;
  completedAuditCount: number;
  totalAuditCount: number;
  currentAuditId: string | null;
  isPromotionReady: boolean;
}

export interface WorldTowerAuditCampaignEvidence {
  campaign: WorldTowerAuditCampaign;
  auditReports: Record<string, AbilityBalanceAuditReport>;
  catalogValidation: CombatCharacterProfileCatalogValidationReport | null;
  candidateSmoke: WorldTowerProfileShadowCalibrationReport | null;
  candidateCertification: WorldTowerCalibrationCertificationReport | null;
}
