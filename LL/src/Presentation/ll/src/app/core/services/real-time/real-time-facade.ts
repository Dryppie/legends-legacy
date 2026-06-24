import { Injectable, effect, signal } from '@angular/core';
import { GameEventService } from './game-event.service';
import { AuthService } from '../api/auth/auth.service';
import { GameRealtimeConnection } from './game-realtime/game-realtime-connection.service';
import { GameRealtimeEventRegistry } from './game-realtime/game-realtime-event-registry.service';
import { isGameRealtimeEnabled } from './game-realtime/game-realtime-feature';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private readonly initialized = signal(false);

  constructor(
    private auth: AuthService,
    private socket: GameEventService,
    private gameRealtime: GameRealtimeConnection,
    private gameRealtimeRegistry: GameRealtimeEventRegistry,
  ) {
    effect(
      () => {
        if (!this.initialized()) return;

        if (this.auth.isAuthenticated()) {
          if (isGameRealtimeEnabled()) {
            this.gameRealtimeRegistry.initialize();
            this.gameRealtime
              .connect()
              .then(() => {
                if ((window as any).__gameSignalRDebug) {
                  (window as any).__gameSignalRDebug.isConnected = () =>
                    this.gameRealtime.isConnected();
                }
              })
              .catch((error) =>
                console.warn('Failed to connect game realtime', error),
              );
          }

          this.socket
            .connect({ kind: 'World' })
            .catch((error) =>
              console.warn('Failed to connect game realtime', error),
            );
        } else {
          this.gameRealtimeRegistry.dispose();
          this.gameRealtime
            .disconnect()
            .catch((error) =>
              console.warn('Failed to disconnect game realtime', error),
            );
          this.socket
            .disconnect()
            .catch((error) =>
              console.warn('Failed to disconnect game realtime', error),
            );
        }
      },
      { allowSignalWrites: true },
    );
  }

  initialize() {
    this.initialized.set(true);
  }
}
