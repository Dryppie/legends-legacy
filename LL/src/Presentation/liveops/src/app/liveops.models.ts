export interface ApiResponse<T> {
  isSuccess: boolean;
  data: T | null;
  errorMessage: string;
}

export interface OperatorSession {
  subject: string;
  displayName: string;
  permissions: string[];
  environment: string;
  isDevelopmentOperator: boolean;
}

export interface PlayerSummary {
  accountId: string;
  characterId: string;
  accountLabel: string;
  email: string | null;
  characterName: string;
  characterLevel: number;
  createdUtc: string;
  activeBanId: string | null;
  activeBanReason: string | null;
  activeBanExpiresAt: string | null;
  activeMultiplayerRestrictionId: string | null;
  activeMultiplayerRestrictionReason: string | null;
  activeMultiplayerRestrictionExpiresAt: string | null;
}

export interface ChatRestriction {
  id: string;
  characterId: string;
  reason: string;
  createdBySubject: string;
  createdAt: string;
  expiresAt: string | null;
}

export interface AdministrationHistory {
  operationId: string;
  actionType: string;
  permission: string;
  actorSubject: string;
  actorDisplayName: string;
  targetResourceId: string | null;
  reason: string;
  internalNotes: string | null;
  detailsJson: string;
  occurredAt: string;
}

export interface ChatHistory {
  operationId: string;
  actionType: string;
  restrictionId: string;
  actorSubject: string;
  actorDisplayName: string;
  reason: string;
  occurredAt: string;
}

export interface PlayerDetails {
  player: PlayerSummary;
  activeMute: ChatRestriction | null;
  chatAvailable: boolean;
  chatStatusMessage: string | null;
  administrationHistory: AdministrationHistory[];
  chatHistory: ChatHistory[];
}

export interface ItemCatalogEntry {
  id: string;
  name: string;
  description: string;
  itemType: string;
  rarity: string;
  stackable: boolean;
  isBound: boolean;
}

export interface TimelineEntry {
  operationId: string;
  actionType: string;
  actorDisplayName: string;
  reason: string;
  occurredAt: string;
  source: 'Game' | 'Chat';
}

export interface SupportSection<T> {
  isAvailable: boolean;
  source: string;
  fetchedAtUtc: string;
  message: string | null;
  data: T | null;
}

export interface AccountRestrictionHistory {
  id: string;
  type: string;
  status: 'Active' | 'Expired' | 'Revoked';
  reason: string;
  createdBySubject: string;
  createdAt: string;
  expiresAt: string | null;
  revokedBySubject: string | null;
  revokedAt: string | null;
  revocationReason: string | null;
}

export interface AccountSupportSnapshot {
  accountCreatedUtc: string;
  lastSessionIssuedUtc: string | null;
  activeSessionCount: number;
  loginActivityMessage: string;
  restrictions: AccountRestrictionHistory[];
}

export interface ActivitySupportSnapshot {
  currentAction: string;
  actionDetailType: string | null;
  lastActionMutationAtUtc: string | null;
  nextResolutionAtUtc: string | null;
  blockedUntilUtc: string | null;
  scheduleGeneration: number | null;
  activityMessage: string;
}

export interface RecentInventoryAcquisition {
  itemInstanceId: string;
  itemBaseId: string;
  itemName: string;
  quantity: number;
  acquisitionSource: string;
  acquiredAtUtc: string;
}

export interface RecentCompensationGrant {
  operationId: string;
  itemBaseId: string;
  itemName: string;
  quantity: number;
  reason: string;
  riskLevel: string;
  occurredAtUtc: string;
}

export interface EconomySupportSnapshot {
  cinders: number;
  soulstones: number;
  fateEcho: number;
  sigilFragments: number;
  guildFavor: number;
  towerTokens: number;
  inventoryRowCount: number;
  inventoryQuantity: number;
  unseenInventoryRows: number;
  recentAcquisitions: RecentInventoryAcquisition[];
  recentCompensationGrants: RecentCompensationGrant[];
}

export interface GuildSupportSnapshot {
  isMember: boolean;
  guildId: string | null;
  guildName: string | null;
  guildTag: string | null;
  role: string | null;
  joinedAtUtc: string | null;
  guildLevel: number | null;
  memberCount: number | null;
}

