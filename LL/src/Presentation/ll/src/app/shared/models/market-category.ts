import { ItemType } from './enums/itemType';

export type MarketCategoryId =
  | 'resources'
  | 'catalysts'
  | 'equipment'
  | 'essences';

export interface MarketCategorySelection {
  id: MarketCategoryId;
  label: string;
  itemType: ItemType;
  subcategory: string | null;
}
