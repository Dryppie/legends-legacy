import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameEventService } from '../../real-time/game-event.service';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { InventoryService } from './inventory.service';
import { InventoryStateService } from './inventory-state.service';

describe('InventoryStateService', () => {
  it('does not let an older inventory request overwrite a forced refresh', () => {
    const initialRequest = new Subject<InventoryDto>();
    const purchaseRefresh = new Subject<InventoryDto>();
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValues(initialRequest, purchaseRefresh);

    TestBed.configureTestingModule({
      providers: [
        InventoryStateService,
        { provide: InventoryService, useValue: inventoryApi },
        {
          provide: GameEventService,
          useValue: {
            eventEnvelope: { LootReceivedMsg: signal(null) },
            reconnectCount: signal(0),
          },
        },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    service.load(true);

    purchaseRefresh.next({ inventoryItems: [item('purchased-item')] });
    initialRequest.next({ inventoryItems: [item('stale-item')] });

    expect(service.items().map((entry) => entry.id)).toEqual([
      'purchased-item',
    ]);
  });

  it('applies a purchase grant only once across HTTP and websocket delivery', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValue(of({ inventoryItems: [] }));

    TestBed.configureTestingModule({
      providers: [
        InventoryStateService,
        { provide: InventoryService, useValue: inventoryApi },
        {
          provide: GameEventService,
          useValue: {
            eventEnvelope: { LootReceivedMsg: signal(null) },
            reconnectCount: signal(0),
          },
        },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    const reward = { ...item('reward'), quantity: 4 };

    expect(service.applyInventoryGrant('grant-id', [reward])).toBeTrue();
    expect(service.applyInventoryGrant('grant-id', [reward])).toBeFalse();
    expect(service.items()).toEqual([reward]);
  });

  it('replaces an older in-flight snapshot with a post-grant snapshot', () => {
    const initialRequest = new Subject<InventoryDto>();
    const postGrantSnapshot = new Subject<InventoryDto>();
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValues(
      initialRequest,
      postGrantSnapshot,
    );

    TestBed.configureTestingModule({
      providers: [
        InventoryStateService,
        { provide: InventoryService, useValue: inventoryApi },
        {
          provide: GameEventService,
          useValue: {
            eventEnvelope: { LootReceivedMsg: signal(null) },
            reconnectCount: signal(0),
          },
        },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    const reward = item('reward');
    service.applyInventoryGrant('grant-id', [reward]);

    initialRequest.next({ inventoryItems: [item('stale')] });
    postGrantSnapshot.next({ inventoryItems: [item('existing'), reward] });

    expect(inventoryApi.getInventory).toHaveBeenCalledTimes(2);
    expect(service.items().map((entry) => entry.id)).toEqual([
      'existing',
      'reward',
    ]);
  });
});

function item(id: string): InventoryItem {
  return {
    id,
    quantity: 1,
    itemInstance: {
      id: `${id}-instance`,
      itemBase: {
        id: `${id}-base`,
        name: id,
        description: '',
        rarity: 'Common' as never,
        itemType: ItemType.Resource,
        stackable: true,
      },
    },
  };
}
