import { of } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { EssencesService } from './essences.service';

describe('EssencesService', () => {
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
