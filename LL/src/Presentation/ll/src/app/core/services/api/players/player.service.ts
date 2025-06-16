import { Injectable } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class PlayerService {
  constructor(private api: ApiService) {}

  getOnlinePlayerCount(): Observable<number> {
    return this.api.get('player/onlineCount').pipe(
      map((playerCount) => {
        return playerCount;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get online player count'));
      }),
    );
  }
}