export interface RecentMarketplaceTrade {
  orderId: string;
  direction: string;
  itemBaseId: string;
  itemName: string;
  quantity: number;
  totalPrice: number;
  purchasedAtUtc: string;
}

export interface MarketplaceSupportSnapshot {
  activeListingCount: number;
  activeBuyOrderCount: number;
  recentTrades: RecentMarketplaceTrade[];
}

export interface PlayerTransferHistory {
  transferId: string;
  direction: 'Incoming' | 'Outgoing' | 'BetweenOwnCharacters';
  kind: 'Cinders' | 'InventoryItem';
  senderAccountId: string;
  senderCharacterId: string;
  senderCharacterName: string;
  recipientAccountId: string;
  recipientCharacterId: string;
  recipientCharacterName: string;
  assetId: string;
  assetName: string;
  sourceItemInstanceId: string | null;
  destinationItemInstanceId: string | null;
  quantity: number;
  occurredAtUtc: string;
  conversation: TransferConversationSummary;
}

export type TransferConversationStatus =
  | 'EstablishedConversation'
  | 'OneWayConversation'
  | 'SharedChannelActivity'
  | 'NoRecordedConversation'
  | 'ChatUnavailable';

export interface TransferConversationSummary {
  status: TransferConversationStatus;
  isAvailable: boolean;
  message: string | null;
  senderToRecipientMessageCount: number;
  recipientToSenderMessageCount: number;
  immediateMessageCount: number;
  firstMessageAt: string | null;
  lastMessageAt: string | null;
  sharedChannelCount: number;
  sharedChannelMessageCount: number;
  windowFrom: string;
  windowTo: string;
}

export interface TransferConversationMessage {
  id: string;
  senderId: string;
  senderName: string;
  body: string;
  targetCharacterId: string | null;
  targetCharacterName: string | null;
  sentAt: string;
}

export interface TransferConversationPage {
  transferId: string;
  summary: TransferConversationSummary;
  messages: TransferConversationMessage[];
  nextCursor: string | null;
}

export interface TransferHistorySupportSnapshot {
  historyLimit: number;
  entries: PlayerTransferHistory[];
  nextCursor: string | null;
}

export interface StateRevision {
  scope: string;
  revision: number;
  updatedAtUtc: string;
}

export interface SynchronizationSupportSnapshot {
  pendingDeliveries: number;
  failedDeliveries: number;
  oldestPendingAtUtc: string | null;
  lastOutboxEventAtUtc: string | null;
  revisions: StateRevision[];
  pendingRewardMessage: string;
}

export interface EquipmentSupportDescriptor {
  definitionId: string; archetypeId: string; tier: number; rank: number; balanceVersion: number;
  rarity: string; nativeStyleId: string | null; activeStyleId: string | null;
  ownership: string; ownerId: string; awardKind: string; sourceId: string; awardId: string;
  baseSalvageScrap: number; paidScrap: number; paidCinders: number;
  investments: { operationId: string; rank: number; scrap: number; cinders: number }[];
}

export interface EquipmentSupportItem {
  instanceId: string; itemBaseId: string; name: string; locations: string[];
  progression: EquipmentSupportDescriptor | null;
}

export interface EquipmentSupportDungeonRun {
  runId: string; dungeonId: string; name: string; status: string; currentRoomIndex: number;
  createdAtUtc: string; completedAtUtc: string | null; rewardsClaimedAtUtc: string | null;
  commitment: {
    characterId: string; runId: string; dungeonId: string; poolId: string; difficulty: number;
    matchingChance: number; guaranteeCompletions: number; completionScrap: number;
    target: EquipmentSupportItem | null;
  } | null;
  receipt: {
    runId: string; poolId: string; securedAtUtc: string; claimedAtUtc: string | null;
    previousProgress: number; progress: number; scrap: number; equipment: EquipmentSupportItem | null;
  } | null;
  rewardRowCount: number;
  rewardRows: {
    rewardRowId: string; itemBaseId: string; name: string; itemType: string;
    quantity: number; source: string; equipment: EquipmentSupportItem | null;
  }[];
}

