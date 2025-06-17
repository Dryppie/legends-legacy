import {
  Subscription,
  catchError,
  expand,
  EMPTY,
  timer,
  mergeMap,
  Observable,
} from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CharacterActionsPollingService {
  private sub: Subscription | null = null;

  start(
    fetch: () => Observable<CharacterActionDto | null>,
    onUpdate: (action: CharacterActionDto | null) => void,
  ): void {
    this.stop(); // ensure only one poller is active

    this.sub = fetch()
      .pipe(
        expand((action) => {
          if (
            !action ||
            action.isDeleted ||
            action.characterActionType === CharacterActionType.Idle
          ) {
            return EMPTY;
          }

          const updatedAt = new Date(action.updatedAt).getTime();
          const now = Date.now();

          const nextDelay =
            action.characterActionType === CharacterActionType.Combat
              ? Math.max(updatedAt - now, 0)
              : Math.max(
                  environment.baseDuration * 1000 - (now - updatedAt),
                  0,
                );

          return timer(nextDelay).pipe(mergeMap(() => fetch()));
        }),
        catchError((err) => {
          console.error('Polling error:', err);
          return EMPTY;
        }),
      )
      .subscribe(onUpdate);
  }

  stop(): void {
    this.sub?.unsubscribe();
    this.sub = null;
  }
}
