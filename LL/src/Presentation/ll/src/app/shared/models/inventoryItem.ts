import { Item } from './item';

export interface InventoryItem {
  id: string;
  item: Item;
  quantity?: number;
}
