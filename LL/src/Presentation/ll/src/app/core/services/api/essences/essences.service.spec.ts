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

  it('leaves partial loadout mutations for coordinator reconciliation', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'put',
      'delete',
    ]);
    api.put.and.returnValue(of({}));
    api.delete.and.returnValue(of({}));
    const service = new EssencesService(api);
    const request = { id: 'loadout-1', name: 'Loadout', slots: [] };

    service.updateLoadout('loadout-1', request).subscribe();
    service.deleteLoadout('loadout-1').subscribe();

    expect(api.put).toHaveBeenCalledOnceWith(
      'essence/loadouts/loadout-1',
      request,
    );
    expect(api.delete).toHaveBeenCalledOnceWith('essence/loadouts/loadout-1');
  });
});
