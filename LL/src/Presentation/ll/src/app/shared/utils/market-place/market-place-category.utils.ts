import { ItemType } from '../../models/enums/itemType';
import { ItemBase } from '../../models/item';

export const MARKETPLACE_RESOURCE_FAMILY_ITEM_IDS: ReadonlyMap<
  string,
  readonly string[]
> = new Map([
  ['metal', ['ore', 'copper_ore', 'verdant_ore']],
  ['wood', ['wood', 'bloodwood', 'living_bark']],
  ['hide', ['rawhide', 'thick_hide', 'scaled_hide']],
  ['crystal', ['crystalline_powder', 'cracked_garnet', 'soulglass_shard']],
  ['stone', ['rough_stone', 'mossy_stone', 'deep_stone']],
  ['fiber', ['woven_fiber', 'silk_thread', 'spectral_thread']],
  ['bone', ['bone_fragments', 'grave_bone', 'ancient_bone']],
  ['chitin', ['ant_chitin', 'hardened_chitin', 'royal_chitin_fragment']],
  ['resin', ['hive_resin', 'amber_resin', 'living_resin']],
  ['oil', ['murky_fish_oil', 'refined_fish_oil', 'shadow_oil']],
]);

export const MARKETPLACE_CATALYST_ITEM_IDS: ReadonlySet<string> = new Set([
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

export function isMarketplaceBlueprintResource(base: ItemBase): boolean {
  return (
    base.itemType === ItemType.Resource &&
    (base.id.toLowerCase().startsWith('blueprint_') ||
      base.name.toLowerCase().startsWith('blueprint:'))
  );
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
    case 'blueprints':
      return isMarketplaceBlueprintResource(base);
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
