import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TUTORIAL_STEP_START_LUMO_RUINS,
  TutorialCompletion,
  TutorialState,
} from '../../../../shared/models/tutorial';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameEventService } from '../../real-time/game-event.service';
import { TutorialService } from './tutorial.service';

@Injectable({ providedIn: 'root' })
export class TutorialStateService {
  private static readonly completionRevealDelayMs = 1100;

  private readonly _state = signal<TutorialState | null>(null);
  private readonly _hasLoaded = signal(false);
  private readonly _isCompleted = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _completion = signal<TutorialCompletion | null>(null);
  private readonly _completionTransitioning = signal(false);
  private readonly _welcomeTransitionPending = signal(false);
  private lastActiveState: TutorialState | null = null;
  private pendingCompletion: TutorialCompletion | null = null;
  private completionRevealTimer: ReturnType<typeof setTimeout> | null = null;
  private lastLogoutCount = 0;

  readonly state = computed(() => this._state());
  readonly hasLoaded = computed(() => this._hasLoaded());
  readonly isCompleted = computed(() => this._isCompleted());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly completion = computed(() => this._completion());
  readonly completionTransitioning = computed(() =>
    this._completionTransitioning(),
  );
  readonly requiresWelcome = computed(
    () => this._state()?.requiresWelcome === true,
  );
  readonly presentationReady = computed(
    () =>
      !!this._state() &&
      !this._state()!.requiresWelcome &&
      !this._welcomeTransitionPending(),
  );
  readonly visible = computed(() => {
    const state = this._state();
    return !!state && !state.isCompleted;
  });

  constructor(
    private readonly tutorialService: TutorialService,
    private readonly router: Router,
    private readonly firstPartyTour: FirstPartyTourService,
    private readonly eventService: GameEventService,
    private readonly eventBus: EventBusService,
  ) {
    effect(
      () => {
        const event = this.eventService.event.TutorialProgressedMsg();
        if (!event?.tutorial) return;

        untracked(() => this.applyState(event.tutorial));
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const event = this.eventService.event.TutorialCompletedMsg();
        if (!event) return;

        untracked(() => {
          const current = this._state();
          if (!current || current.tutorialId === event.tutorialId) {
            this.applyCompletion({
              tutorialId: event.tutorialId,
              rewardCinders: event.rewardCinders ?? 0,
              nextRoute: event.nextRoute ?? '/game/combat',
              wasSkipped: event.wasSkipped ?? false,
            });
          }
        });
      },
      { allowSignalWrites: true },
    );

    this.lastLogoutCount = this.eventBus.logout();

    effect(
      () => {
        const logoutCount = this.eventBus.logout();
        if (logoutCount === this.lastLogoutCount) {
          return;
        }

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

    this.tutorialService
      .getState()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (state) => {
          this.applyState(state);
        },
        error: (err) =>
          this._error.set(err?.message ?? 'Failed to load tutorial progress'),
      });
  }

  initialize(state: TutorialState | null): void {
    this._loading.set(false);
    this._error.set(null);
    this.applyState(state);
  }

  reset(): void {
    this.clearCompletionRevealTimer();
    this._state.set(null);
    this._hasLoaded.set(false);
    this._isCompleted.set(false);
    this._loading.set(false);
    this._error.set(null);
    this._completion.set(null);
    this._completionTransitioning.set(false);
    this._welcomeTransitionPending.set(false);
    this.lastActiveState = null;
    this.pendingCompletion = null;
  }

  refresh(): void {
    this.tutorialService.getState().subscribe({
      next: (state) => this.applyState(state),
      error: () => undefined,
    });
  }

  refreshAfterOutboxProgress(delayMs = 750): void {
    this.refresh();
    window.setTimeout(() => this.refresh(), delayMs);
  }

