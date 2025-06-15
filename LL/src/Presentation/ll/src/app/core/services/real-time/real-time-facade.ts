import { Injectable, effect } from '@angular/core';
import { GameSocketService } from './game-socket.service';
import { AuthService } from '../api/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class RealTimeFacade {
  constructor(
    private auth: AuthService,
    private socket: GameSocketService,
  ) {
    effect(
      () => {
        /* read the auth signal */
        if (this.auth.isAuthenticated()) {
          this.socket.connect(); // writes isConnected/lastMsg
        } else {
          this.socket.disconnect(); // writes isConnected/lastMsg
        }
      },
      { allowSignalWrites: true },
    );
  }
}
