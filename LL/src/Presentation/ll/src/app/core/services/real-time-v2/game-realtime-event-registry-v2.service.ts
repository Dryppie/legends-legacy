import { Injectable, Injector, NgZone, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { CharacterActionsStateService } from '../api/character-actions/character-actions.state.service';
import { CharacterStateService } from '../api/character/character-state.service';
import { InventoryStateService } from '../api/inventory/inventory-state.service';
import { GameRealtimeDiagnosticsV2 } from './game-realtime-diagnostics-v2.service';
import {
  CharacterSnapshotV2,
  DungeonRewardsClaimedV2,
  GameRealtimeEnvelopeV2,
  IdleCombatProcessedV2,
  InventorySnapshotV2,
  LootReceivedV2,
  gameRealtimeEventNamesV2,
} from './game-realtime-contracts-v2';
import { GameRealtimeConnectionV2 } from './game-realtime-connection-v2.service';
import { isGameRealtimeV2Enabled } from './game-realtime-feature-v2';
import { GameRealtimeStoreV2 } from './game-realtime-store-v2.service';

type Handler = (envelope: GameRealtimeEnvelopeV2) => void;

@Injectable({ providedIn: 'root' })
export class GameRealtimeEventRegistryV2 {
  private readonly connection = inject(GameRealtimeConnectionV2);
  private readonly diagnostics = inject(GameRealtimeDiagnosticsV2);
  private readonly injector = inject(Injector);
  private readonly zone = inject(NgZone);
  private readonly handlers = new Map<string, Handler>();
  private registered = false;
  private subscription?: Subscription;
  private pendingIdleEnvelope: GameRealtimeEnvelopeV2<IdleCombatProcessedV2> | null =
    null;
  private idleBatchScheduled = false;

  initialize(): void {
    if (!isGameRealtimeV2Enabled() || this.registered) return;
    this.registered = true;
    this.registerHandlers();
    this.subscription = this.connection.events$.subscribe((envelope) =>
      this.dispatch(envelope),
    );
  }

  dispose(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.registered = false;
  }

  private registerHandlers(): void {
    this.addHandler(gameRealtimeEventNamesV2.dungeonRewardsClaimed, (envelope) => {
      const payload = envelope.payload as DungeonRewardsClaimedV2;
      this.injector
        .get(GameRealtimeStoreV2)
        .setRewardClaim(payload.claimedLoot ?? []);
    });

    this.addHandler(gameRealtimeEventNamesV2.lootReceived, (envelope) => {
      const payload = envelope.payload as LootReceivedV2;
      this.injector.get(GameRealtimeStoreV2).addLoot(payload.items ?? []);
      this.injector
        .get(InventoryStateService)
        .addOrIncrementMany(payload.items ?? []);
    });

    this.addHandler(gameRealtimeEventNamesV2.inventorySnapshot, (envelope) => {
      const payload = envelope.payload as InventorySnapshotV2;
      this.injector.get(InventoryStateService).setInventory(payload.items ?? []);
    });

    this.addHandler(gameRealtimeEventNamesV2.characterSnapshot, (envelope) => {
      const payload = envelope.payload as CharacterSnapshotV2;
      this.injector.get(CharacterStateService).updateCharacter(payload.character);
    });

    this.addHandler(gameRealtimeEventNamesV2.idleCombatProcessed, (envelope) => {
      this.pendingIdleEnvelope = envelope as GameRealtimeEnvelopeV2<IdleCombatProcessedV2>;
      if (this.idleBatchScheduled) return;

      this.idleBatchScheduled = true;
      this.zone.runOutsideAngular(() => {
        setTimeout(() => {
          const pending = this.pendingIdleEnvelope;
          this.pendingIdleEnvelope = null;
          this.idleBatchScheduled = false;
          if (!pending) return;

          this.zone.run(() => {
            const payload = pending.payload;
            this.injector.get(GameRealtimeStoreV2).setIdleAction(payload.action);
            this.injector
              .get(CharacterActionsStateService)
              .applyRealtimeIdleCombat(payload.action);
          });
        }, 0);
      });
    });
  }

  private addHandler(eventName: string, handler: Handler): void {
    if (this.handlers.has(eventName)) {
      throw new Error(`Duplicate GameRealtimeV2 handler registered for ${eventName}`);
    }

    this.handlers.set(eventName, handler);
  }

  private dispatch(envelope: GameRealtimeEnvelopeV2): void {
    const handler = this.handlers.get(envelope.event);
    if (!handler) {
      return;
    }

    this.diagnostics.runHandler(envelope, () => handler(envelope), true);
  }
}
