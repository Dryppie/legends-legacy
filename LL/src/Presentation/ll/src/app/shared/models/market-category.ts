import { ItemType } from './enums/itemType';

export type MarketCategoryId =
  | 'resources'
  | 'consumables'
  | 'blueprints'
  | 'catalysts'
  | 'equipment'
  | 'essences';

export interface MarketCategorySelection {
  id: MarketCategoryId;
  label: string;
  itemType: ItemType;
  subcategory: string | null;
}
