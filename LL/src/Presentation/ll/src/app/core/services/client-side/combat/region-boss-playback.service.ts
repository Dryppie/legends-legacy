import { Injectable, inject } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';
import {
  RegionBossPlaybackBundle,
  RegionBossService,
} from '../../api/region-boss/region-boss.service';
import { TowerCombatFrame } from '../../api/world-tower/world-tower.service';

@Injectable({ providedIn: 'root' })
export class RegionBossPlaybackService {
  private readonly regionBosses = inject(RegionBossService);
  private readonly bundles = new Map<
    string,
    Observable<RegionBossPlaybackBundle>
  >();

  getBundle(runId: string): Observable<RegionBossPlaybackBundle> {
    const cached = this.bundles.get(runId);
    if (cached) return cached;

    const request = this.regionBosses.getPlaybackBundle(runId).pipe(
      catchError((error: unknown) => {
        this.bundles.delete(runId);
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.bundles.set(runId, request);
    return request;
  }

  frameAtTick(
    bundle: RegionBossPlaybackBundle,
    tick: number,
  ): TowerCombatFrame {
    if (!bundle.frames.length) {
      throw new Error('Region Boss playback bundle contains no frames.');
    }

    let low = 0;
    let high = bundle.frames.length - 1;
    while (low < high) {
      const middle = low + Math.floor((high - low + 1) / 2);
      if (bundle.frames[middle].tick <= tick) low = middle;
      else high = middle - 1;
    }

    const frame = bundle.frames[low];
    return {
      sequence: frame.sequence,
      tick: frame.tick,
      friendly: frame.friendly,
      hostile: frame.hostile,
      entityStats: frame.entityStats,
      events: [],
      isFinal: frame.isFinal,
      outcome: null,
    };
  }
}
