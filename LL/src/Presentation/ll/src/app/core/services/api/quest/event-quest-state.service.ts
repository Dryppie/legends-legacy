import { effect, Injectable, signal, untracked } from '@angular/core';
import { finalize } from 'rxjs';
import { EventQuestJournal } from '../../../../shared/models/event-quest';
import { GameEventService } from '../../real-time/game-event.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { EventQuestService } from './event-quest.service';

@Injectable({ providedIn: 'root' })
export class EventQuestStateService {
  private readonly _journal = signal<EventQuestJournal>({ events: [] });
  private readonly _loaded = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private refreshTimer: number | null = null;
  private lastLogoutCount = 0;

  readonly journal = this._journal.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  constructor(
    private readonly api: EventQuestService,
    events: GameEventService,
    eventBus: EventBusService,
  ) {
    effect(
      () => {
        if (!events.event.EventQuestChangedMsg()) return;
        untracked(() => this.scheduleRefresh());
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

  load(): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    this.api
      .getJournal()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (journal) => {
          this._journal.set(journal ?? { events: [] });
          this._loaded.set(true);
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to load event quests'),
      });
  }

  claim(eventQuestId: string): void {
    this.performClaim(
      () => this.api.claim(eventQuestId),
      'Failed to claim event rewards',
    );
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
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (journal) => this._journal.set(journal),
        error: (error) =>
          this._error.set(error?.message ?? errorMessage),
      });
  }

  private reset(): void {
    if (this.refreshTimer !== null) window.clearTimeout(this.refreshTimer);
    this.refreshTimer = null;
    this._journal.set({ events: [] });
    this._loaded.set(false);
    this._loading.set(false);
    this._error.set(null);
  }

  private scheduleRefresh(): void {
    if (this.refreshTimer !== null) window.clearTimeout(this.refreshTimer);
    this.refreshTimer = window.setTimeout(() => {
      this.refreshTimer = null;
      this.load();
    }, 750);
  }
}
