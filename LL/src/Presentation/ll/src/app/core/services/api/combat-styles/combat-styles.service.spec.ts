import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { CombatStylesService } from './combat-styles.service';

describe('Combat Style transport', () => {
  it('loads the global Combat Style without an activity query', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of({}));
    new CombatStylesService(api).get().subscribe();
    expect(api.get).toHaveBeenCalledOnceWith('combat-styles');
  });

  it('saves only global choices and handles the returned style state', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'putVersioned',
    ]);
    api.putVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const request = {
      combatStyleId: 'conduit',
      refinementId: null,
      upgradeIds: ['full-circuit'],
      masteredUpgradeId: 'full-circuit',
    };
    new CombatStylesService(api).select(request).subscribe();
    expect(api.putVersioned).toHaveBeenCalledOnceWith(
      'combat-styles/selection',
      request,
      { stateSyncScopesHandledByResponse: ['combat-styles'] },
    );
  });

  it('previews the global choices without creating a mutation invalidation', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['post']);
    api.post.and.returnValue(of({}));
    const request = {
      combatStyleId: 'bastion',
      refinementId: null,
      upgradeIds: ['prepared-wall'],
      masteredUpgradeId: 'prepared-wall',
    };
    new CombatStylesService(api).preview(request).subscribe();
    expect(api.post).toHaveBeenCalledOnceWith(
      'combat-styles/preview',
      request,
      {
        forceStateSyncRefresh: false,
      },
    );
  });
});
