import { equipmentSourceLabel } from './acquisition-source';

describe('equipmentSourceLabel compatibility', () => {
  it('shows readable labels for stored equipment reward sources', () => {
    expect(equipmentSourceLabel('model-e:starter')).toBe('Starter equipment');
    expect(equipmentSourceLabel('model-e:protected-dungeon')).toBe('Protected dungeon reward');
    expect(equipmentSourceLabel('equipment:dungeon-completion')).toBe('Dungeon completion');
  });

  it('keeps other sources readable and supplies the caller fallback', () => {
    expect(equipmentSourceLabel('quest:completion')).toBe('Quest Completion');
    expect(equipmentSourceLabel(undefined, 'Dungeon')).toBe('Dungeon');
  });
});
