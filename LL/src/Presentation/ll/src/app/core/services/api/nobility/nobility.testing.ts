import { Provider, signal } from '@angular/core';
import { of } from 'rxjs';
import { NobilityService } from './nobility.service';
import { TimeSyncService } from '../time-sync/time-sync.service';

/** Isolate unrelated component tests from the canonical membership API. */
export function provideFreeNobilityForTests(): Provider[] {
  return [
    { provide: NobilityService, useValue: {
      isNoble: signal(false), equipmentLimit: signal(3), status: signal(null),
      publicAppearance: () => of({ serverTime: new Date().toISOString(), expiresAt: null,
        showBadge: false }),
    } },
    { provide: TimeSyncService, useValue: { now: () => Date.now(), updateFromServerTime: () => undefined } },
  ];
}

