import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
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

  it('clears the new marker optimistically and reports it to the server once', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'markItemSeen'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [item('crafted', true), item('old')] }),
    );
    inventoryApi.markItemSeen.and.returnValue(of({}));

    const service = createService(inventoryApi);

    expect(service.newItemCount()).toBe(1);

    service.markSeen('crafted-instance');

    expect(
      service.items().find((entry) => entry.id === 'crafted')?.isNew,
    ).toBeFalse();
    expect(service.newItemCount()).toBe(0);
    expect(inventoryApi.markItemSeen).toHaveBeenCalledOnceWith(
      'crafted-instance',
    );

    // A second click must not produce a second write.
    service.markSeen('crafted-instance');
    expect(inventoryApi.markItemSeen).toHaveBeenCalledTimes(1);
  });

  it('leaves the marker cleared when the server write fails', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'markItemSeen'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [item('crafted', true)] }),
    );
    inventoryApi.markItemSeen.and.returnValue(
      throwError(() => new Error('offline')),
    );

    const service = createService(inventoryApi);

    expect(() => service.markSeen('crafted-instance')).not.toThrow();
    expect(
      service.items().find((entry) => entry.id === 'crafted')?.isNew,
    ).toBeFalse();
  });

  it('updates a favorite optimistically and persists the preference', () => {
    const favoriteRequest = new Subject<{
      itemInstanceId: string;
      isFavorite: boolean;
    }>();
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'setItemFavorite'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [item('favorite')] }),
    );
    inventoryApi.setItemFavorite.and.returnValue(favoriteRequest);
    const service = createService(inventoryApi);

    service.setFavorite('favorite-instance', true).subscribe();

    expect(service.items()[0].isFavorite).toBeTrue();
    expect(service.isFavorite('favorite-instance')).toBeTrue();
    expect(inventoryApi.setItemFavorite).toHaveBeenCalledOnceWith(
      'favorite-instance',
      true,
    );

    favoriteRequest.next({
      itemInstanceId: 'favorite-instance',
      isFavorite: true,
    });
    favoriteRequest.complete();
    expect(service.items()[0].isFavorite).toBeTrue();
  });

  it('rolls back an optimistic favorite when persistence fails', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'setItemFavorite'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [item('favorite')] }),
    );
    inventoryApi.setItemFavorite.and.returnValue(
      throwError(() => new Error('offline')),
    );
    const service = createService(inventoryApi);
    let receivedError: unknown;

    service.setFavorite('favorite-instance', true).subscribe({
      error: (error) => (receivedError = error),
    });

    expect(receivedError).toEqual(jasmine.any(Error));
    expect(service.items()[0].isFavorite).toBeFalsy();
  });
});

function createService(
  inventoryApi: jasmine.SpyObj<InventoryService>,
): InventoryStateService {
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

  return TestBed.inject(InventoryStateService);
}

function item(id: string, isNew = false): InventoryItem {
  return {
    id,
    quantity: 1,
    isNew,
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
