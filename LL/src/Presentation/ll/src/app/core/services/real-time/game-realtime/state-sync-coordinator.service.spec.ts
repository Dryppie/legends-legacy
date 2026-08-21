import { Injector } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
import { defer, of, throwError } from 'rxjs';
import { StateSyncService } from '../../api/state-sync/state-sync.service';
import { StateSyncCoordinator } from './state-sync-coordinator.service';

describe('StateSyncCoordinator', () => {
  it('reconciles on focus only after a long suspension', fakeAsync(() => {
    const getCheckpoint = jasmine
      .createSpy()
      .and.returnValue(
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
      );
    const injector = {
      get: () => ({ getCheckpoint }),
    } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    coordinator.initialize();

    window.dispatchEvent(new Event('blur'));
    tick(1_000);
    window.dispatchEvent(new Event('focus'));
    tick();
    expect(getCheckpoint).not.toHaveBeenCalled();

    window.dispatchEvent(new Event('blur'));
    tick(5 * 60_000);
    window.dispatchEvent(new Event('focus'));
    tick();
    expect(getCheckpoint).toHaveBeenCalledTimes(1);

    coordinator.dispose();
  }));

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

  it('does not refresh twice when a semantic event repeats the mutation version', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('tournament', 'tournament-grounds', refresh);

    coordinator.acceptMutationResponse({ tournament: 5 });
    tick(51);
    tick();
    coordinator.acceptDomainVersion('tournament', 5, 'semantic-5');
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);

    coordinator.acceptDomainVersion('tournament', 6, 'semantic-6');
    tick(51);
    tick();
    expect(refresh).toHaveBeenCalledTimes(2);
  }));

  it('accepts a lower revision after an audience identity changes', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('guild', 'guild', refresh);

    coordinator.acceptInvalidation({
      scope: 'guild',
      revision: 9,
      reason: 'old-guild',
    });
    tick(51);
    tick();

    coordinator.resetScope('guild');
    coordinator.acceptInvalidation({
      scope: 'guild',
      revision: 1,
      reason: 'new-guild',
    });
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(2);
    expect(coordinator.latestRevision('guild')).toBe(1);
    expect(coordinator.status()[0].appliedRevision).toBe(1);
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
    const refresh = jasmine
      .createSpy('refresh')
      .and.callFake(() =>
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

  it('refreshes a stale registration when its owner becomes active', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    let active = false;
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('inventory', 'inventory', refresh, () => active);

    coordinator.acceptInvalidation({
      scope: 'inventory',
      revision: 4,
      reason: 'while-inactive',
    });
    tick(51);
    expect(refresh).not.toHaveBeenCalled();

    active = true;
    coordinator.activate('inventory', 'inventory');
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(coordinator.status()[0].appliedRevision).toBe(4);
  }));

  it('passes the target revision to refresh owners', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('guild', 'guild', refresh);

    coordinator.acceptInvalidation({
      scope: 'guild',
      revision: 9,
      reason: 'new-guild-state',
    });
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledOnceWith({
      scope: 'guild',
      key: 'guild',
      targetRevision: 9,
    });
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

  it('does not refetch a scope whose mutation snapshot was already applied', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('essences', 'essences', refresh);

    coordinator.acceptInvalidation({
      scope: 'essences',
      revision: 5,
      reason: 'spend-dust',
    });
    coordinator.acceptMutationResponse({ essences: 5 }, true, ['essences']);
    tick(51);
    tick();

    expect(refresh).not.toHaveBeenCalled();
    expect(coordinator.status()[0].appliedRevision).toBe(5);

    coordinator.acceptInvalidation({
      scope: 'essences',
      revision: 6,
      reason: 'newer-mutation',
    });
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);
  }));

  it('reconciles a response-owned scope when the store rejects its mutation body', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const refresh = jasmine
      .createSpy('refresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('colosseum', 'colosseum', refresh);

    coordinator.rejectMutationResponse('colosseum', 5);
    coordinator.acceptMutationResponse({ colosseum: 5 }, true, ['colosseum']);
    tick(51);
    tick();

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(coordinator.status()[0].appliedRevision).toBe(5);
  }));

  it('accepts bootstrap versions only for resources included in the snapshot', fakeAsync(() => {
    const api = {
      getCheckpoint: () =>
        of({ characterId: 'character', revisions: {}, serverTimeUtc: '' }),
    } as unknown as StateSyncService;
    const injector = { get: () => api } as unknown as Injector;
    const coordinator = new StateSyncCoordinator(injector);
    const characterRefresh = jasmine
      .createSpy('characterRefresh')
      .and.returnValue(Promise.resolve());
    const inventoryRefresh = jasmine
      .createSpy('inventoryRefresh')
      .and.returnValue(Promise.resolve());
    coordinator.register('character', 'character', characterRefresh);
    coordinator.register('inventory', 'inventory', inventoryRefresh);

    coordinator.acceptSnapshotResponse({ character: 4, inventory: 7 }, [
      'character',
    ]);
    coordinator.acceptInvalidation({
      scope: 'inventory',
      revision: 7,
      reason: 'checkpoint',
    });
    tick(51);
    tick();

    expect(characterRefresh).not.toHaveBeenCalled();
    expect(inventoryRefresh).toHaveBeenCalledTimes(1);
    expect(coordinator.status()).toContain(
      jasmine.objectContaining({ scope: 'character', appliedRevision: 4 }),
    );
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
