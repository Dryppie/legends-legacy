import {
  computed,
  effect,
  inject,
  Injectable,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { finalize, forkJoin, Observable, tap } from 'rxjs';
import {
  CombatAreaAccess,
  ONBOARDING_QUEST_CATEGORY,
  QuestJournal,
  QuestStatus,
} from '../../../../shared/models/quest';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { RealtimeSignalDeduper } from '../../real-time/game-realtime/realtime-deduplication';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { AuthService } from '../auth/auth.service';
import { QuestService } from './quest.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';

@Injectable({ providedIn: 'root' })
export class QuestStateService {
  private readonly _journal = signal<QuestJournal>({ quests: [] });
  private readonly _areaAccess = signal<CombatAreaAccess[]>([]);
  private readonly _loaded = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly auth = inject(AuthService);
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private journalRequestEpoch = 0;
  private areaAccessRequestEpoch = 0;
  private lastLogoutCount = 0;

  readonly journal = this._journal.asReadonly();
  readonly areaAccess = this._areaAccess.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly activeQuests = computed(() =>
    this._journal().quests.filter(
      (quest) => quest.status === QuestStatus.Active,
    ),
  );
  readonly completedQuests = computed(() =>
    this._journal().quests.filter(
      (quest) => quest.status === QuestStatus.Completed,
    ),
  );
  readonly pinnedQuest = computed(() => {
    const journal = this._journal();
    return (
      journal.quests.find((quest) => quest.questId === journal.pinnedQuestId) ??
      journal.quests.find((quest) => quest.isPinned) ??
      null
    );
  });
  readonly pinnedObjective = computed(() =>
    this.pinnedQuest()?.objectives.find((objective) => !objective.isCompleted),
  );
  /** True only while a quest from the new-player tutorial line is pinned. */
  readonly isOnboardingQuestPinned = computed(
    () => this.pinnedQuest()?.category === ONBOARDING_QUEST_CATEGORY,
  );
  /**
   * The pinned objective, but only while a tutorial quest is pinned.
   *
   * Onboarding-only UI (scoped recipe lists, forced views, highlights) must key
   * off this rather than `pinnedObjective`, otherwise later quests that reuse
   * the same objective type (e.g. the Tier 2 crafting side quests) re-trigger
   * the tutorial presentation.
   */
  readonly pinnedOnboardingObjective = computed(() =>
    this.isOnboardingQuestPinned() ? this.pinnedObjective() : undefined,
  );

  constructor(
    private readonly api: QuestService,
    private readonly router: Router,
    private readonly events: GameRealtimeEventRegistry,
    private readonly eventBus: EventBusService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register(
      'quests',
      'quests',
      () => this.synchronizeJournal(),
      () => this._loaded(),
    );
    this.stateSync.register(
      'area-access',
      'area-access',
      () => this.synchronizeAreaAccess(),
      () => this._loaded(),
    );
    effect(
      () => {
        const event = this.events.event.QuestJournalChanged();
        if (!event?.journal) return;
        untracked(() => {
          if (
            event.stateVersion > 0 &&
            !this.domainVersions.isCurrent('quests', event.stateVersion)
          ) {
            return;
          }
          this.initialize(event.journal);
          if (event.stateVersion > 0) {
            this.stateSync.acceptSnapshotResponse(
              { quests: event.stateVersion },
              ['quests'],
            );
          }
        });
      },
      { allowSignalWrites: true },
    );

    // Area access and quest availability are both derived server-side from
    // character level, and the level-up event carries neither. Without this the
    // newly unlocked area stays locked until something else refetches.
    effect(
      () => {
        const envelope = this.events.eventEnvelope.CharacterLevelUp();
        const levelUp = envelope?.payload;
        if (!levelUp) return;
        untracked(() => {
          const characterId = this.auth.currentCharacter()?.id;
          if (!characterId || levelUp.characterId !== characterId) return;
          if (!this.eventDeduper.shouldProcess('level-up', envelope)) return;
          this.resyncSilently();
        });
      },
      { allowSignalWrites: true },
    );

    this.lastLogoutCount = this.eventBus.logout();
    effect(
      () => {
        const logoutCount = this.eventBus.logout();
        if (logoutCount === this.lastLogoutCount) return;
        this.lastLogoutCount = logoutCount;
        this.reset();
      },
      { allowSignalWrites: true },
    );
  }

  initialize(journal: QuestJournal): void {
    this.journalRequestEpoch += 1;
    this._journal.set(journal ?? { quests: [] });
    this._loaded.set(true);
    this.stateSync.activate('quests', 'quests');
    this.stateSync.activate('area-access', 'area-access');
    this._loading.set(false);
    this._error.set(null);
  }

  load(): void {
    if (this._loading()) return;
    const requestEpoch = ++this.journalRequestEpoch;
    this._loading.set(true);
    this._error.set(null);
    this.api
      .getJournal()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (journal) => {
          if (requestEpoch !== this.journalRequestEpoch) return;
          this.initialize(journal);
          this.loadAreaAccess();
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to load quests'),
      });
  }

  selectChoice(
    questId: string,
    optionKey: string,
    onComplete?: () => void,
  ): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    this.api
      .selectChoice(questId, optionKey)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          this.applyVersionedJournal(result);
          onComplete?.();
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to select quest choice'),
      });
  }

  acknowledgeWelcome(onComplete?: () => void, onError?: () => void): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    this.api
      .acknowledgeWelcome()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          this.applyVersionedJournal(result);
          onComplete?.();
        },
        error: (error) => {
          this._error.set(
            error?.message ?? 'Failed to start the tutorial. Please try again.',
          );
          onError?.();
        },
      });
  }

  pin(questId: string | null): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    this.api
      .pin(questId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => this.applyVersionedJournal(result),
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to pin quest'),
      });
  }

  navigateToPinnedObjective(): void {
    const quest = this.pinnedQuest();
    if (quest?.choice && !quest.choice.selectedOptionKey) {
      void this.router.navigateByUrl('/game/quests');
      return;
    }

    const route = this.pinnedObjective()?.presentation.destinationRoute;
    if (route) void this.router.navigateByUrl(route);
  }

  accessFor(areaId: string): CombatAreaAccess | null {
    return (
      this._areaAccess().find((access) => access.areaId === areaId) ?? null
    );
  }

  loadAreaAccess(): void {
    const requestEpoch = ++this.areaAccessRequestEpoch;
    this.api.getAreaAccess().subscribe({
      next: (access) => {
        if (requestEpoch === this.areaAccessRequestEpoch) {
          this._areaAccess.set(access);
        }
      },
      error: () => undefined,
    });
  }

  clearError(): void {
    this._error.set(null);
  }

  reportError(message: string): void {
    this._error.set(message);
  }

  reset(): void {
    this.journalRequestEpoch += 1;
    this.areaAccessRequestEpoch += 1;
    this._journal.set({ quests: [] });
    this._areaAccess.set([]);
    this._loaded.set(false);
    this._loading.set(false);
    this._error.set(null);
  }

  /** Re-pull journal and area access together, without touching loading/error UI. */
  private resyncSilently(): void {
    this.synchronize().subscribe({ error: () => undefined });
  }

  private synchronize(): Observable<unknown> {
    const requestEpoch = ++this.journalRequestEpoch;
    const accessRequestEpoch = ++this.areaAccessRequestEpoch;
    return forkJoin({
      journal: this.api.getJournal(),
      access: this.api.getAreaAccess(),
    }).pipe(
      tap({
        next: ({ journal, access }) => {
          if (requestEpoch === this.journalRequestEpoch) {
            this.initialize(journal);
          }
          if (accessRequestEpoch === this.areaAccessRequestEpoch) {
            this._areaAccess.set(access);
          }
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to synchronize quests'),
      }),
    );
  }

  private synchronizeJournal(): Observable<unknown> {
    const requestEpoch = ++this.journalRequestEpoch;
    return this.api.getJournal().pipe(
      tap({
        next: (journal) => {
          if (requestEpoch === this.journalRequestEpoch) {
            this.initialize(journal);
          }
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to synchronize quests'),
      }),
    );
  }

  private synchronizeAreaAccess(): Observable<unknown> {
    const requestEpoch = ++this.areaAccessRequestEpoch;
    return this.api.getAreaAccess().pipe(
      tap({
        next: (access) => {
          if (requestEpoch === this.areaAccessRequestEpoch) {
            this._areaAccess.set(access);
          }
        },
      }),
    );
  }

  private applyVersionedJournal(
    result: VersionedMutationResult<QuestJournal>,
  ): boolean {
    if (
      !this.domainVersions.isCurrent('quests', result.domainVersions['quests'])
    ) {
      return false;
    }

    this.initialize(result.data);
    return true;
  }
}
