import { signal } from '@angular/core';
import { EquipmentOverviewComponent } from './equipment-overview.component';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { EquipmentSlot } from '../../models/Dtos/equipment-slots/equipmentSlot';
describe('EquipmentOverviewComponent', () => {
  it('shows the eight combat slots', () => {
    const component = new EquipmentOverviewComponent({} as ModalService, { equipmentSlots: signal<EquipmentSlot[]>([]) } as unknown as EquipmentStateService);
    expect(component.slots().length).toBe(8);
  });
});
