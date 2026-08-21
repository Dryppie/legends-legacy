import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../api.service';
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

  upgrade(
    soulstoneUpgradeId: string,
  ): Observable<VersionedMutationResult<SoulstoneUpgradeMutationResult>> {
    return this.api
      .postVersioned<SoulstoneUpgradeMutationResult>(
        'soulstoneUpgrade/upgrade',
        soulstoneUpgradeId,
        {
          stateSyncScopesHandledByResponse: ['soulstones', 'character'],
        },
      )
      .pipe(
        tap(() => {}),
        catchError((e) => {
          return throwError(() => e);
        }),
      );
  }

  resetSoulstoneUpgrades(): Observable<
    VersionedMutationResult<SoulstoneUpgradeMutationResult>
  > {
    return this.api
      .postVersioned<SoulstoneUpgradeMutationResult>(
        'soulstoneUpgrade/reset',
        {},
        {
          stateSyncScopesHandledByResponse: ['soulstones', 'character'],
        },
      )
      .pipe(
        tap(() => {}),
        catchError((e) => {
          return throwError(() => e);
        }),
      );
  }
}
