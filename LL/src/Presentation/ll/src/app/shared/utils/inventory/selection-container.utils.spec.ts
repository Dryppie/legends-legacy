import { ItemBase, SelectionCrateMetadata, SelectionCrateOption } from '../../models/item';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { filterSelectionOptions, initialSelectionContainerOptionId } from './selection-container.utils';

describe('equipment supply choices', () => {
  const item = (id: string, selectionCrate: SelectionCrateMetadata): ItemBase => ({
    id, name: id, description: '', rarity: Rarity.Common, itemType: ItemType.Resource,
    stackable: true, selectionCrate,
  });
  const options: SelectionCrateOption[] = [
    { id: 'health', name: 'Plate Helm — Vitality', description: 'Head · Tier 2 · Legendary · Masterpiece · Rank 5', quantity: 1 },
    { id: 'power', name: 'Wand — Power', description: 'One-handed · Tier 2 · Legendary · Masterpiece · Rank 5', quantity: 1 },
  ];

  it('requires an explicit Tower equipment choice', () => {
    expect(initialSelectionContainerOptionId(item('item.tower_supply.v1.floor_10',
      { selectionLabel: 'Equipment', options }))).toBe('');
  });

  it('filters by combined item, specialization and slot terms without mutating options', () => {
    expect(filterSelectionOptions(options, '  HEAD vitality ')).toEqual([options[0]]);
    expect(filterSelectionOptions(options, 'wand legendary')).toEqual([options[1]]);
    expect(filterSelectionOptions(options, 'shield')).toEqual([]);
    expect(filterSelectionOptions(options, '')).toEqual(options);
    expect(options.length).toBe(2);
  });

  it('keeps existing random and tutorial container behavior', () => {
    expect(initialSelectionContainerOptionId(item('item.uncommon_equipment_box',
      { isRandom: true, selectionLabel: '', options: [] }))).toBe('random');
    expect(initialSelectionContainerOptionId(item('item.arms_chest',
      { selectionLabel: 'Weapon', options }))).toBe('health');
  });
});
