import { Injectable, effect } from '@angular/core';
import { GameSocketService } from './game-socket.service';
import { AuthService } from '../api/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  private initialized = false;

  constructor(
    private auth: AuthService,
    private socket: GameSocketService,
  ) {
    effect(
      () => {
        if (!this.initialized) return;

        if (this.auth.isAuthenticated()) {
          this.socket.connect();
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
