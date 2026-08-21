import { fakeAsync, tick } from '@angular/core/testing';
import { StateSyncDiagnostics } from './state-sync-diagnostics.service';

describe('StateSyncDiagnostics', () => {
  it('attributes immediate GETs and refresh callbacks to the latest mutation', fakeAsync(() => {
    const diagnostics = new StateSyncDiagnostics();

    diagnostics.recordMutation(
      'POST',
      '/api/v1/inventory/scrap',
      { inventory: 3 },
      ['inventory'],
    );
    diagnostics.recordGet('/api/v1/inventory');
    diagnostics.recordRefresh('inventory', 'inventory');

    expect(diagnostics.snapshot()).toEqual(
      jasmine.objectContaining({
        mutationCount: 1,
        candidateFollowUpGetCount: 1,
        refreshCallbackCount: 1,
      }),
    );
    expect(diagnostics.snapshot().mutations[0]).toEqual(
      jasmine.objectContaining({
        candidateFollowUpGets: ['/api/v1/inventory'],
        refreshCallbacks: [{ scope: 'inventory', key: 'inventory' }],
      }),
    );

    tick(2_001);
    diagnostics.recordGet('/api/v1/character');
    expect(diagnostics.snapshot().candidateFollowUpGetCount).toBe(1);
  }));

  it('does not retain mutation traces in production', () => {
    const previousEnvironment = (window as any).env;
    (window as any).env = { ...previousEnvironment, environment: 'prod' };

    try {
      const diagnostics = new StateSyncDiagnostics();
      diagnostics.recordMutation('POST', '/api/v1/test', { inventory: 1 }, []);
      diagnostics.recordGet('/api/v1/inventory');
      diagnostics.recordRefresh('inventory', 'inventory');

      expect(diagnostics.snapshot()).toEqual({
        mutationCount: 0,
        candidateFollowUpGetCount: 0,
        refreshCallbackCount: 0,
        mutations: [],
      });
    } finally {
      (window as any).env = previousEnvironment;
    }
  });
});
