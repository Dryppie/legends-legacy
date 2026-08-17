import { of } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { CharacterActionsService } from './character-actions.service';

describe('CharacterActionsService', () => {
  it('does not force a second state refresh after resolving an action', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({ isSuccess: true, data: null }));
    const service = new CharacterActionsService(api);

    service.resolveCurrentAction().subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'CharacterActions/Resolve',
      {},
      { forceStateSyncRefresh: false },
    );
  });
});
