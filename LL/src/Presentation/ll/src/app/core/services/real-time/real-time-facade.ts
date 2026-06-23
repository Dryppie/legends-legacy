import { Injectable, effect, signal } from '@angular/core';
import { GameEventService } from './game-event.service';
import { AuthService } from '../api/auth/auth.service';
import { GameRealtimeConnectionV2 } from '../real-time-v2/game-realtime-connection-v2.service';
import { GameRealtimeEventRegistryV2 } from '../real-time-v2/game-realtime-event-registry-v2.service';
import { isGameRealtimeV2Enabled } from '../real-time-v2/game-realtime-feature-v2';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private readonly initialized = signal(false);

  constructor(
    private auth: AuthService,
    private socket: GameEventService,
    private gameRealtimeV2: GameRealtimeConnectionV2,
    private gameRealtimeRegistryV2: GameRealtimeEventRegistryV2,
  ) {
    effect(
      () => {
        if (!this.initialized()) return;

        if (this.auth.isAuthenticated()) {
          if (isGameRealtimeV2Enabled()) {
            this.gameRealtimeRegistryV2.initialize();
            this.gameRealtimeV2
              .connect()
              .then(() => {
                if ((window as any).__gameSignalRDebug) {
                  (window as any).__gameSignalRDebug.isConnected = () =>
                    this.gameRealtimeV2.isConnected();
                }
              })
              .catch((error) =>
                console.warn('Failed to connect game realtime v2', error),
              );
          }

          this.socket
            .connect({ kind: 'World' })
            .catch((error) =>
              console.warn('Failed to connect game realtime', error),
            );
        } else {
          this.gameRealtimeRegistryV2.dispose();
          this.gameRealtimeV2
            .disconnect()
            .catch((error) =>
              console.warn('Failed to disconnect game realtime v2', error),
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
