import { InventoryItem } from '../../../shared/models/inventoryItem';
import { LootReceivedMsg } from './loot/loot-received';

export interface ServerEnvelope<Type extends string, V> {
  type: Type;
  payload: V;
}

export type LootEnvelope /*  { type:"loot"; payload: LootReceivedMsg } */ =
  ServerEnvelope<'loot', InventoryItem[]>;

export type Incoming = LootEnvelope;
