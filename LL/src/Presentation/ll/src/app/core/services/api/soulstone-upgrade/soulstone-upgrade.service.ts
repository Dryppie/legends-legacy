import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, Observable, tap, throwError } from 'rxjs';
import {
  SoulstoneUpgradeMutationResult,
  SoulstoneUpgradeView,
} from '../../../../shared/models/soulstones/soulstone-upgrade-view';

@Injectable({
  providedIn: 'root',
})
export class SoulstoneUpgradeService {
  constructor(private readonly api: ApiService) {}

  getSoulstoneUpgrades(): Observable<SoulstoneUpgradeView[]> {
    return this.api.get('soulstoneUpgrade').pipe(
      tap(() => {}),
      catchError((e) => {
        return throwError(() => e);
      }),
    );
  }

  upgrade(soulstoneUpgradeId: string): Observable<SoulstoneUpgradeMutationResult> {
    return this.api.post('soulstoneUpgrade/upgrade', soulstoneUpgradeId).pipe(
      tap(() => {}),
      catchError((e) => {
        return throwError(() => e);
      }),
    );
  }

  resetSoulstoneUpgrades(): Observable<SoulstoneUpgradeMutationResult> {
    return this.api.post('soulstoneUpgrade/reset').pipe(
      tap(() => {}),
      catchError((e) => {
        return throwError(() => e);
      }),
    );
  }
}
