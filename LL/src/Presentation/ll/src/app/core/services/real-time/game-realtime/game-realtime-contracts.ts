import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export const gameRealtimeEventNames = {
  dungeonRewardsClaimed: 'DungeonRewardsClaimed',
  lootReceived: 'LootReceived',
  inventorySnapshot: 'InventorySnapshot',
  characterSnapshot: 'CharacterSnapshot',
  idleCombatProcessed: 'IdleCombatProcessed',
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
}

export interface LootReceived {
  characterId: string;
  items: InventoryItem[];
  source: string;
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

export interface IdleCombatProcessed {
  characterId: string;
  action: CharacterActionDto;
}

export type GameRealtimePayload =
  | DungeonRewardsClaimed
  | LootReceived
  | InventorySnapshot
  | CharacterSnapshot
  | IdleCombatProcessed;
