import { TestBed } from '@angular/core/testing';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { EquipmentSlot } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../../../models/enums/equipmentType';
import { EquipmentInstance } from '../../../../models/item';
import { InventoryEquipmentModalComponent } from './inventory-equipment-modal.component';

describe('InventoryEquipmentModalComponent', () => {
  it('opens management mode without comparison work', () => {
    const equipment = {
      id: 'equipped-item',
      itemBase: { equipmentType: EquipmentType.TwoHanded },
    } as unknown as EquipmentInstance;
    const equipmentState = {
      equipmentSlots: () => [] as EquipmentSlot[],
    } as EquipmentStateService;
    const equipmentApi = jasmine.createSpyObj<EquipmentService>(
      'EquipmentService',
      ['compareEquipment'],
    );
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryEquipmentModalComponent(
          equipmentState,
          { items: () => [] } as unknown as InventoryStateService,
          equipmentApi,
          {
            currentCharacter: () => null,
          } as unknown as CharacterStateService,
        ),
    );
    component.equipmentInstance = equipment;
    component.managementOnly = true;

    component.ngOnInit();

    expect(component.isEquipped).toBeFalse();
    expect(component.requiresHandSelection).toBeFalse();
    expect(equipmentApi.compareEquipment).not.toHaveBeenCalled();
  });
});
