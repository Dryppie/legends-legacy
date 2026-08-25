import { of } from 'rxjs';
import { DungeonDifficulty } from '../../../../shared/models/enums/dungeonDifficulty';
import { ApiService } from '../api.service';
import { DungeonService } from './dungeon.service';

describe('DungeonService response ownership', () => {
  it('declares the complete scopes returned by each dungeon mutation', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', [
      'postVersioned',
    ]);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    const service = new DungeonService(api);

    service
      .startDungeon({
        dungeonId: 'dungeon-1',
        dungeonTier: DungeonDifficulty.Normal,
      })
      .subscribe();
    service.executeDungeonAction('run-1', { actionId: 'fight' }).subscribe();
    service.dismissFailedDungeonRun().subscribe();
    service.assembleSigil('dungeon-1', 4).subscribe();

    expect(api.postVersioned.calls.argsFor(0)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['dungeons', 'inventory'],
    });
    expect(api.postVersioned.calls.argsFor(1)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['dungeons'],
    });
    expect(api.postVersioned.calls.argsFor(2)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['dungeons'],
    });
    expect(api.postVersioned.calls.argsFor(3)[2]).toEqual({
      stateSyncScopesHandledByResponse: ['dungeons', 'inventory', 'character'],
    });
    expect(api.postVersioned.calls.argsFor(3)[1]).toEqual({ quantity: 4 });
  });
});
