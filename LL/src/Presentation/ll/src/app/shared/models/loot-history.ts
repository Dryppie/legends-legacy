import { InventoryItem } from './inventoryItem';

export interface LootHistoryEntry {
  id: string;
  item: InventoryItem;
  source: string;
  location?: string | null;
  receivedAt: string;
}
