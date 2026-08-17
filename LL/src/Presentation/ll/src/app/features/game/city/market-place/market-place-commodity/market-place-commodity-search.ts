import { EssenceItem, ItemBase } from '../../../../../shared/models/item';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import {
  essenceDefinitionSearchTerms,
  toSearchText,
} from '../../../../../shared/search/essence-search';

export function marketplaceCommoditySearchText(base: ItemBase): string {
  const searchable: (string | null | undefined)[] = [
    base.name,
    base.description,
  ];

  if (base.itemType === ItemType.Essence) {
    searchable.push(
      ...essenceDefinitionSearchTerms((base as EssenceItem).essence),
    );
  }

  return toSearchText(searchable);
}