export interface EquipmentSupportSnapshot {
  dungeonRun?: EquipmentSupportDungeonRun | null;
  rowLimit: number; equipmentCount: number; pendingRewardCount: number; progressTruncated: boolean;
  items: EquipmentSupportItem[];
  pendingRewards: { runId: string; poolId: string; securedAtUtc: string; scrap: number; equipment: EquipmentSupportItem | null }[];
  protection: { poolId: string; targetDefinitionId: string | null; completionsWithoutMatch: number; revision: number }[];
  ordinary: {
    poolId: string; hasEnteredRegion: boolean; targetDefinitionId: string | null; plainVictories: number;
    requiredPlainVictories: number | null; sigilFamilyId: string | null; sigilVictories: number;
    requiredSigilVictories: number | null; scrapRemainder: number; revision: number; lastEncounterAtUtc: string | null;
  }[];
  learnedStyles: { styleId: string; learnedAtUtc: string; freeApplicationOperationId: string | null }[];
}

export interface PlayerSupportSnapshot {
  accountId: string;
  characterId: string;
  generatedAtUtc: string;
  account: SupportSection<AccountSupportSnapshot>;
  activity: SupportSection<ActivitySupportSnapshot>;
  economy: SupportSection<EconomySupportSnapshot>;
  guild: SupportSection<GuildSupportSnapshot>;
  marketplace: SupportSection<MarketplaceSupportSnapshot>;
  transfers: SupportSection<TransferHistorySupportSnapshot>;
  synchronization: SupportSection<SynchronizationSupportSnapshot>;
  equipment?: SupportSection<EquipmentSupportSnapshot>;
}

export interface ActionPreviewField {
  label: string;
  value: string;
}

export interface ActionPreview {
  previewToken: string;
  operationId: string;
  actionKind: string;
  title: string;
  targetName: string;
  targetId: string;
  riskLevel: 'Normal' | 'Permanent' | 'HighValue';
  expiresAt: string;
  confirmationText: string | null;
  fields: ActionPreviewField[];
  warnings: string[];
}

export interface AdministrationAuditEntry {
  operationId: string;
  source: 'Game' | 'Chat';
  actionType: string;
  permission: string;
  actorSubject: string;
  actorDisplayName: string;
  targetAccountId: string | null;
  targetCharacterId: string | null;
  targetResourceId: string | null;
  reason: string;
  internalNotes: string | null;
  detailsJson: string;
  riskLevel: 'Normal' | 'Permanent' | 'HighValue';
  outcome: 'Completed';
  occurredAt: string;
}

export interface PlayerMessageHistoryEntry {
  id: string;
  channelType: string;
  contextKey: string;
  body: string;
  targetCharacterId: string | null;
  targetCharacterName: string | null;
  sentAt: string;
}

export interface PlayerMessageHistoryPage {
  entries: PlayerMessageHistoryEntry[];
  nextCursor: string | null;
}

export interface AdministrationAuditPage {
  entries: AdministrationAuditEntry[];
  nextCursor: string | null;
  unavailableSources: string[];
}

export interface AdministrationAuditFilters {
  from?: string;
  to?: string;
  source?: string;
  actionType?: string;
  actor?: string;
  permission?: string;
  reference?: string;
  riskLevel?: string;
  target?: string;
  operationId?: string;
}

export interface OperationalDependencyStatus {
  key: string;
  name: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unavailable';
  message: string;
  affectedCapabilities: string[];
}

export interface OperationalBuildStatus {
  releaseVersion: string;
  frontendVersion: string;
  gameVersion: string;
  chatVersion: string;
  commitSha: string | null;
  deployedAtUtc: string | null;
  processStartedAtUtc: string;
}

export interface OperationalOutboxStatus {
  isAvailable: boolean;
  status: 'Healthy' | 'Degraded' | 'Unavailable';
  pendingDeliveries: number;
  failedDeliveries: number;
  oldestPendingAtUtc: string | null;
}

export interface OperationalRestrictionStatus {
  isAvailable: boolean;
  expiringWithinSevenDays: number;
  nextExpiryAtUtc: string | null;
}

