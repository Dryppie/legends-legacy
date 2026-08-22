import { of, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { MarketPlaceService } from './market-place.service';

describe('MarketPlaceService', () => {
  it('falls back to the individual marketplace reads when snapshot is unavailable', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.callFake((path: string) => {
      if (path === 'marketplace/snapshot?historyTake=25') {
        return throwError(() => ({ status: 404 }));
      }

      return of([]);
    });
    const service = new MarketPlaceService(api, {} as never);

    service.getSnapshot(25).subscribe((snapshot) => {
      expect(snapshot).toEqual({
        listings: [],
        catalog: [],
        history: [],
        buyOrders: [],
      });
    });

    expect(api.get.calls.allArgs()).toEqual([
      ['marketplace/snapshot?historyTake=25'],
      ['marketplace'],
      ['marketplace/catalog'],
      ['marketplace/history?take=25'],
      ['marketplace/buyOrders'],
    ]);
  });

  it('does not hide non-route snapshot failures behind fallback requests', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(throwError(() => ({ status: 500 })));
    const service = new MarketPlaceService(api, {} as never);

    service.getSnapshot().subscribe({
      error: (error) => {
        expect(error.message).toBe('Failed to get marketplace snapshot');
      },
    });

    expect(api.get).toHaveBeenCalledOnceWith(
      'marketplace/snapshot?historyTake=50',
    );
  });

  it('owns complete multi-entity marketplace changes but leaves incomplete inventory for refresh', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new MarketPlaceService(api, {} as never);

    service.buyCommodity('item-1', 2, 100).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'marketplace/buyCommodity',
      { itemBaseId: 'item-1', quantity: 2, maximumUnitPrice: 100 },
      {
        stateSyncScopesHandledByResponse: ['marketplace', 'character'],
      },
    );
  });

  it('owns a complete single-entity marketplace response', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new MarketPlaceService(api, {} as never);

    service
      .buyoutListing({ marketPlaceListingId: 'listing-1', quantity: 1 })
      .subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'marketplace/buyoutListing',
      { marketPlaceListingId: 'listing-1', quantity: 1 },
      {
        stateSyncScopesHandledByResponse: [
          'marketplace',
          'inventory',
          'character',
        ],
      },
    );
  });
});
