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
export type AccountRiskSignalType = 'IncomingConcentration' | 'OneSidedRelationship' | 'OneSidedItemTransfer' | 'FeederNetwork' | 'YoungAccountOutflow' | 'CircularTransfer';
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
  history: AccountRiskHistoryPoint[];
  notes: AccountRiskNote[];
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
