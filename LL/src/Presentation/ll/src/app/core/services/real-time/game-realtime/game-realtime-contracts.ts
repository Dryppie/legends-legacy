import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { MarketplaceChangeSet } from '../../../../shared/models/Dtos/market-place/marketplace-change-set';
import { EquipmentInstance } from '../../../../shared/models/item';
import { QuestJournal } from '../../../../shared/models/quest';
import { TournamentGroundsUpdated } from '../colosseum/tournament-grounds-updated';
import { RaidUpdated } from '../raid/raid-updated';
import { RaidDirectoryUpdated } from '../raid/raid-directory-updated';
import { WorldTowerCombatFrameUpdated } from '../world-tower/world-tower-combat-frame-updated';
import { WorldTowerRallyUpdated } from '../world-tower/world-tower-rally-updated';
import {
  StateSyncScope,
  StateVersionMap,
} from './state-sync-scopes.generated';

export {
  isStateSyncScope,
  stateSyncScopes,
  StateSyncScope,
  StateVersionMap,
} from './state-sync-scopes.generated';

export const gameRealtimeSignalEventNames = {
  accountAccessChanged: 'AccountAccessChanged',
  characterLevelUp: 'CharacterLevelUp',
  marketplaceChanged: 'MarketplaceChanged',
  guildApplication: 'GuildApplication',
  guildInviteReceived: 'GuildInviteReceived',
  guildInviteRejected: 'GuildInviteRejected',
  guildApplicationRejected: 'GuildApplicationRejected',
  guildBuildingsChanged: 'GuildBuildingsChanged',
  guildMissionsChanged: 'GuildMissionsChanged',
  guildStateChanged: 'GuildStateChanged',
  guildVaultChatMessage: 'GuildVaultChatMessage',
  guildMembershipChanged: 'GuildMembershipChanged',
  guildDisbanded: 'GuildDisbanded',
  guildDirectoryChanged: 'GuildDirectoryChanged',
  questJournalChanged: 'QuestJournalChanged',
  eventQuestChanged: 'EventQuestChanged',
  arenaBattleCompleted: 'ArenaBattleCompleted',
  prophecyProgressed: 'ProphecyProgressed',
  achievementUnlocked: 'AchievementUnlocked',
  playerTransfer: 'PlayerTransfer',
  tournamentGroundsUpdated: 'TournamentGroundsUpdated',
  worldTowerRallyUpdated: 'WorldTowerRallyUpdated',
  worldTowerCombatFrameUpdated: 'WorldTowerCombatFrameUpdated',
  raidUpdated: 'RaidUpdated',
  raidDirectoryUpdated: 'RaidDirectoryUpdated',
} as const;

export const gameRealtimeEventNames = {
  lootReceived: 'LootReceived',
  stateInvalidated: 'StateInvalidated',
  stateInvalidations: 'StateInvalidations',
  ...gameRealtimeSignalEventNames,
} as const;

export type GameRealtimeEventName =
  (typeof gameRealtimeEventNames)[keyof typeof gameRealtimeEventNames];

export interface GameRealtimeEnvelope<TPayload = unknown> {
  updateId?: string;
  occurredAt?: string;
  event: GameRealtimeEventName | string;
  payload: TPayload;
}

export interface LootReceived {
  characterId: string;
  items: InventoryItem[];
  source: string;
  location?: string | null;
  grantId?: string | null;
}

export interface StateInvalidations {
  characterId: string;
  revisions: StateVersionMap;
  reason: string;
}

export interface AccountAccessChanged {
  accountId: string;
  reason: string;
  occurredAtUtc: string;
}

export interface CharacterLevelUp {
  characterId: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
  unlockedEssenceSlots: number;
}

export interface MarketplaceChanged {
  changes: MarketplaceChangeSet;
}

export interface GuildApplication {
  guildId: string;
  playerId: string;
}

export interface GuildInviteReceived {
  guildId: string;
  characterId: string;
}

export interface GuildInviteRejected {
  guildId: string;
  characterId: string;
}

export interface GuildApplicationRejected {
  guildId: string;
  characterId: string;
}

export interface GuildBuildingsChanged {
  guildId: string;
  buildingId: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
}

export interface GuildMissionsChanged {
  guildId: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
}

export interface GuildStateChanged {
  guildId: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
}

export interface GuildVaultChatMessage {
  guildId: string;
  messageId: string;
  actorCharacterId: string;
  actorName: string;
  action: 'donated' | 'withdrew';
  equipment: EquipmentInstance;
  sentAt: string;
}