export interface OperationalStatus {
  overallStatus: 'Healthy' | 'Degraded' | 'Unhealthy';
  environment: string;
  serverTimeUtc: string;
  build: OperationalBuildStatus;
  dependencies: OperationalDependencyStatus[];
  outbox: OperationalOutboxStatus;
  restrictions: OperationalRestrictionStatus;
  permanentActionsLast24Hours: number;
  highValueActionsLast24Hours: number;
  recentActions: AdministrationAuditEntry[];
  warnings: string[];
}

export type AccountRiskSeverity = 'Low' | 'Moderate' | 'High' | 'Critical';
export type AccountRiskSignalType = 'IncomingConcentration' | 'OneSidedRelationship' | 'OneSidedItemTransfer' | 'IncomingItemFunnel' | 'ItemQuantityConsolidation' | 'YoungItemSourceNetwork' | 'YoungItemCoordinationNetwork' | 'FeederNetwork' | 'YoungAccountOutflow' | 'EphemeralItemOutflow' | 'CircularTransfer';
export type AccountInvestigationStatus = 'Unreviewed' | 'Investigating' | 'Watchlisted' | 'Cleared' | 'ConfirmedAbuse' | 'Actioned';

export interface AccountRiskSummary {
  accountId: string;
  characterId: string;
  accountLabel: string;
  characterName: string;
  characterLevel: number;
  accountCreatedUtc: string;
  lastSessionUtc: string | null;
  score: number;
  severity: AccountRiskSeverity;
  primarySignalType: AccountRiskSignalType | null;
  primaryReason: string;
  connectedAccountCount: number;
  incomingCinders: number;
  outgoingCinders: number;
  transferCount: number;
  firstFlaggedAt: string | null;
  lastTriggeredAt: string | null;
  evaluatedAt: string;
  evaluationVersion: number;
  analysisWindowStart: string;
  evidenceComplete: boolean;
  analyzedTransferCount: number;
  investigationStatus: AccountInvestigationStatus;
}

export interface AccountRiskPage {
  entries: AccountRiskSummary[];
  total: number;
  counts: Partial<Record<AccountRiskSeverity, number>>;
  lastEvaluatedAt: string | null;
  firstEvidenceAt: string | null;
  directTransferCount: number;
  directItemTransferCount: number;
  evaluatedAccountCount: number;
  eligibleAccountCount: number;
  upToDateAccountCount: number;
  pendingEvaluationCount: number;
  incompleteEvaluationCount: number;
  evaluationVersion: number;
  lookbackDays: number;
  page: number;
  pageSize: number;
}

export interface AccountRiskSignal {
  type: AccountRiskSignalType;
  category: string;
  contribution: number;
  title: string;
  explanation: string;
  evidence: Record<string, number>;
  supportingTransferIds: string[];
  firstObservedAt: string | null;
  lastObservedAt: string | null;
  supportingTransferCount: number;
  supportingEvidenceComplete: boolean;
  correlationFamily: string;
}

export interface AccountRiskRelationship {
  accountId: string;
  characterId: string;
  characterName: string;
  relationship: string;
  sentToSubject: number;
  receivedFromSubject: number;
  transactionCount: number;
  youngAccount: boolean;
  riskScore: number | null;
  riskSeverity: AccountRiskSeverity | null;
  itemTransfersToSubject: number;
  itemTransfersFromSubject: number;
}

export interface AccountRiskTransfer {
  transferId: string;
  direction: 'Incoming' | 'Outgoing';
  kind: 'Cinders' | 'InventoryItem';
  counterpartyAccountId: string;
  counterpartyCharacterId: string;
  counterpartyCharacterName: string;
  assetId: string;
  assetName: string;
  quantity: number;
  occurredAt: string;
}

export interface AccountRiskHistoryPoint {
  id: string;
  score: number;
  severity: AccountRiskSeverity;
  evaluatedAt: string;
  evaluationVersion: number;
  analysisWindowStart: string;
  evidenceComplete: boolean;
  analyzedTransferCount: number;
}

export interface AccountRiskNote {
  id: string;
  actorSubject: string;
  actorDisplayName: string;
  body: string;
  createdAt: string;
}

