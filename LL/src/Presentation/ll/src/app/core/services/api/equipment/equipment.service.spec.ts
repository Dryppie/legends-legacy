import { of } from 'rxjs';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { ApiService } from '../api.service';
import { EquipmentService, EquipmentUpgradeQuote } from './equipment.service';

describe('EquipmentService', () => {
  it('records a comparison with a request identity and the selected activity', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({}));
    const service = new EquipmentService(api);
    service.compareEquipment('sword-1', EquipmentSlotType.MainHand, 'None').subscribe();
    expect(api.post).toHaveBeenCalledOnceWith('equipment/compare/observe', {
      requestId: jasmine.stringMatching(/^[0-9a-f-]{36}$/i),
      equipmentInstanceId: 'sword-1', slotType: EquipmentSlotType.MainHand, activity: 'None',
    });
  });

  it('reinforces with only the item and operation IDs', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ outcome: {} }));
    const service = new EquipmentService(api);

    service
      .reinforce({
        operationId: 'reinforce-1',
        request: { itemInstanceId: 'sword-1' },
      } as EquipmentUpgradeQuote)
      .subscribe();

    expect(api.post).toHaveBeenCalledOnceWith('equipment/upgrade/reinforce', {
      operationId: 'reinforce-1',
      itemInstanceId: 'sword-1',
    });
  });

  it('applies the selected variant without a quote token', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ outcome: {} }));
    const service = new EquipmentService(api);
    const quote = {
      operationId: 'conversion-1',
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

  it('sends favorite confirmation and an operation ID without a quote token', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ outcome: {} }));
    const service = new EquipmentService(api);
    const quote = {
      operationId: 'operation-1',
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
    });
  });
});
