import { EquipmentInstance, ItemInstance } from '../../models/item';
import { ItemType } from '../../models/enums/itemType';

export function marketplaceEquipment(
  item: ItemInstance,
): EquipmentInstance | null {
  return item.itemBase.itemType === ItemType.Equipment
    ? (item as EquipmentInstance)
    : null;
}

export function marketplaceItemIsBound(item: ItemInstance): boolean {
  const model = marketplaceEquipment(item)?.progression;
  return !!(
    item.isBound ||
    item.itemBase.isBound ||
    (model && model.ownership !== 'UnboundPersonal')
  );
}

export function marketplaceStyleLabel(id: string | null | undefined): string {
  return id
    ? id
        .replace(/^blueprint_/, '')
        .replace(/[_.-]/g, ' ')
        .replace(/\b\w/g, (letter) => letter.toUpperCase())
    : 'Plain';
}

export function marketplaceEquipmentSummary(item: ItemInstance): string {
  const equipment = marketplaceEquipment(item);
  if (!equipment) return '—';
  return equipment.progression
    ? 'Tier ' + equipment.tier + ' · Rank ' + equipment.progression.rank
    : equipment.quality;
}
