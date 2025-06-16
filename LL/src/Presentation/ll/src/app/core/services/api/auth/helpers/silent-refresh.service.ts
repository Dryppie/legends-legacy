import { Injectable } from '@angular/core';
import { Observable, switchMap, timer } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SilentRefreshService {
  /**
   * Arms a one-shot silent-refresh.
   *
   * @param exp      absolute JWT expiry (unix-seconds)
   * @param refresh  () => Observable<boolean> | Promise<boolean>
   * @param logout   () => void      called when refresh fails
   * @return         disposer – call to cancel the timer
   */
  schedule(
    exp: number,
    refresh: () => Observable<boolean> | Promise<boolean>,
    logout: () => void,
  ): () => void {
    const now = Date.now() / 1000;
    const delay = Math.max((exp - now) * 1000 * 0.85, 5_000); // 85 %

    const sub = timer(delay)
      .pipe(
        switchMap(() => refresh()), // works for Observable OR Promise
      )
      .subscribe((ok) => {
        if (!ok) logout();
      });

    return () => sub.unsubscribe(); // disposer
  }
}
