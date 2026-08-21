import { effect, Injectable, signal, untracked } from '@angular/core';
import { finalize, Observable, tap } from 'rxjs';
import { EventQuestJournal } from '../../../../shared/models/event-quest';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { EventQuestService } from './event-quest.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';

@Injectable({ providedIn: 'root' })
export class EventQuestStateService {
  private readonly _journal = signal<EventQuestJournal>({ events: [] });
  private readonly _loaded = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private loadEpoch = 0;
  private refreshAfterCurrentRequest = false;
  private lastLogoutCount = 0;
  private activeViews = 0;
  private dirty = false;
  private changeVersion = 0;

  readonly journal = this._journal.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  constructor(
    private readonly api: EventQuestService,
    events: GameRealtimeEventRegistry,
    eventBus: EventBusService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register(
      'event-quests',
      'event-quests',
      () => this.synchronize(),
      () => this._loaded() && this.activeViews > 0,
    );
    effect(
      () => {
        if (!events.event.EventQuestChanged()) return;
        untracked(() => {
          this.changeVersion += 1;
          this.dirty = true;
          if (this.activeViews > 0) this.load(true);
        });
      },
      { allowSignalWrites: true },
    );

    this.lastLogoutCount = eventBus.logout();
    effect(
      () => {
        const logoutCount = eventBus.logout();
        if (logoutCount === this.lastLogoutCount) return;
        this.lastLogoutCount = logoutCount;
        this.reset();
      },
      { allowSignalWrites: true },
    );
  }

  load(force = false): void {
    if (this._loading()) {
      if (force) this.refreshAfterCurrentRequest = true;
      return;
    }
    const requestEpoch = ++this.loadEpoch;
    const requestChangeVersion = this.changeVersion;
    this._loading.set(true);
    this.refreshAfterCurrentRequest = false;
    this._error.set(null);
    this.api
      .getJournal()
      .pipe(
        finalize(() => {
          if (requestEpoch !== this.loadEpoch) return;
          this._loading.set(false);
          if (this.refreshAfterCurrentRequest) {
            this.refreshAfterCurrentRequest = false;
            this.load(true);
          }
        }),
      )
      .subscribe({
        next: (journal) => {
          if (requestEpoch !== this.loadEpoch) return;
          this._journal.set(journal ?? { events: [] });
          this._loaded.set(true);
          this.stateSync.activate('event-quests', 'event-quests');
          if (requestChangeVersion === this.changeVersion) this.dirty = false;
        },
        error: (error) => {
          if (requestEpoch === this.loadEpoch) {
            this._error.set(error?.message ?? 'Failed to load event quests');
          }
        },
      });
  }

  private synchronize(): Observable<unknown> {
    const requestEpoch = ++this.loadEpoch;
    const requestChangeVersion = this.changeVersion;
    this._loading.set(true);
    this._error.set(null);
    return this.api.getJournal().pipe(
      tap({
        next: (journal) => {
          if (requestEpoch !== this.loadEpoch) return;
          this._journal.set(journal ?? { events: [] });
          this._loaded.set(true);
          this.stateSync.activate('event-quests', 'event-quests');
          if (requestChangeVersion === this.changeVersion) this.dirty = false;
        },
        error: (error) => {
          if (requestEpoch === this.loadEpoch) {
            this._error.set(error?.message ?? 'Failed to load event quests');
          }
        },
      }),
      finalize(() => {
        if (requestEpoch === this.loadEpoch) this._loading.set(false);
      }),
    );
  }

  claim(eventQuestId: string): void {
    this.performClaim(
      () => this.api.claim(eventQuestId),
      'Failed to claim event rewards',
    );
  }

  activateView(): void {
    this.activeViews += 1;
    this.stateSync.activate('event-quests', 'event-quests');
    if (this.activeViews === 1 && this.dirty) this.load(true);
  }

  deactivateView(): void {
    this.activeViews = Math.max(0, this.activeViews - 1);
  }

  claimMilestone(eventQuestId: string, milestoneKey: string): void {
    this.performClaim(
      () => this.api.claimMilestone(eventQuestId, milestoneKey),
      'Failed to claim milestone rewards',
    );
  }

  claimAllMilestones(eventQuestId: string): void {
    this.performClaim(
      () => this.api.claimAllMilestones(eventQuestId),
      'Failed to claim milestone rewards',
    );
  }

  private performClaim(
    request: () => ReturnType<EventQuestService['claim']>,
    errorMessage: string,
  ): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    request()
      .pipe(
        finalize(() => {
          this._loading.set(false);
          if (this.refreshAfterCurrentRequest) {
            this.refreshAfterCurrentRequest = false;
            this.load(true);
          }
        }),
      )
      .subscribe({
        next: (result) => {
          if (
            !this.domainVersions.isCurrent(
              'event-quests',
              result.domainVersions['event-quests'],
            )
          ) {
            return;
          }
          this.loadEpoch += 1;
          this._journal.set(result.data);
          this._loaded.set(true);
          this.dirty = false;
        },
        error: (error) => this._error.set(error?.message ?? errorMessage),
      });
  }

  private reset(): void {
    this.loadEpoch += 1;
    this.refreshAfterCurrentRequest = false;
    this.activeViews = 0;
    this.changeVersion += 1;
    this.dirty = false;
    this._journal.set({ events: [] });
    this._loaded.set(false);
    this._loading.set(false);
    this._error.set(null);
  }
}
