import { of } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { EssencesService } from './essences.service';

describe('EssencesService', () => {
  it('sets and clears Creature Focus through the renamed route without extra refreshes', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new EssencesService(api);
    for (const creatureId of ['creature-1', null]) {
      service.setCreatureFocus(creatureId).subscribe();
      expect(api.postVersioned.calls.mostRecent().args).toEqual([
        'essence/creature-focus',
        { creatureId },
        { stateSyncScopesHandledByResponse: ['essences'] },
      ]);
    }
    expect(api.postVersioned).toHaveBeenCalledTimes(2);
  });

  it('marks Dust response scopes as handled without follow-up refreshes', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new EssencesService(api);

    service.spendDust('essence-1', 1).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'essence/essence-1/spend-dust',
      { dustAmount: 1 },
      {
        stateSyncScopesHandledByResponse: [
          'essences',
          'inventory',
          'equipment',
        ],
      },
    );
  });

  it('sends the requested shatter quantity with handled mutation scopes', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new EssencesService(api);

    service.dismantle('inventory-item-1', 5).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'essence/items/inventory-item-1/dismantle',
      { quantity: 5 },
      {
        stateSyncScopesHandledByResponse: [
          'essences',
          'inventory',
          'equipment',
        ],
      },
    );
  });

  it('marks loadout mutation responses as essence-owned', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'putVersioned',
      'deleteVersioned',
    ]);
    api.putVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    api.deleteVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new EssencesService(api);
    const request = { id: 'loadout-1', name: 'Loadout', slots: [] };

    service.updateLoadout('loadout-1', request).subscribe();
    service.deleteLoadout('loadout-1').subscribe();

    expect(api.putVersioned).toHaveBeenCalledOnceWith(
      'essence/loadouts/loadout-1',
      request,
      { stateSyncScopesHandledByResponse: ['essences'] },
    );
    expect(api.deleteVersioned).toHaveBeenCalledOnceWith(
      'essence/loadouts/loadout-1',
      {},
      { stateSyncScopesHandledByResponse: ['essences'] },
    );
  });
});
