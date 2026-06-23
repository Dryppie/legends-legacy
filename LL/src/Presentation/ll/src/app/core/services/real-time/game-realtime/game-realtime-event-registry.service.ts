import { Injectable, Injector, NgZone, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { CharacterActionsStateService } from '../../api/character-actions/character-actions.state.service';
import { CharacterStateService } from '../../api/character/character-state.service';
import { InventoryStateService } from '../../api/inventory/inventory-state.service';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';
import {
  CharacterSnapshot,
  DungeonRewardsClaimed,
  GameRealtimeEnvelope,
  IdleCombatProcessed,
  InventorySnapshot,
  LootReceived,
  gameRealtimeEventNames,
} from './game-realtime-contracts';
import { GameRealtimeConnection } from './game-realtime-connection.service';
import { isGameRealtimeEnabled } from './game-realtime-feature';
import { GameRealtimeStore } from './game-realtime-store.service';

type Handler = (envelope: GameRealtimeEnvelope) => void;

@Injectable({ providedIn: 'root' })
export class GameRealtimeEventRegistry {
  private readonly connection = inject(GameRealtimeConnection);
  private readonly diagnostics = inject(GameRealtimeDiagnostics);
  private readonly injector = inject(Injector);
  private readonly zone = inject(NgZone);
  private readonly handlers = new Map<string, Handler>();
  private registered = false;
  private subscription?: Subscription;
  private pendingIdleEnvelope: GameRealtimeEnvelope<IdleCombatProcessed> | null =
    null;
  private idleBatchScheduled = false;

  initialize(): void {
    if (!isGameRealtimeEnabled() || this.registered) return;
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
    this.addHandler(gameRealtimeEventNames.dungeonRewardsClaimed, (envelope) => {
      const payload = envelope.payload as DungeonRewardsClaimed;
      this.injector
        .get(GameRealtimeStore)
        .setRewardClaim(payload.claimedLoot ?? []);
    });

    this.addHandler(gameRealtimeEventNames.lootReceived, (envelope) => {
      const payload = envelope.payload as LootReceived;
      this.injector.get(GameRealtimeStore).addLoot(payload.items ?? []);
      this.injector
        .get(InventoryStateService)
        .addOrIncrementMany(payload.items ?? []);
    });

    this.addHandler(gameRealtimeEventNames.inventorySnapshot, (envelope) => {
      const payload = envelope.payload as InventorySnapshot;
      this.injector.get(InventoryStateService).setInventory(payload.items ?? []);
    });

    this.addHandler(gameRealtimeEventNames.characterSnapshot, (envelope) => {
      const payload = envelope.payload as CharacterSnapshot;
      this.injector.get(CharacterStateService).updateCharacter(payload.character);
    });

    this.addHandler(gameRealtimeEventNames.idleCombatProcessed, (envelope) => {
      this.pendingIdleEnvelope = envelope as GameRealtimeEnvelope<IdleCombatProcessed>;
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
            this.injector.get(GameRealtimeStore).setIdleAction(payload.action);
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
      throw new Error(`Duplicate GameRealtime handler registered for ${eventName}`);
    }

    this.handlers.set(eventName, handler);
  }

  private dispatch(envelope: GameRealtimeEnvelope): void {
    const handler = this.handlers.get(envelope.event);
    if (!handler) {
      return;
    }

    this.diagnostics.runHandler(envelope, () => handler(envelope), true);
  }
}
