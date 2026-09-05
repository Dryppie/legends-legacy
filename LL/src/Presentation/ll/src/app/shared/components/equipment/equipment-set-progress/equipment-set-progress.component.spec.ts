import { EquipmentSetProgressComponent } from './equipment-set-progress.component';
import { EquipmentInstance, EquipmentSetMetadata } from '../../../models/item';

describe('EquipmentSetProgressComponent', () => {
  it('distinguishes the next and locked bonuses from active bonuses', () => {
    const component = new EquipmentSetProgressComponent();
    component.equipmentSet = equipmentSet();
    component.equippedItems = [setItem('one')];

    expect(component.bonusClass(component.equipmentSet.bonuses[0])).toBe(
      'equipment-set-bonus-next',
    );
    expect(
      component.bonusProgressLabel(component.equipmentSet.bonuses[0]),
    ).toBe('1 more item');
    expect(component.bonusClass(component.equipmentSet.bonuses[1])).toBe(
      'equipment-set-bonus-locked',
    );
    expect(component.bonusClass(component.equipmentSet.bonuses[2])).toBe(
      'equipment-set-bonus-locked',
    );
  });

  it('highlights every bonus in catalog mode', () => {
    const component = new EquipmentSetProgressComponent();
    component.equipmentSet = equipmentSet();
    component.highlightAllBonuses = true;

    expect(
      component.equipmentSet.bonuses.map((bonus) =>
        component.bonusClass(bonus),
      ),
    ).toEqual([
      'equipment-set-bonus-active',
      'equipment-set-bonus-active',
      'equipment-set-bonus-active',
    ]);
  });
});

function equipmentSet(): EquipmentSetMetadata {
  return {
    id: 'set_warden',
    name: 'Warden',
    description: 'Stabilizes after taking sustained pressure.',
    bonuses: [
      { id: 'two', requiredEquippedItems: 2, description: 'Two pieces.' },
      { id: 'four', requiredEquippedItems: 4, description: 'Four pieces.' },
      { id: 'six', requiredEquippedItems: 6, description: 'Six pieces.' },
    ],
  };
}

function setItem(id: string): EquipmentInstance {
  return {
    id,
    equipmentSet: equipmentSet(),
  } as unknown as EquipmentInstance;
}
