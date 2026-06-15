import { Injectable, effect, signal } from '@angular/core';
import { GameEventService } from './game-event.service';
import { AuthService } from '../api/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private readonly initialized = signal(false);

  constructor(
    private auth: AuthService,
    private socket: GameEventService,
  ) {
    effect(
      () => {
        if (!this.initialized()) return;

        if (this.auth.isAuthenticated()) {
          this.socket
            .connect({ kind: 'World' })
            .catch((error) =>
              console.warn('Failed to connect game realtime', error),
            );
        } else {
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
