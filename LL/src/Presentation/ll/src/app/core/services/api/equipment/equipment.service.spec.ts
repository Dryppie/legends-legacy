import { of } from 'rxjs';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { ApiService } from '../api.service';
import { EquipmentService, EquipmentUpgradeQuote } from './equipment.service';

describe('EquipmentService', () => {
  it('applies the exact variant from the reviewed quote', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ outcome: {}, freshQuote: null }));
    const service = new EquipmentService(api);
    const quote = {
      operationId: 'conversion-1',
      token: 'reviewed-state',
      request: {
        kind: 'ApplyVariant',
        itemInstanceId: 'sword-1',
        blueprintStyleId: 'blueprint_fury',
        allowFavoriteDismantle: false,
      },
    } as EquipmentUpgradeQuote;
    service.applyVariant(quote).subscribe();
    expect(api.post).toHaveBeenCalledOnceWith('equipment/upgrade/variant', {
      operationId: 'conversion-1',
      itemInstanceId: 'sword-1',
      blueprintStyleId: 'blueprint_fury',
      quoteToken: 'reviewed-state',
    });
  });
  it('marks authoritative equipment mutation scopes as handled', () => {
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

  it('executes dismantling from the exact confirmed quote', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ outcome: {}, freshQuote: null }));
    const service = new EquipmentService(api);
    const quote = {
      operationId: 'operation-1',
      token: 'quoted-state',
      request: {
        kind: 'Dismantle',
        itemInstanceId: 'equipment-1',
        allowFavoriteDismantle: true,
      },
    } as EquipmentUpgradeQuote;

    service.dismantle(quote).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith('equipment/upgrade/dismantle', {
      operationId: 'operation-1',
      itemInstanceId: 'equipment-1',
      allowFavoriteDismantle: true,
      quoteToken: 'quoted-state',
    });
  });
});
