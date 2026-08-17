import { ItemInstance } from './item';

export interface InventoryItem {
  id: string;
  itemInstance: ItemInstance;
  quantity: number;
  /** Whether the current character marked this inventory row as a favorite. */
  isFavorite?: boolean;
  /** A crafted item the character has not inspected yet. Server-owned. */
  isNew?: boolean;
}
