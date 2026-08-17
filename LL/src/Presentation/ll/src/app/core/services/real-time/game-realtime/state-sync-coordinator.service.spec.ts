import { Injector } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
import { of } from 'rxjs';
import { StateSyncService } from '../../api/state-sync/state-sync.service';
import { StateSyncCoordinator } from './state-sync-coordinator.service';

describe('StateSyncCoordinator', () => {
  it('coalesces newer revisions and ignores duplicate or stale invalidations', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = {
      get: () => api,
    } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine.createSpy('refresh');
    coordinator.register('character', 'inventory', refresh);

    coordinator.acceptInvalidation(
      { scope: 'character', revision: 2, reason: 'first' },
      'update-1',
    );
    coordinator.acceptInvalidation(
      { scope: 'character', revision: 3, reason: 'newer' },
      'update-2',
    );
    coordinator.acceptInvalidation(
      { scope: 'character', revision: 4, reason: 'duplicate' },
      'update-2',
    );
    tick(51);

    expect(refresh).toHaveBeenCalledTimes(1);
  }));

  it('reconciles a missed checkpoint revision through the same resource callback', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({
          characterId: 'character',
          revisions: { character: 5 },
          serverTimeUtc: '2026-08-17T10:00:00Z',
        }),
    } as unknown as StateSyncService;
    const injector = {
      get: () => api,
    } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine.createSpy('refresh');
    coordinator.register('character', 'quests', refresh);

    void coordinator.reconcile();
    tick(51);

    expect(refresh).toHaveBeenCalledTimes(1);
  }));
});
