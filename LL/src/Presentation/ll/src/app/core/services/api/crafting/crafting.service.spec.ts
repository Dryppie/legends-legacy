import { of } from 'rxjs';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { ApiService } from '../api.service';
import { CraftingService } from './crafting.service';

describe('CraftingService response ownership', () => {
  it('uses the returned tempering inventory without claiming unrelated scopes', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(
      of({
        data: {
          isSuccess: true,
          data: {
            removedInventoryItemIds: [],
            returnedInventoryItems: [],
            removedQueueItemIds: [],
            action: null,
          },
        },
        domainVersions: { inventory: 1 },
      }),
    );
    const service = new CraftingService(api, {
      showToast: jasmine.createSpy('showToast'),
    } as unknown as ToastService);

    service.removeItemFromQueue({ id: 'queue-1' }).subscribe();
    service.cancelTemperingQueue().subscribe();

    expect(api.postVersioned.calls.argsFor(0)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['inventory'],
    });
    expect(api.postVersioned.calls.argsFor(1)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['inventory'],
    });
  });
});
