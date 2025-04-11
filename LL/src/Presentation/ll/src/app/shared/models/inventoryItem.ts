import { ItemInstance } from './item';

export interface InventoryItem {
  id: string;
  itemInstance: ItemInstance;
  quantity?: number;
}
