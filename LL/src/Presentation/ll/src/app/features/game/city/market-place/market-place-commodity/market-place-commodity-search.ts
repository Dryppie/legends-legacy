import { EssenceItem, ItemBase } from '../../../../../shared/models/item';
import { ItemType } from '../../../../../shared/models/enums/itemType';

export function marketplaceCommoditySearchText(base: ItemBase): string {
  const searchable = [base.name, base.description];

  if (base.itemType === ItemType.Essence) {
    const essence = (base as EssenceItem).essence;
    if (essence) {
      searchable.push(
        essence.name,
        essence.variantName,
        essence.displayName,
        essence.description,
        ...Object.values(essence.tagsByCategory ?? {}).flat(),
        essence.activeAbility.name,
        ...(essence.activeAbility.tags ?? []),
        essence.passiveAbility.name,
        ...(essence.passiveAbility.tags ?? []),
        ...(essence.evolution.addsTags ?? []),
      );
    }
  }

  return searchable.join(' ').toLowerCase();
}
