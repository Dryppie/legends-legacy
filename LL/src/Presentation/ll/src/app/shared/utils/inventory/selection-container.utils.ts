import { ItemBase, SelectionCrateOption } from '../../models/item';

export function selectionContainerMetadata(item: ItemBase) {
  return item.selectionCrate ?? null;
}

export function initialSelectionContainerOptionId(item?: ItemBase): string {
  if (item?.selectionCrate?.isRandom) return 'random';
  if (item?.id.startsWith('item.essence_token.') || item?.id.startsWith('item.tower_supply.')) return '';
  return item?.selectionCrate?.options[0]?.id ?? '';
}

export function filterSelectionOptions(options: SelectionCrateOption[], search: string): SelectionCrateOption[] {
  const terms = search.toLocaleLowerCase().trim().split(/\s+/).filter(Boolean);
  return options.filter(option => {
    const text = `${option.name} ${option.description ?? ''}`.toLocaleLowerCase();
    return terms.every(term => text.includes(term));
  });
}
