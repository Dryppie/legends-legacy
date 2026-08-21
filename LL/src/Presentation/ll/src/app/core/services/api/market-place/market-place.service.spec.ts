import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { MarketPlaceService } from './market-place.service';

describe('MarketPlaceService', () => {
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
