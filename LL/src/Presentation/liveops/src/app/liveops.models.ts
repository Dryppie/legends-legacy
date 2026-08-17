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
