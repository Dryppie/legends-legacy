import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { InventoryService } from './inventory.service';
import { InventoryStateService } from './inventory-state.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';
import { SetInventoryItemFavoriteResponse } from './inventory.service';
import { AuthService } from '../auth/auth.service';

describe('InventoryStateService', () => {
  it('waits for authentication before loading the inventory', () => {
    const authenticated = signal(false);
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
          provide: AuthService,
          useValue: { isAuthenticated: authenticated.asReadonly() },
        },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    TestBed.inject(InventoryStateService);
    TestBed.flushEffects();
    expect(inventoryApi.getInventory).not.toHaveBeenCalled();

    authenticated.set(true);
    TestBed.flushEffects();
    expect(inventoryApi.getInventory).toHaveBeenCalledTimes(1);
  });

  it('does not let an older inventory request overwrite a mutation response', () => {
    const initialRequest = new Subject<InventoryDto>();
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValue(initialRequest);

    const service = createService(inventoryApi);
    service.setInventory([item('mutation-response')]);
    initialRequest.next({ inventoryItems: [item('stale-item')] });

    expect(service.items().map((entry) => entry.id)).toEqual([
      'mutation-response',
    ]);
  });

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
        { provide: AuthService, useValue: authenticatedAuth() },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    TestBed.flushEffects();
    service.load(true);

    purchaseRefresh.next({ inventoryItems: [item('purchased-item')] });
    initialRequest.next({ inventoryItems: [item('stale-item')] });

    expect(service.items().map((entry) => entry.id)).toEqual([
      'purchased-item',
    ]);
  });

  it('applies a purchase grant only once across HTTP and websocket delivery', () => {
    const reward = { ...item('reward'), quantity: 4 };
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValues(
      of({ inventoryItems: [] }),
      of({ inventoryItems: [reward] }),
    );

    TestBed.configureTestingModule({
      providers: [
        InventoryStateService,
        { provide: InventoryService, useValue: inventoryApi },
        { provide: AuthService, useValue: authenticatedAuth() },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    TestBed.flushEffects();

    expect(service.applyInventoryGrant('grant-id', [reward])).toBeTrue();
    expect(service.applyInventoryGrant('grant-id', [reward])).toBeFalse();
    expect(service.items()).toEqual([reward]);
    expect(inventoryApi.getInventory).toHaveBeenCalledTimes(2);
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
        { provide: AuthService, useValue: authenticatedAuth() },
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });

    const service = TestBed.inject(InventoryStateService);
    TestBed.flushEffects();
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
    inventoryApi.markItemSeen.and.returnValue(
      of({
        data: {
          itemInstanceId: 'crafted-instance',
          inventoryItems: [item('crafted'), item('old')],
        },
        domainVersions: { inventory: 1 },
      }),
    );

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
    const favoriteRequest = new Subject<
      VersionedMutationResult<SetInventoryItemFavoriteResponse>
    >();
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
      data: {
        itemInstanceId: 'favorite-instance',
        isFavorite: true,
        inventoryItems: [{ ...item('favorite'), isFavorite: true }],
      },
      domainVersions: { inventory: 1, equipment: 1 },
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

  it('updates a favorite while the item is equipped', () => {
    const favoriteRequest = new Subject<
      VersionedMutationResult<SetInventoryItemFavoriteResponse>
    >();
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'setItemFavorite'],
    );
    inventoryApi.getInventory.and.returnValue(of({ inventoryItems: [] }));
    inventoryApi.setItemFavorite.and.returnValue(favoriteRequest);
    const service = createService(inventoryApi);
    service.setEquippedItems([{ id: 'equipped-instance', isFavorite: false }]);

    service.setFavorite('equipped-instance', true).subscribe();

    expect(service.isFavorite('equipped-instance')).toBeTrue();
    expect(inventoryApi.setItemFavorite).toHaveBeenCalledOnceWith(
      'equipped-instance',
      true,
    );

    favoriteRequest.next({
      data: {
        itemInstanceId: 'equipped-instance',
        isFavorite: true,
        inventoryItems: [],
      },
      domainVersions: { inventory: 1, equipment: 1 },
    });
    favoriteRequest.complete();
    expect(service.isFavorite('equipped-instance')).toBeTrue();
  });

  it('rolls back an equipped favorite when persistence fails', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory', 'setItemFavorite'],
    );
    inventoryApi.getInventory.and.returnValue(of({ inventoryItems: [] }));
    inventoryApi.setItemFavorite.and.returnValue(
      throwError(() => new Error('offline')),
    );
    const service = createService(inventoryApi);
    service.setEquippedItems([{ id: 'equipped-instance', isFavorite: false }]);

    service.setFavorite('equipped-instance', true).subscribe({
      error: () => undefined,
    });

    expect(service.isFavorite('equipped-instance')).toBeFalse();
  });

  it('rejects a late mutation snapshot after a newer inventory version is observed', () => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [item('current')] }),
    );
    const service = createService(inventoryApi);
    TestBed.inject(DomainVersionTracker).observe({ inventory: 5 });

    const applied = service.applyVersionedInventory({
      data: { inventoryItems: [item('late')] },
      domainVersions: { inventory: 4 },
    });

    expect(applied).toBeFalse();
    expect(service.items().map((entry) => entry.id)).toEqual(['current']);
  });

  it('repairs a missing delta when mutation responses arrive newest-first', fakeAsync(() => {
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValues(
      of({ inventoryItems: [item('initial')] }),
      of({ inventoryItems: [item('after-both-mutations')] }),
    );
    const service = createService(inventoryApi);
    TestBed.inject(DomainVersionTracker).observe({ inventory: 6 });

    const applied = service.applyVersionedInventoryDelta(
      {
        data: { item: item('older-delta') },
        domainVersions: { inventory: 5 },
      },
      (data) => service.addOrIncrement(data.item),
    );
    tick(51);

    expect(applied).toBeFalse();
    expect(inventoryApi.getInventory).toHaveBeenCalledTimes(2);
    expect(service.items().map((entry) => entry.id)).toEqual([
      'after-both-mutations',
    ]);
  }));

  it('repairs a grant that arrives after a snapshot already containing it', () => {
    const authoritativeReward = { ...item('reward'), quantity: 4 };
    const inventoryApi = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['getInventory'],
    );
    inventoryApi.getInventory.and.returnValue(
      of({ inventoryItems: [authoritativeReward] }),
    );
    const service = createService(inventoryApi);

    service.applyInventoryGrant('late-grant', [authoritativeReward]);

    expect(service.items()).toEqual([authoritativeReward]);
    expect(inventoryApi.getInventory).toHaveBeenCalledTimes(2);
  });
});

function createService(
  inventoryApi: jasmine.SpyObj<InventoryService>,
): InventoryStateService {
  TestBed.configureTestingModule({
    providers: [
      InventoryStateService,
      { provide: InventoryService, useValue: inventoryApi },
      { provide: AuthService, useValue: authenticatedAuth() },
      { provide: EventBusService, useValue: { logout: signal(false) } },
    ],
  });

  const service = TestBed.inject(InventoryStateService);
  TestBed.flushEffects();
  return service;
}

function authenticatedAuth(): Pick<AuthService, 'isAuthenticated'> {
  return { isAuthenticated: signal(true).asReadonly() };
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
