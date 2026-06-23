import { CharacterActionDto } from '../../../shared/models/Dtos/characterActionDto';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { InventoryItem } from '../../../shared/models/inventoryItem';

export const gameRealtimeEventNamesV2 = {
  dungeonRewardsClaimed: 'DungeonRewardsClaimedV2',
  lootReceived: 'LootReceivedV2',
  inventorySnapshot: 'InventorySnapshotV2',
  characterSnapshot: 'CharacterSnapshotV2',
  idleCombatProcessed: 'IdleCombatProcessedV2',
} as const;

export type GameRealtimeEventNameV2 =
  (typeof gameRealtimeEventNamesV2)[keyof typeof gameRealtimeEventNamesV2];

export interface GameRealtimeEnvelopeV2<TPayload = unknown> {
  updateId?: string;
  occurredAt?: string;
  event: GameRealtimeEventNameV2 | string;
  payload: TPayload;
}

export interface DungeonRewardsClaimedV2 {
  characterId: string;
  claimedLoot: InventoryItem[];
}

export interface LootReceivedV2 {
  characterId: string;
  items: InventoryItem[];
  source: string;
}

export interface InventorySnapshotV2 {
  characterId: string;
  items: InventoryItem[];
  reason: string;
}

export interface CharacterSnapshotV2 {
  characterId: string;
  character: CharacterDto;
  reason: string;
}

export interface IdleCombatProcessedV2 {
  characterId: string;
  action: CharacterActionDto;
}

export type GameRealtimePayloadV2 =
  | DungeonRewardsClaimedV2
  | LootReceivedV2
  | InventorySnapshotV2
  | CharacterSnapshotV2
  | IdleCombatProcessedV2;
