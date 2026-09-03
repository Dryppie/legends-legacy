import { ItemBase } from '../../models/item';

export function selectionContainerMetadata(item: ItemBase) {
  return item.selectionCrate ?? null;
}

export function initialSelectionContainerOptionId(item?: ItemBase): string {
  if (item?.id.startsWith('item.essence_token.')) return '';
  return item?.selectionCrate?.options[0]?.id ?? '';
}
