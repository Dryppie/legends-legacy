import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketPlaceOrder } from '../../../../shared/models/Dtos/market-place/market-place-order';
import { MarketPlaceBuyOrder } from '../../../../shared/models/Dtos/market-place/market-place-buy-order';
import { MarketplaceChangeSet } from '../../../../shared/models/Dtos/market-place/marketplace-change-set';
import { ToastService } from '../../client-side/components/toast/toast.service';
import {
  GameRealtimeEnvelope,
  MarketplaceChanged,
} from '../../real-time/game-realtime/game-realtime-contracts';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { VersionedMutationResult } from '../api.service';
import { CharacterService } from '../character/character.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { MarketplaceStateService } from './market-place-state.service';
import { MarketPlaceService } from './market-place.service';

describe('MarketplaceStateService semantic sequencing', () => {
  const characterId = 'character-1';
  let eventEnvelope: WritableSignal<GameRealtimeEnvelope<MarketplaceChanged> | null>;
  let stateSync: jasmine.SpyObj<StateSyncCoordinator>;
  let domainVersions: DomainVersionTracker;
  let service: MarketplaceStateService;
  let marketplace: jasmine.SpyObj<MarketPlaceService>;

  beforeEach(() => {
    eventEnvelope = signal<GameRealtimeEnvelope<MarketplaceChanged> | null>(
      null,
    );
    stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      [
        'register',
        'activate',
        'latestRevision',
        'acceptSnapshotResponse',
        'acceptInvalidation',
        'rejectMutationResponse',
      ],
    );
    stateSync.register.and.returnValue(() => undefined);
    stateSync.latestRevision.and.returnValue(0);

    marketplace = jasmine.createSpyObj<MarketPlaceService>(
      'MarketPlaceService',
      ['getSnapshot'],
    );
    marketplace.getSnapshot.and.returnValue(
      of({ listings: [], catalog: [], history: [], buyOrders: [] }),
    );

    TestBed.configureTestingModule({
      providers: [
        MarketplaceStateService,
        DomainVersionTracker,
        { provide: MarketPlaceService, useValue: marketplace },
        {
          provide: CharacterService,
          useValue: {
            currentCharacterId: () => characterId,
            currentCharacter: () => null,
            updateCharacter: () => undefined,
          },
        },
        { provide: InventoryStateService, useValue: {} },
        {
          provide: GameRealtimeEventRegistry,
          useValue: {
            eventEnvelope: {
              MarketplaceChanged: eventEnvelope.asReadonly(),
            },
          },
        },
        { provide: ToastService, useValue: {} },
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });

    domainVersions = TestBed.inject(DomainVersionTracker);
    service = TestBed.inject(MarketplaceStateService);
    service.load();
  });

  it('applies a contiguous entity and history change atomically', () => {
    domainVersions.observe({ marketplace: 3 });
    const listing = { id: 'listing-1' } as MarketPlaceListing;
    const order = {
      id: 'trade-1',
      buyerId: characterId,
      sellerId: 'seller-1',
      purchasedAt: new Date('2026-08-21T10:00:00Z'),
    } as MarketPlaceOrder;

    emit({
      version: 4,
      listingChanges: [{ listingId: listing.id, listing }],
      buyOrderChanges: [],
      orders: [order],
      affectedCharacterIds: [characterId, 'seller-1'],
    });

    expect(service.listings()).toEqual([listing]);
    expect(service.history()).toEqual([order]);
    expect(stateSync.acceptSnapshotResponse).toHaveBeenCalledWith(
      { marketplace: 4 },
      ['marketplace'],
    );
  });

  it('rejects a sequence gap and requests one authoritative reconciliation', () => {
    domainVersions.observe({ marketplace: 3 });

    emit({
      version: 5,
      listingChanges: [
        {
          listingId: 'listing-1',
          listing: { id: 'listing-1' } as MarketPlaceListing,
        },
      ],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [],
    });

    expect(service.listings()).toEqual([]);
    expect(stateSync.acceptInvalidation).toHaveBeenCalledOnceWith({
      characterId: null,
      scope: 'marketplace',
      revision: 5,
      reason: 'Marketplace semantic sequence gap',
    });
  });

  it('applies a contiguous mutation response using the pre-response coordinator revision', () => {
    stateSync.latestRevision.and.returnValue(3);
    domainVersions.observe({ marketplace: 4 });
    const listing = { id: 'listing-1' } as MarketPlaceListing;

    const applied = applyResponse({
      version: 4,
      listingChanges: [{ listingId: listing.id, listing }],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [characterId],
    });

    expect(applied).toBeTrue();
    expect(service.listings()).toEqual([listing]);
    expect(stateSync.acceptSnapshotResponse).toHaveBeenCalledWith(
      { marketplace: 4 },
      ['marketplace'],
    );
  });

  it('rejects a mutation response that skips a marketplace revision', () => {
    stateSync.latestRevision.and.returnValue(3);
    domainVersions.observe({ marketplace: 5 });

    const applied = applyResponse({
      version: 5,
      listingChanges: [
        {
          listingId: 'listing-1',
          listing: { id: 'listing-1' } as MarketPlaceListing,
        },
      ],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [characterId],
    });

    expect(applied).toBeFalse();
    expect(service.listings()).toEqual([]);
    expect(stateSync.rejectMutationResponse).toHaveBeenCalledOnceWith(
      'marketplace',
      5,
    );
  });

  it('does not reapply an equal-version event after the mutation response', () => {
    const applyChanges = spyOn(
      service as unknown as {
        applyMarketplaceChanges(changes: MarketplaceChangeSet): void;
      },
      'applyMarketplaceChanges',
    ).and.callThrough();
    const listing = { id: 'listing-1' } as MarketPlaceListing;
    const changes: MarketplaceChangeSet = {
      version: 1,
      listingChanges: [{ listingId: listing.id, listing }],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [characterId],
    };

    expect(applyResponse(changes)).toBeTrue();
    domainVersions.observe({ marketplace: 1 });
    emit(changes);

    expect(applyChanges).toHaveBeenCalledTimes(1);
  });

  it('does not reapply an equal-version response after the event arrived first', () => {
    const applyChanges = spyOn(
      service as unknown as {
        applyMarketplaceChanges(changes: MarketplaceChangeSet): void;
      },
      'applyMarketplaceChanges',
    ).and.callThrough();
    const listing = { id: 'listing-1' } as MarketPlaceListing;
    const changes: MarketplaceChangeSet = {
      version: 1,
      listingChanges: [{ listingId: listing.id, listing }],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [characterId],
    };

    emit(changes);
    stateSync.latestRevision.and.returnValue(1);

    expect(applyResponse(changes)).toBeTrue();
    expect(applyChanges).toHaveBeenCalledTimes(1);
  });

  it('does not let an older refresh overwrite a mutation response', () => {
    const staleSnapshot = new Subject<{
      listings: MarketPlaceListing[];
      catalog: [];
      history: MarketPlaceOrder[];
      buyOrders: MarketPlaceBuyOrder[];
    }>();
    marketplace.getSnapshot.and.returnValue(staleSnapshot.asObservable());
    service.refresh();

    const current = { id: 'current-listing' } as MarketPlaceListing;
    expect(
      applyResponse({
        version: 1,
        listingChanges: [{ listingId: current.id, listing: current }],
        buyOrderChanges: [],
        orders: [],
        affectedCharacterIds: [characterId],
      }),
    ).toBeTrue();

    staleSnapshot.next({
      listings: [{ id: 'stale-listing' } as MarketPlaceListing],
      catalog: [],
      history: [],
      buyOrders: [],
    });
    staleSnapshot.complete();

    expect(service.listings()).toEqual([current]);
    expect(service.loading()).toBeFalse();
  });

  it('does not let an older refresh overwrite a realtime semantic change', () => {
    const staleSnapshot = new Subject<{
      listings: MarketPlaceListing[];
      catalog: [];
      history: MarketPlaceOrder[];
      buyOrders: MarketPlaceBuyOrder[];
    }>();
    marketplace.getSnapshot.and.returnValue(staleSnapshot.asObservable());
    service.refresh();

    const current = { id: 'current-listing' } as MarketPlaceListing;
    emit({
      version: 1,
      listingChanges: [{ listingId: current.id, listing: current }],
      buyOrderChanges: [],
      orders: [],
      affectedCharacterIds: [],
    });

    staleSnapshot.next({
      listings: [{ id: 'stale-listing' } as MarketPlaceListing],
      catalog: [],
      history: [],
      buyOrders: [],
    });
    staleSnapshot.complete();

    expect(service.listings()).toEqual([current]);
    expect(service.loading()).toBeFalse();
  });

  function emit(changes: MarketplaceChanged['changes']): void {
    eventEnvelope.set({
      event: 'MarketplaceChanged',
      updateId: `market-${changes.version}`,
      payload: { changes },
    });
    TestBed.flushEffects();
  }

  function applyResponse(changes: MarketplaceChangeSet): boolean {
    const result: VersionedMutationResult<{
      marketplace: MarketplaceChangeSet;
    }> = {
      data: { marketplace: changes },
      domainVersions: { marketplace: changes.version },
    };
    return (
      service as unknown as {
        applyVersionedMarketplace(
          response: VersionedMutationResult<{
            marketplace: MarketplaceChangeSet;
          }>,
        ): boolean;
      }
    ).applyVersionedMarketplace(result);
  }
});
