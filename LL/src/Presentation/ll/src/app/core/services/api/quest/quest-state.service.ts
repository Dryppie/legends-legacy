import {
  computed,
  effect,
  inject,
  Injectable,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  CombatAreaAccess,
  ONBOARDING_QUEST_CATEGORY,
  QuestJournal,
  QuestStatus,
} from '../../../../shared/models/quest';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';
import { GameEventService } from '../../real-time/game-event.service';
import { AuthService } from '../auth/auth.service';
import { QuestService } from './quest.service';

@Injectable({ providedIn: 'root' })
export class QuestStateService {
  private readonly _journal = signal<QuestJournal>({ quests: [] });
  private readonly _areaAccess = signal<CombatAreaAccess[]>([]);
  private readonly _loaded = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly auth = inject(AuthService);
  private readonly eventDeduper = new GameEventDeduper();
  private lastLogoutCount = 0;
  private lastReconnectCount = 0;

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
    private readonly events: GameEventService,
    private readonly eventBus: EventBusService,
  ) {
    effect(
      () => {
        const event = this.events.event.QuestJournalChangedMsg();
        if (!event?.journal) return;
        untracked(() => {
          this.initializeAndRefreshAccessWhenNeeded(event.journal);
        });
      },
      { allowSignalWrites: true },
    );

    // Area access and quest availability are both derived server-side from
    // character level, and the level-up event carries neither. Without this the
    // newly unlocked area stays locked until something else refetches.
    effect(
      () => {
        const envelope = this.events.eventEnvelope.CharacterLevelUpMsg();
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

    this.lastReconnectCount = this.events.reconnectCount();
    effect(
      () => {
        const reconnectCount = this.events.reconnectCount();
        if (reconnectCount === this.lastReconnectCount) return;
        this.lastReconnectCount = reconnectCount;
        // Events that landed while the socket was down are gone, so re-pull
        // rather than trusting the state we were holding.
        untracked(() => {
          if (!this._loaded()) return;
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
    this._journal.set(journal ?? { quests: [] });
    this._loaded.set(true);
    this._loading.set(false);
    this._error.set(null);
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
          this.initialize(journal);
          this.loadAreaAccess();
        },
        error: (error) =>
          this._error.set(error?.message ?? 'Failed to load quests'),
      });
  }

  refreshAfterOutboxProgress(delayMs = 750): void {
    this.loadJournalSilently();
    window.setTimeout(() => this.loadJournalSilently(), delayMs);
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
        next: (journal) => {
          this.initialize(journal);
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
        next: (journal) => {
          this.initialize(journal);
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
        next: (journal) => this.initialize(journal),
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
    this.api.getAreaAccess().subscribe({
      next: (access) => this._areaAccess.set(access),
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
    this._journal.set({ quests: [] });
    this._areaAccess.set([]);
    this._loaded.set(false);
    this._loading.set(false);
    this._error.set(null);
  }

  /** Re-pull journal and area access together, without touching loading/error UI. */
  private resyncSilently(): void {
    this.api.getJournal().subscribe({
      next: (journal) => {
        this.initialize(journal);
        this.loadAreaAccess();
      },
      error: () => undefined,
    });
  }

  private loadJournalSilently(): void {
    this.api.getJournal().subscribe({
      next: (journal) => this.initializeAndRefreshAccessWhenNeeded(journal),
      error: () => undefined,
    });
  }

  private initializeAndRefreshAccessWhenNeeded(journal: QuestJournal): void {
    const previousCompleted = this.completedQuestSignature(this._journal());
    const nextCompleted = this.completedQuestSignature(journal);
    this.initialize(journal);
    if (previousCompleted !== nextCompleted) this.loadAreaAccess();
  }

  private completedQuestSignature(journal: QuestJournal): string {
    return journal.quests
      .filter((quest) => quest.status === QuestStatus.Completed)
      .map((quest) => quest.questId)
      .sort()
      .join('|');
  }
}
