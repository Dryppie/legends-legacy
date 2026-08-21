import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { InventoryService } from './inventory.service';

describe('InventoryService', () => {
  it('marks the locally applied favorite response scopes as handled', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(
      of({
        data: {
          itemInstanceId: 'item-1',
          isFavorite: true,
          inventoryItems: [],
        },
        domainVersions: { inventory: 3, equipment: 2 },
      }),
    );
    const service = new InventoryService(api);

    service.setItemFavorite('item-1', true).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'inventory/items/item-1/favorite',
      { isFavorite: true },
      {
        stateSyncScopesHandledByResponse: ['inventory'],
      },
    );
  });
});