export interface AccountRiskDetails {
  account: AccountRiskSummary;
  signals: AccountRiskSignal[];
  relationships: AccountRiskRelationship[];
  transfers: AccountRiskTransfer[];
  totalRetainedTransferCount: number;
  history: AccountRiskHistoryPoint[];
  notes: AccountRiskNote[];
}

export type AccountTemporalCorrelationAssessment = 'InsufficientData' | 'NoMaterialCorrelation' | 'Low' | 'Moderate' | 'High';

export interface AccountTemporalCorrelationMatch {
  subjectChainStartedAt: string;
  relatedChainStartedAt: string;
  deltaMinutes: number;
  sequence: 'SubjectThenRelated' | 'RelatedThenSubject';
  nearbyTransferIds: string[];
}

export interface AccountTemporalCorrelation {
  relatedAccountId: string;
  relatedCharacterId: string;
  relatedCharacterName: string;
  assessment: AccountTemporalCorrelationAssessment;
  summary: string;
  subjectChainStartCount: number;
  relatedChainStartCount: number;
  subjectActiveDays: number;
  relatedActiveDays: number;
  sharedActiveDays: number;
  activeDaySimilarity: number;
  nearStartMatchCount: number;
  strongNearStartMatchCount: number;
  repeatedMatchDays: number;
  matchLift: number;
  hourOfWeekSimilarity: number;
  transferAdjacentMatchCount: number;
  firstObservedAt: string | null;
  lastObservedAt: string | null;
  windowStart: string;
  evaluatedAt: string;
  evidenceComplete: boolean;
  analyzedTokenCount: number;
  analyzedTransferCount: number;
  analysisVersion: number;
  matches: AccountTemporalCorrelationMatch[];
  limitations: string[];
}

export interface AccountTemporalCorrelationReport {
  accountId: string;
  windowStart: string;
  evaluatedAt: string;
  evidenceComplete: boolean;
  analyzedTokenCount: number;
  analyzedTransferCount: number;
  analysisVersion: number;
  entries: AccountTemporalCorrelation[];
}

export type TransferConversationCorrelationAssessment =
  | 'ChatUnavailable'
  | 'RecordedBidirectionalConversation'
  | 'UncommunicativeValueTransferPattern'
  | 'BelowPatternThreshold';

export interface TransferConversationCorrelationEntry {
  counterpartyAccountId: string;
  counterpartyCharacterId: string;
  counterpartyCharacterName: string;
  assessment: TransferConversationCorrelationAssessment;
  meetsPatternThreshold: boolean;
  explanation: string;
  transferCount: number;
  incomingTransferCount: number;
  outgoingTransferCount: number;
  cinderValue: number;
  incomingCinders: number;
  outgoingCinders: number;
  itemTransferCount: number;
  establishedConversationCount: number;
  oneWayConversationCount: number;
  sharedChannelActivityCount: number;
  noRecordedConversationCount: number;
  immediateMessageCount: number;
  firstTransferAt: string;
  lastTransferAt: string;
  supportingTransferIds: string[];
}

export interface TransferConversationCorrelationReport {
  accountId: string;
  windowStart: string;
  evaluatedAt: string;
  evidenceComplete: boolean;
  analyzedTransferCount: number;
  unavailableConversationCount: number;
  entries: TransferConversationCorrelationEntry[];
}

export interface AccountRiskFilters {
  search?: string;
  minimumSeverity?: string;
  signalType?: string;
  status?: string;
  minimumScore?: string;
  maximumAccountAgeDays?: string;
  lastTriggeredAfter?: string;
  sort?: string;
}

export interface AccountRiskOperation {
  operationId: string;
  wasAlreadyProcessed: boolean;
  status: AccountInvestigationStatus | null;
  note: AccountRiskNote | null;
}

export interface CompensationEquipmentOption {
  definitionId: string;
  name: string;
  itemBaseId: string;
  archetypeId: string;
  minimumTier: number;
  maximumTier: number;
  nativeStyleId: string | null;
  compatibleStyleIds: string[];
}

export interface CompensationEquipmentOptions {
  usesEquipmentProgression: boolean;
  maximumQuantity: number;
  options: CompensationEquipmentOption[];
}
