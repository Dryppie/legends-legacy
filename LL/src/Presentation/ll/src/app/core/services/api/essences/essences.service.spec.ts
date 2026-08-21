import { of } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { EssencesService } from './essences.service';

describe('EssencesService', () => {
  it('marks Dust response scopes as handled without follow-up refreshes', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({}));
    const service = new EssencesService(api);

    service.spendDust('essence-1', 1).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'essence/essence-1/spend-dust',
      { dustAmount: 1 },
      {
        stateSyncScopesHandledByResponse: [
          'essences',
          'inventory',
          'character',
          'equipment',
          'quests',
        ],
      },
    );
  });
});
