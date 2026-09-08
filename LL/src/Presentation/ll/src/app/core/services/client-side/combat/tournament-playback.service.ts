import { Injectable, inject } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';
import { ColosseumService } from '../../api/colosseum/colosseum.service';
import {
  TournamentCombatFrame,
  TournamentPlaybackBundle,
} from '../../../../shared/models/Dtos/colosseum/tournamentGrounds';
import { colosseumFrameAtTick } from './colosseum-playback';

@Injectable({ providedIn: 'root' })
export class TournamentPlaybackService {
  private readonly colosseum = inject(ColosseumService);
  private readonly bundles = new Map<
    string,
    Observable<TournamentPlaybackBundle>
  >();

  getBundle(
    tournamentId: string,
    matchId: string,
    etag: string,
  ): Observable<TournamentPlaybackBundle> {
    const key = `${matchId}:${etag}`;
    const cached = this.bundles.get(key);
    if (cached) return cached;

    for (const existingKey of this.bundles.keys()) {
      if (existingKey.startsWith(`${matchId}:`))
        this.bundles.delete(existingKey);
    }
    const request = this.colosseum
      .getTournamentMatchPlaybackBundle(tournamentId, matchId)
      .pipe(
        catchError((error: unknown) => {
          this.bundles.delete(key);
          return throwError(() => error);
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    this.bundles.set(key, request);
    return request;
  }

  frameAtTick(
    bundle: TournamentPlaybackBundle,
    tick: number,
  ): TournamentCombatFrame {
    return colosseumFrameAtTick(bundle, tick);
  }
}
