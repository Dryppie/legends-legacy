import { ItemType } from '../../models/enums/itemType';
import { ItemBase } from '../../models/item';

export const MARKETPLACE_RESOURCE_FAMILY_ITEM_IDS: ReadonlyMap<
  string,
  readonly string[]
> = new Map([
  ['ore', ['ore', 'copper_ore']],
  ['wood', ['wood', 'bloodwood']],
  ['hide', ['rawhide', 'thick_hide']],
]);

export const MARKETPLACE_CATALYST_ITEM_IDS: ReadonlySet<string> = new Set([
  'fury_heart',
  'arcane_focus',
  'executioners_mark',
  'aegis_runestone',
  'warden_sigil',
  'endurance_core',
  'phoenix_ember',
  'spirit_prism',
  'venom_gland',
  'royal_chitin_plate',
  'hive_ichor',
  'item.monster_core.lesser',
  'item.monster_core.greater',
  'item.monster_core.primal',
  'item.evolution_catalyst.cunning',
  'item.evolution_catalyst.warden',
  'item.evolution_catalyst.flame',
  'item.evolution_catalyst.echo',
  'item.evolution_catalyst.hollow',
  'item.evolution_catalyst.holy',
  'item.evolution_catalyst.beast',
  'item.evolution_catalyst.undead',
  'item.evolution_catalyst.boss_soul_core',
]);

export function isMarketplaceTradableItemBase(base: ItemBase): boolean {
  return base.isBound !== true;
}

export function matchesMarketplaceResourceSubcategory(
  base: ItemBase,
  subcategory: string | null | undefined,
): boolean {
  if (base.itemType !== ItemType.Resource) return false;
  if (!subcategory) return true;

  const normalized = subcategory.toLowerCase();
  switch (normalized) {
    case 'all resources':
      return true;
    case 'catalysts':
      return MARKETPLACE_CATALYST_ITEM_IDS.has(base.id);
    default:
      return (
        MARKETPLACE_RESOURCE_FAMILY_ITEM_IDS.get(normalized)?.includes(
          base.id,
        ) ?? false
      );
  }
}

export function getMarketplaceResourceSortRank(
  base: ItemBase,
  subcategory: string | null | undefined,
): number {
  if (!subcategory) return Number.MAX_SAFE_INTEGER;

  const normalized = subcategory.toLowerCase();
  const familyIds =
    normalized === 'catalysts'
      ? [...MARKETPLACE_CATALYST_ITEM_IDS]
      : MARKETPLACE_RESOURCE_FAMILY_ITEM_IDS.get(normalized);
  const index = familyIds?.indexOf(base.id) ?? -1;

  return index === -1 ? Number.MAX_SAFE_INTEGER : index;
}
