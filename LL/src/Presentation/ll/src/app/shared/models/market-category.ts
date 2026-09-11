import { ItemType } from './enums/itemType';

export type MarketCategoryId =
  | 'monster-cores'
  | 'blueprints'
  | 'signets'
  | 'equipment'
  | 'essences';

export interface MarketCategorySelection {
  id: MarketCategoryId;
  label: string;
  itemType: ItemType;
  subcategory: string | null;
}
