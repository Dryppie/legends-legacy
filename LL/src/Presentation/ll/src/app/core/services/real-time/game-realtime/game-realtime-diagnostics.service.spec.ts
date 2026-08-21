import { NgZone } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { StateSyncDiagnostics } from './state-sync-diagnostics.service';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';

describe('GameRealtimeDiagnostics', () => {
  it('records handled, duplicate, unknown, and failed deliveries', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeDiagnostics,
        { provide: Router, useValue: { url: '/test' } },
        {
          provide: NgZone,
          useValue: new NgZone({ enableLongStackTrace: false }),
        },
        { provide: StateSyncDiagnostics, useValue: {} },
      ],
    });
    const diagnostics = TestBed.inject(GameRealtimeDiagnostics);

    const handled = {
      updateId: 'handled',
      event: 'AccountAccessChanged',
      payload: {},
    };
    diagnostics.recordReceive(handled);
    diagnostics.runHandler(handled, () => undefined, true);

    const duplicate = { updateId: 'duplicate', event: 'RaidUpdated', payload: {} };
    diagnostics.recordReceive(duplicate);
    diagnostics.recordDuplicate(duplicate);

    const unknown = { updateId: 'unknown', event: 'UnknownEvent', payload: {} };
    diagnostics.recordReceive(unknown);
    diagnostics.recordUnknown(unknown);

    const failed = { updateId: 'failed', event: 'LootReceived', payload: {} };
    diagnostics.recordReceive(failed);
    expect(() =>
      diagnostics.runHandler(
        failed,
        () => {
          throw new Error('handler failed');
        },
        true,
      ),
    ).toThrowError('handler failed');

    expect(
      diagnostics.recentEvents().map((event) => event.disposition),
    ).toEqual(['handled', 'duplicate', 'unknown', 'failed']);
    expect(diagnostics.recentEvents()[3].handlerError).toBe('handler failed');
  });

  it('runs handlers without retaining diagnostics in production', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeDiagnostics,
        { provide: Router, useValue: { url: '/test' } },
        {
          provide: NgZone,
          useValue: new NgZone({ enableLongStackTrace: false }),
        },
        { provide: StateSyncDiagnostics, useValue: {} },
      ],
    });
    const diagnostics = TestBed.inject(GameRealtimeDiagnostics);
    const previousEnvironment = (window as any).env;
    (window as any).env = { ...previousEnvironment, environment: 'prod' };

    try {
      const handler = jasmine.createSpy('handler');
      const envelope = {
        updateId: 'production-event',
        event: 'AccountAccessChanged',
        payload: {},
      };

      diagnostics.start();
      diagnostics.recordReceive(envelope);
      diagnostics.runHandler(envelope, handler, true);

      expect(handler).toHaveBeenCalledTimes(1);
      expect(diagnostics.recentEvents()).toEqual([]);
      expect((window as any).__gameSignalRDebug).toBeUndefined();
    } finally {
      (window as any).env = previousEnvironment;
    }
  });
});
