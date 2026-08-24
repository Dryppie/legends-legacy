import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { GameRealtimeStore } from '../../real-time/game-realtime/game-realtime-store.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { LootHistoryService } from './loot-history.service';

@Injectable({ providedIn: 'root' })
export class LootHistoryStateService {
  private initialized = false;

  constructor(
    private readonly api: LootHistoryService,
    private readonly store: GameRealtimeStore,
    private readonly stateSync: StateSyncCoordinator,
  ) {}

  initialize(): void {
    if (this.initialized) return;
    this.initialized = true;
    this.stateSync.register(
      'loot-history',
      'loot-history',
      () => this.reload(),
      () => true,
      false,
    );
  }

  reload(): Observable<LootHistoryEntry[]> {
    return this.api
      .getRecent()
      .pipe(tap((entries) => this.store.setLootHistory(entries)));
  }

  clear(): Observable<number> {
    return this.api.clear().pipe(tap(() => this.store.clearLootHistory()));
  }
}
