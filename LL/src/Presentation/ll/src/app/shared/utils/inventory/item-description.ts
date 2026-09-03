import { ItemBase } from '../../models/item';

export function itemDescription(base: ItemBase): string {
  if (base.id === 'item.blueprint_selection_box') {
    return 'Choose a Blueprint to learn a reusable equipment style in the Forge.';
  }
  if (base.id.startsWith('blueprint_')) {
    return 'Consume one Blueprint in the Forge to permanently learn a reusable equipment style for every compatible archetype. The first application is free. Later style changes cost Cinders.';
  }
  return base.description;
}