export interface GuildMembershipChanged {
  guildId: string;
  characterId: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
}

export interface GuildDisbanded {
  guildId: string;
  actorCharacterId?: string;
  initiatorHandled?: boolean;
}

export interface GuildDirectoryChanged {
  reason: string;
  actorCharacterId?: string;
}

export interface QuestJournalChanged {
  journal: QuestJournal;
  stateVersion: number;
}

export interface EventQuestChanged {
  eventQuestId: string;
  updatedAt: string;
}

export interface ArenaBattleCompleted {
  characterId: string;
  enemyId: string;
  outcome: string;
  characterRatingBefore: number;
  characterRatingAfter: number;
  enemyRatingBefore: number;
  enemyRatingAfter: number;
}

export interface ProphecyProgressed {
  characterId: string;
  prophecyId: string;
  title: string;
  scope: string;
  slotType: string;
  status: string;
  currentValue: number;
  targetValue: number;
  amountGained: number;
  completed: boolean;
}

export interface AchievementUnlocked {
  characterId?: string | null;
  achievementKey: string;
  achievementName: string;
  points: number;
  titleKey?: string | null;
  titleName?: string | null;
  message: string;
  isGlobal: boolean;
}

export interface PlayerTransfer {
  transferId: string;
  messageId: string;
  characterId: string;
  message: string;
}

export interface GameRealtimeSignalEventMap {
  AccountAccessChanged: AccountAccessChanged;
  CharacterLevelUp: CharacterLevelUp;
  MarketplaceChanged: MarketplaceChanged;
  GuildApplication: GuildApplication;
  GuildInviteReceived: GuildInviteReceived;
  GuildInviteRejected: GuildInviteRejected;
  GuildApplicationRejected: GuildApplicationRejected;
  GuildBuildingsChanged: GuildBuildingsChanged;
  GuildMissionsChanged: GuildMissionsChanged;
  GuildStateChanged: GuildStateChanged;
  GuildVaultChatMessage: GuildVaultChatMessage;
  GuildMembershipChanged: GuildMembershipChanged;
  GuildDisbanded: GuildDisbanded;
  GuildDirectoryChanged: GuildDirectoryChanged;
  QuestJournalChanged: QuestJournalChanged;
  EventQuestChanged: EventQuestChanged;
  ArenaBattleCompleted: ArenaBattleCompleted;
  ProphecyProgressed: ProphecyProgressed;
  AchievementUnlocked: AchievementUnlocked;
  PlayerTransfer: PlayerTransfer;
  TournamentGroundsUpdated: TournamentGroundsUpdated;
  WorldTowerRallyUpdated: WorldTowerRallyUpdated;
  WorldTowerCombatFrameUpdated: WorldTowerCombatFrameUpdated;
  RaidUpdated: RaidUpdated;
  RaidDirectoryUpdated: RaidDirectoryUpdated;
}

export type GameRealtimeSignalEventName = keyof GameRealtimeSignalEventMap;

export function isGameRealtimeSignalEventName(
  name: string,
): name is GameRealtimeSignalEventName {
  return Object.values(gameRealtimeSignalEventNames).includes(
    name as GameRealtimeSignalEventName,
  );
}

export interface StateInvalidated {
  characterId?: string | null;
  scope: StateSyncScope;
  revision: number;
  reason: string;
}

export interface StateSyncCheckpoint {
  characterId: string;
  revisions: StateVersionMap;
  serverTimeUtc: string;
}

export type GameRealtimePayload =
  | LootReceived
  | AccountAccessChanged
  | CharacterLevelUp
  | MarketplaceChanged
  | GuildApplication
  | GuildInviteReceived
  | GuildInviteRejected
  | GuildApplicationRejected
  | GuildBuildingsChanged
  | GuildMissionsChanged
  | GuildStateChanged
  | GuildVaultChatMessage
  | GuildMembershipChanged
  | GuildDisbanded
  | GuildDirectoryChanged
  | QuestJournalChanged
  | EventQuestChanged
  | ArenaBattleCompleted
  | ProphecyProgressed
  | AchievementUnlocked
  | PlayerTransfer
  | TournamentGroundsUpdated
  | WorldTowerRallyUpdated
  | WorldTowerCombatFrameUpdated
  | RaidUpdated
  | RaidDirectoryUpdated
  | StateInvalidated
  | StateInvalidations;