  acknowledgeWelcome(onComplete?: () => void): void {
    if (this._loading() || !this.requiresWelcome()) return;

    this._loading.set(true);
    this._error.set(null);
    this._welcomeTransitionPending.set(true);
    this.tutorialService
      .acknowledgeWelcome()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (state) => {
          this.applyState(state);
          onComplete?.();
        },
        error: (err) => {
          this._welcomeTransitionPending.set(false);
          this._error.set(
            err?.message ?? 'Failed to start the tutorial. Please try again.',
          );
        },
      });
  }

  completeWelcomeTransition(): void {
    this._welcomeTransitionPending.set(false);
  }

  skip(onComplete?: () => void): void {
    if (this._loading()) return;

    this._loading.set(true);
    this._error.set(null);
    this.tutorialService
      .skip()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (completion) => {
          this.applyCompletion(completion);
          onComplete?.();
        },
        error: (err) =>
          this._error.set(err?.message ?? 'Failed to skip the tutorial'),
      });
  }

  dismissCompletion(): void {
    this._completion.set(null);
  }

  completeCompletionTransition(): void {
    const completion = this.pendingCompletion;
    if (!this._completionTransitioning() || !completion) return;

    this.finishCompletion(completion);
  }

  reportError(message: string): void {
    this._error.set(message);
  }

  clearError(): void {
    this._error.set(null);
  }

  navigateToCurrentStep(): void {
    const state = this._state();
    const route =
      state?.presentation?.destinationRoute ?? state?.destinationRoute;
    if (!route) return;

    this.router.navigateByUrl(route);
  }

  private applyState(state: TutorialState | null): void {
    this._hasLoaded.set(true);

    if (this._completionTransitioning()) {
      return;
    }

    const current = this._state();
    if (
      state &&
      current &&
      state.tutorialId === current.tutorialId &&
      state.version === current.version &&
      state.currentStepIndex < current.currentStepIndex
    ) {
      // A refresh started before a realtime progression event can finish later
      // with an older step. Tutorial progress is monotonic within a definition
      // version, so accepting that response would replay prior-step UI effects.
      return;
    }

    this._isCompleted.set(!state || state.isCompleted);
    const activeState = state && !state.isCompleted ? state : null;
    this._state.set(activeState);
    if (activeState) {
      this.lastActiveState = activeState;
    }
  }

  private applyCompletion(completion: TutorialCompletion): void {
    const transitionState = this._state() ?? this.lastActiveState;
    const shouldAnimate =
      !completion.wasSkipped &&
      transitionState?.currentStep === TUTORIAL_STEP_START_LUMO_RUINS &&
      !this.prefersReducedMotion();

    if (!shouldAnimate) {
      this.finishCompletion(completion);
      return;
    }

    if (this._completionTransitioning()) {
      this.pendingCompletion = completion;
      this.ensureCompletionRevealTimer(completion);
      return;
    }

    this._hasLoaded.set(true);
    this._isCompleted.set(true);
    this._state.set(transitionState);
    this._completion.set(null);
    this._completionTransitioning.set(true);
    this._welcomeTransitionPending.set(false);
    this.pendingCompletion = completion;
    this.firstPartyTour.stop(true);
    this.ensureCompletionRevealTimer(completion);
  }

  private ensureCompletionRevealTimer(
    fallbackCompletion: TutorialCompletion,
  ): void {
    if (this.completionRevealTimer !== null) return;

    this.completionRevealTimer = window.setTimeout(
      () => this.finishCompletion(this.pendingCompletion ?? fallbackCompletion),
      TutorialStateService.completionRevealDelayMs,
    );
  }

  private finishCompletion(completion: TutorialCompletion): void {
    this.clearCompletionRevealTimer();
    this._hasLoaded.set(true);
    this._isCompleted.set(true);
    this._state.set(null);
    this._completion.set(completion);
    this._completionTransitioning.set(false);
    this._welcomeTransitionPending.set(false);
    this.lastActiveState = null;
    this.pendingCompletion = null;
    this.firstPartyTour.stop(true);
  }

  private clearCompletionRevealTimer(): void {
    if (this.completionRevealTimer === null) return;

    clearTimeout(this.completionRevealTimer);
    this.completionRevealTimer = null;
  }

  private prefersReducedMotion(): boolean {
    return (
      typeof window !== 'undefined' &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
    );
  }
}
