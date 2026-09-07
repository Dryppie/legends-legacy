import { InventoryItem } from '../../models/inventoryItem';
import { EquipmentInstance } from '../../models/item';
import { Rarity } from '../../models/enums/rarity';
import { ItemQuality } from '../../models/enums/itemQuality';
import { marketplaceStyleLabel } from '../market-place/marketplace-equipment';
export type EquipmentInventorySort =
  | 'Name'
  | 'Quality'
  | 'Rank'
  | 'Style'
  | 'Gear Power';
export type InventorySort = EquipmentInventorySort | 'Tier' | 'Rarity';
export type SortDirection = 'asc' | 'desc';
export const equipmentInventorySortOptions: EquipmentInventorySort[] = [
  'Name',
  'Quality',
  'Rank',
  'Style',
  'Gear Power',
];
const RARITY_ORDER: Record<Rarity, number> = {
  [Rarity.Common]: 0,
  [Rarity.Uncommon]: 1,
  [Rarity.Rare]: 2,
  [Rarity.Epic]: 3,
  [Rarity.Unique]: 4,
  [Rarity.Legendary]: 5,
  [Rarity.Legacy]: 6,
};
const QUALITY_ORDER: Record<ItemQuality, number> = {
  [ItemQuality.Crude]: 0,
  [ItemQuality.Standard]: 1,
  [ItemQuality.Fine]: 2,
  [ItemQuality.Exceptional]: 3,
  [ItemQuality.Masterpiece]: 4,
};

const equipment = (item: InventoryItem) =>
  'equipmentBase' in item.itemInstance
    ? (item.itemInstance as EquipmentInstance)
    : null;
const displayName = (item: InventoryItem) =>
  equipment(item)?.displayName ?? item.itemInstance.itemBase.name;
export function sortInventoryItems(
  items: InventoryItem[],
  sort: InventorySort,
  direction: SortDirection,
): InventoryItem[] {
  return [...items].sort((a, b) => {
    const aEquipment = equipment(a);
    const bEquipment = equipment(b);
    let difference = 0;

    switch (sort) {
      case 'Name':
        difference = displayName(a).localeCompare(displayName(b));
        break;
      case 'Tier':
        difference = (aEquipment?.tier ?? 0) - (bEquipment?.tier ?? 0);
        break;
      case 'Rarity':
        difference =
          RARITY_ORDER[aEquipment?.rarity ?? a.itemInstance.itemBase.rarity] -
          RARITY_ORDER[bEquipment?.rarity ?? b.itemInstance.itemBase.rarity];
        break;
      case 'Quality':
        difference =
          (aEquipment ? QUALITY_ORDER[aEquipment.quality] : -1) -
          (bEquipment ? QUALITY_ORDER[bEquipment.quality] : -1);
        break;
      case 'Rank':
        difference =
          (aEquipment?.progression?.rank ?? -1) -
          (bEquipment?.progression?.rank ?? -1);
        break;
      case 'Style':
        difference = marketplaceStyleLabel(
          aEquipment?.progression?.activeStyleId,
        ).localeCompare(
          marketplaceStyleLabel(bEquipment?.progression?.activeStyleId),
        );
        break;
      case 'Gear Power':
        difference =
          (aEquipment?.itemBudget ?? 0) - (bEquipment?.itemBudget ?? 0);
        break;
    }

    return (
      (direction === 'asc' ? difference : -difference) ||
      displayName(a).localeCompare(displayName(b))
    );
  });
}
