import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export const gameRealtimeEventNames = {
  dungeonRewardsClaimed: 'DungeonRewardsClaimed',
  lootReceived: 'LootReceived',
  inventorySnapshot: 'InventorySnapshot',
  characterSnapshot: 'CharacterSnapshot',
  stateInvalidated: 'StateInvalidated',
} as const;

export type GameRealtimeEventName =
  (typeof gameRealtimeEventNames)[keyof typeof gameRealtimeEventNames];

export interface GameRealtimeEnvelope<TPayload = unknown> {
  updateId?: string;
  occurredAt?: string;
  event: GameRealtimeEventName | string;
  payload: TPayload;
}

export interface DungeonRewardsClaimed {
  characterId: string;
  claimedLoot: InventoryItem[];
  location?: string | null;
}

export interface LootReceived {
  characterId: string;
  items: InventoryItem[];
  source: string;
  location?: string | null;
  grantId?: string | null;
}

export interface InventorySnapshot {
  characterId: string;
  items: InventoryItem[];
  reason: string;
}

export interface CharacterSnapshot {
  characterId: string;
  character: CharacterDto;
  reason: string;
}

export type StateSyncScope = 'character' | 'marketplace' | string;

export interface StateInvalidated {
  characterId?: string | null;
  scope: StateSyncScope;
  revision: number;
  reason: string;
}

export interface StateSyncCheckpoint {
  characterId: string;
  revisions: Record<string, number>;
  serverTimeUtc: string;
}

export type GameRealtimePayload =
  | DungeonRewardsClaimed
  | LootReceived
  | InventorySnapshot
  | CharacterSnapshot
  | StateInvalidated;
