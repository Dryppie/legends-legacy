import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { InventoryStateService } from '../../api/inventory/inventory-state.service';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';
import { GameRealtimeConnection } from './game-realtime-connection.service';
import { GameRealtimeEnvelope } from './game-realtime-contracts';
import { GameRealtimeEventRegistry } from './game-realtime-event-registry.service';
import { GameRealtimeStore } from './game-realtime-store.service';

describe('GameRealtimeEventRegistry', () => {
  it('ignores a replayed envelope after reconnect and reports unknown events', () => {
    const events = new Subject<GameRealtimeEnvelope>();
    const diagnostics = jasmine.createSpyObj<GameRealtimeDiagnostics>(
      'GameRealtimeDiagnostics',
      ['runHandler', 'recordDuplicate', 'recordUnknown'],
    );
    diagnostics.runHandler.and.callFake(
      (_envelope: GameRealtimeEnvelope, handler: () => void) => handler(),
    );

    TestBed.configureTestingModule({
      providers: [
        GameRealtimeEventRegistry,
        {
          provide: GameRealtimeConnection,
          useValue: { events$: events.asObservable() },
        },
        {
          provide: GameRealtimeDiagnostics,
          useValue: diagnostics,
        },
      ],
    });

    const previousEnvironment = (window as any).env;
    (window as any).env = { gameSignalREnabled: 'true' };

    try {
      const registry = TestBed.inject(GameRealtimeEventRegistry);
      registry.initialize();

      events.next({
        updateId: 'access-update',
        event: 'AccountAccessChanged',
        payload: {
          accountId: 'account-id',
          reason: 'revoked',
          occurredAtUtc: '2026-08-21T12:00:00Z',
        },
      });
      events.next({
        updateId: 'unknown-update',
        event: 'UnknownRealtimeEvent',
        payload: {},
      });
      events.next({
        updateId: 'access-update',
        event: 'AccountAccessChanged',
        payload: {
          accountId: 'account-id',
          reason: 'duplicate',
          occurredAtUtc: '2026-08-21T12:00:01Z',
        },
      });

      expect(registry.event.AccountAccessChanged()?.reason).toBe('revoked');
      expect(
        registry.eventEnvelope.AccountAccessChanged()?.updateId,
      ).toBe('access-update');
      expect(diagnostics.recordDuplicate).toHaveBeenCalledTimes(1);
      expect(diagnostics.recordUnknown).toHaveBeenCalledTimes(1);

      registry.dispose();
      expect(registry.event.AccountAccessChanged()).toBeNull();
    } finally {
      (window as any).env = previousEnvironment;
    }
  });

  it('records loot without applying the grant on top of the inventory snapshot', () => {
    const events = new Subject<GameRealtimeEnvelope>();
    const store = jasmine.createSpyObj<GameRealtimeStore>('GameRealtimeStore', [
      'addLoot',
    ]);
    const inventory = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['applyInventoryGrant', 'setInventory'],
    );

    TestBed.configureTestingModule({
      providers: [
        GameRealtimeEventRegistry,
        {
          provide: GameRealtimeConnection,
          useValue: { events$: events.asObservable() },
        },
        {
          provide: GameRealtimeDiagnostics,
          useValue: {
            runHandler: (
              _envelope: GameRealtimeEnvelope,
              handler: () => void,
            ) => handler(),
            recordDuplicate: () => undefined,
            recordUnknown: () => undefined,
          },
        },
        { provide: GameRealtimeStore, useValue: store },
        { provide: InventoryStateService, useValue: inventory },
      ],
    });

    const previousEnvironment = (window as any).env;
    (window as any).env = { gameSignalREnabled: 'true' };

    try {
      const registry = TestBed.inject(GameRealtimeEventRegistry);
      registry.initialize();

      const ore = { quantity: 13 } as InventoryItem;
      events.next({
        event: 'LootReceived',
        occurredAt: '2026-08-18T12:00:00Z',
        payload: {
          characterId: 'character-id',
          items: [ore],
          source: 'combat-reward',
          location: 'Lumo Ruins',
          grantId: 'grant-id',
        },
      });

      expect(store.addLoot).toHaveBeenCalledOnceWith(
        [ore],
        '2026-08-18T12:00:00Z',
        'combat-reward',
        'Lumo Ruins',
        'grant-id',
      );
      expect(inventory.applyInventoryGrant).not.toHaveBeenCalled();
    } finally {
      (window as any).env = previousEnvironment;
    }
  });
});
