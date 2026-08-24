import { Injectable, effect, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { GameRealtimeConnection } from './game-realtime/game-realtime-connection.service';
import { GameRealtimeEventRegistry } from './game-realtime/game-realtime-event-registry.service';
import { isGameRealtimeEnabled } from './game-realtime/game-realtime-feature';
import { StateSyncCoordinator } from './game-realtime/state-sync-coordinator.service';
import { GameBootstrapStateService } from '../api/game-bootstrap/game-bootstrap-state.service';
import { LootHistoryStateService } from '../api/loot-history/loot-history-state.service';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private readonly initialized = signal(false);
  private readonly initialReady: Promise<void>;
  private resolveInitialReady!: () => void;
  private initialReadySettled = false;

  constructor(
    private auth: AuthService,
    private gameRealtime: GameRealtimeConnection,
    private gameRealtimeRegistry: GameRealtimeEventRegistry,
    private stateSync: StateSyncCoordinator,
    private gameBootstrap: GameBootstrapStateService,
    private lootHistoryState: LootHistoryStateService,
  ) {
    this.initialReady = new Promise<void>((resolve) => {
      this.resolveInitialReady = resolve;
    });

    effect(
      () => {
        if (!this.initialized()) return;

        if (this.auth.isAuthenticated()) {
          this.lootHistoryState.initialize();
          if (isGameRealtimeEnabled()) {
            this.gameRealtimeRegistry.initialize();
            this.stateSync.initialize();
            this.gameRealtime
              .subscribeToWorld()
              .then(async () => {
                await this.loadInitialSnapshot();
                await this.stateSync.reconcile();
                if ((window as any).__gameSignalRDebug) {
                  (window as any).__gameSignalRDebug.isConnected = () =>
                    this.gameRealtime.isConnected();
                }
              })
              .catch(async (error) => {
                console.warn('Failed to connect game realtime', error);
                // HTTP state remains usable while the connection service retries.
                await this.loadInitialSnapshot();
                await this.stateSync.reconcile();
              })
              .finally(() => this.markInitialReady());
          } else {
            this.markInitialReady();
          }
        } else {
          this.markInitialReady();
          this.gameRealtimeRegistry.dispose();
          this.stateSync.dispose();
          this.gameRealtime
            .disconnect()
            .catch((error) =>
              console.warn('Failed to disconnect game realtime', error),
            );
        }
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      const periodicReconciliationEnabled =
        this.initialized() &&
        this.auth.isAuthenticated() &&
        isGameRealtimeEnabled() &&
        this.gameRealtime.connectionStatus() === 'connected';
      this.stateSync.setPeriodicReconciliationEnabled(
        periodicReconciliationEnabled,
      );
    });

    effect(() => {
      if (!this.initialized() || !this.auth.isAuthenticated()) return;
      const reconnectCount = this.gameRealtime.reconnectCount();
      if (reconnectCount > 0) {
        void this.recoverAfterReconnect();
      }
    });
  }

  initialize(): Promise<void> {
    this.initialized.set(true);
    return this.initialReady;
  }

  private async loadInitialSnapshot(): Promise<void> {
    try {
      await firstValueFrom(this.gameBootstrap.load());
    } catch (error) {
      console.warn('Initial game bootstrap failed', error);
    }
  }

  private async recoverAfterReconnect(): Promise<void> {
    try {
      await firstValueFrom(this.gameBootstrap.reload());
    } catch (error) {
      console.warn('Game bootstrap reconnect recovery failed', error);
    }
    await this.stateSync.reconcile();
  }

  private markInitialReady(): void {
    if (this.initialReadySettled) return;
    this.initialReadySettled = true;
    this.resolveInitialReady();
  }
}
