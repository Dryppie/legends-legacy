import { InventoryItem } from './inventoryItem';

export interface LootHistoryEntry {
  id: string;
  item: InventoryItem;
  source: string;
  receivedAt: string;
}
