import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import {
  catchError,
  Observable,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { SoulstoneUpgradeView } from '../../../../shared/models/soulstones/soulstone-upgrade-view';

@Injectable({
  providedIn: 'root',
})
export class SoulstoneUpgradeService {
  private readonly refresh$ = new Subject<void>();
  private readonly soulstoneUpgradesObservable$ = this.refresh$.pipe(
    startWith(void 0),
    switchMap(() => this.api.get('soulstoneUpgrade').pipe()),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  constructor(private readonly api: ApiService) {}

  get soulstoneUpgrades$(): Observable<SoulstoneUpgradeView[]> {
    return this.soulstoneUpgradesObservable$;
  }

  upgrade(soulstoneUpgradeId: string): Observable<boolean> {
    return this.api.post('soulstoneUpgrade/upgrade', soulstoneUpgradeId).pipe(
      tap(() => {}),
      catchError((e) => {
        return throwError(() => e);
      }),
    );
  }

  resetSoulstoneUpgrades() {
    return this.api.post('soulstoneUpgrade/reset').pipe(
      tap(() => {}),
      catchError((e) => {
        return throwError(() => e);
      }),
    );
  }
}
