import { Injectable, effect } from '@angular/core';
import { GameEventService } from './game-event.service';
import { AuthService } from '../api/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private initialized = false;

  constructor(
    private auth: AuthService,
    private socket: GameEventService,
  ) {
    effect(
      () => {
        if (!this.initialized) return;

        if (this.auth.isAuthenticated()) {
          this.socket.connect({ kind: 'World' });
        } else {
          this.socket.disconnect();
        }
      },
      { allowSignalWrites: true },
    );
  }

  initialize() {
    this.initialized = true;
  }
}
