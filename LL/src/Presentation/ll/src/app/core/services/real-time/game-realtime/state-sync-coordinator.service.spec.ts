import { Injector } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
import { defer, of, throwError } from 'rxjs';
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
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
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
    tick();

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
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('character', 'quests', refresh);

    void coordinator.reconcile();
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);
  }));

  it('acknowledges only successful refreshes and retries a failed revision', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    let attempt = 0;
    const refresh = jasmine.createSpy('refresh').and.callFake(() =>
      attempt++ === 0
        ? throwError(() => new Error('offline'))
        : of(undefined),
    );
    coordinator.register('inventory', 'inventory', refresh);

    coordinator.acceptInvalidation({
      scope: 'inventory',
      revision: 8,
      reason: 'test',
    });
    tick(51);
    tick();

    expect(coordinator.status()[0]).toEqual(
      jasmine.objectContaining({
        appliedRevision: 0,
        stale: true,
        retryAttempt: 1,
      }),
    );

    tick(1_000);
    tick();

    expect(refresh).toHaveBeenCalledTimes(2);
    expect(coordinator.status()[0]).toEqual(
      jasmine.objectContaining({
        appliedRevision: 8,
        stale: false,
        retryAttempt: 0,
      }),
    );
  }));

  it('retries an unacknowledged registration when an equal checkpoint arrives', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({
          characterId: 'character',
          revisions: { inventory: 3 },
          serverTimeUtc: '',
        }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    let active = false;
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('inventory', 'inventory', refresh, () => active);

    void coordinator.reconcile();
    tick(51);
    expect(refresh).not.toHaveBeenCalled();

    active = true;
    void coordinator.reconcile();
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);
  }));

  it('forces reconciliation after a mutation response even when its revision is older', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('inventory', 'inventory', refresh);

    coordinator.acceptInvalidation({
      scope: 'inventory',
      revision: 5,
      reason: 'newer-event',
    });
    tick(51);
    tick();
    coordinator.acceptMutationResponse({ inventory: 4 });
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(2);
    expect(coordinator.status()[0].appliedRevision).toBe(5);
  }));

  it('does not repeat an applied realtime revision for a non-forcing mutation response', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('character', 'character', refresh);

    coordinator.acceptInvalidation({
      scope: 'character',
      revision: 5,
      reason: 'realtime',
    });
    tick(51);
    tick();

    coordinator.acceptMutationResponse({ character: 5 }, false);
    tick(51);
    tick();
    expect(refresh).toHaveBeenCalledTimes(1);

    coordinator.acceptMutationResponse({ character: 6 }, false);
    tick(51);
    tick();
    expect(refresh).toHaveBeenCalledTimes(2);
  }));

  it('retries checkpoint reconciliation after a transient failure', fakeAsync(() => {
    let attempts = 0;
    const api = {
      getCheckpoint: () =>
        defer(() => {
          attempts += 1;
          return attempts === 1
            ? throwError(() => new Error('offline'))
            : of({
                characterId: 'character',
                revisions: { character: 2 },
                serverTimeUtc: '',
              });
        }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('character', 'character', refresh);
    coordinator.initialize();

    void coordinator.reconcile();
    tick();
    expect(attempts).toBe(1);

    tick(1_000);
    tick(51);
    tick();

    expect(attempts).toBe(2);
    expect(refresh).toHaveBeenCalledTimes(1);
    coordinator.dispose();
  }));

  it('keeps a replacement registration when the old owner unregisters late', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const oldRefresh = jasmine.createSpy('oldRefresh');
    const newRefresh = jasmine
      .createSpy('newRefresh')
      .and.returnValue(Promise.resolve());

    const unregisterOld = coordinator.register(
      'inventory',
      'inventory-page',
      oldRefresh,
    );
    coordinator.register('inventory', 'inventory-page', newRefresh);
    unregisterOld();
    coordinator.acceptInvalidation({
      scope: 'inventory',
      revision: 1,
      reason: 'test',
    });
    tick(51);
    tick();

    expect(oldRefresh).not.toHaveBeenCalled();
    expect(newRefresh).toHaveBeenCalledTimes(1);
  }));
});
