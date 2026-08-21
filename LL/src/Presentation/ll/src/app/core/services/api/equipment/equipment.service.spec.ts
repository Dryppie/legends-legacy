import { of } from 'rxjs';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { ApiService } from '../api.service';
import { EquipmentService } from './equipment.service';

describe('EquipmentService', () => {
  it('marks response-owned equipment mutation scopes as handled', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(
      of({
        data: { equipmentSlots: [], inventoryItems: [] },
        domainVersions: { equipment: 4, inventory: 7 },
      }),
    );
    const service = new EquipmentService(api);
    const equipment = { id: 'equipment-1' } as EquipmentInstance;

    service.equipEquipment(equipment, EquipmentSlotType.MainHand).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'equipment/equip',
      {
        equipmentItemId: 'equipment-1',
        slotType: EquipmentSlotType.MainHand,
      },
      {
        stateSyncScopesHandledByResponse: ['equipment', 'inventory'],
      },
    );
  });
});
