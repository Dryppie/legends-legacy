import { Injectable, computed, effect, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TOUR_STATE_TUTORIAL_CRAFTING_READY,
  TOUR_STATE_TUTORIAL_EQUIPMENT_COMPLETE,
  TUTORIAL_STEP_CRAFT_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TutorialState,
} from '../../../../shared/models/tutorial';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { GameEventService } from '../../real-time/game-event.service';
import { TutorialService } from './tutorial.service';

interface TutorialLoadOptions {
  resumeCurrentStep?: boolean;
}

@Injectable({ providedIn: 'root' })
export class TutorialStateService {
  private readonly _state = signal<TutorialState | null>(null);
  private readonly _hasLoaded = signal(false);
  private readonly _isCompleted = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private hasResumedCurrentStep = false;

  readonly state = computed(() => this._state());
  readonly hasLoaded = computed(() => this._hasLoaded());
  readonly isCompleted = computed(() => this._isCompleted());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly visible = computed(() => {
    const state = this._state();
    return !!state && !state.isCompleted;
  });

  constructor(
    private readonly tutorialService: TutorialService,
    private readonly router: Router,
    private readonly firstPartyTour: FirstPartyTourService,
    private readonly eventService: GameEventService,
  ) {
    this.firstPartyTour.registerStatePredicate(
      TOUR_STATE_TUTORIAL_CRAFTING_READY,
      () => {
        const state = this._state();
        return (
          this._isCompleted() ||
          (!!state &&
            (state.currentStep === TUTORIAL_STEP_CRAFT_EQUIPMENT ||
              state.currentStep === TUTORIAL_STEP_EQUIP_EQUIPMENT))
        );
      },
    );

    this.firstPartyTour.registerStatePredicate(
      TOUR_STATE_TUTORIAL_EQUIPMENT_COMPLETE,
      () => {
        const state = this._state();
        return (
          this._isCompleted() ||
          (!!state && state.currentStep !== TUTORIAL_STEP_EQUIP_EQUIPMENT)
        );
      },
    );

    effect(
      () => {
        const event = this.eventService.event.TutorialProgressedMsg();
        if (!event?.tutorial) return;

        this.applyState(event.tutorial);
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const event = this.eventService.event.TutorialCompletedMsg();
        if (!event) return;

        const current = this._state();
        if (!current || current.tutorialId === event.tutorialId) {
          this._hasLoaded.set(true);
          this._isCompleted.set(true);
          this._state.set(null);
        }
      },
      { allowSignalWrites: true },
    );
  }

  load(options: TutorialLoadOptions = {}): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);

    this.tutorialService
      .getState()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (state) => {
          this.applyState(state);
          if (state && !state.isCompleted) {
            this.resumeCurrentStepIfRequested(state, options);
          }
        },
        error: (err) =>
          this._error.set(err?.message ?? 'Failed to load tutorial progress'),
      });
  }

  initialize(
    state: TutorialState | null,
    options: TutorialLoadOptions = {},
  ): void {
    this._loading.set(false);
    this._error.set(null);
    this.applyState(state);

    if (state && !state.isCompleted) {
      this.resumeCurrentStepIfRequested(state, options);
    }
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

  recordCraftingPageVisited(onComplete?: (state: TutorialState | null) => void): void {
    this.tutorialService.recordCraftingPageVisited(this.router.url).subscribe({
      next: (state) => {
        this.applyState(state);
        onComplete?.(state);
      },
      error: (err) =>
        this._error.set(err?.message ?? 'Failed to update tutorial progress'),
    });
  }

  navigateToCurrentStep(): void {
    const state = this._state();
    const route = state?.presentation?.destinationRoute ?? state?.destinationRoute;
    if (!route) return;

    this.router.navigateByUrl(route);
  }

  private resumeCurrentStepIfRequested(
    state: TutorialState,
    options: TutorialLoadOptions,
  ): void {
    const route = state.presentation?.destinationRoute ?? state.destinationRoute;

    if (
      !options.resumeCurrentStep ||
      this.hasResumedCurrentStep ||
      state.isCompleted ||
      !route ||
      this.isCurrentRoute(route)
    ) {
      return;
    }

    this.hasResumedCurrentStep = true;
    queueMicrotask(() => this.router.navigateByUrl(route));
  }

  private isCurrentRoute(route: string): boolean {
    const current = this.normalizeRoute(this.router.url);
    const expected = this.normalizeRoute(route);

    if (expected.includes('?')) {
      return current === expected;
    }

    return current === expected || current.startsWith(`${expected}?`);
  }

  private normalizeRoute(route: string): string {
    return route.startsWith('/') ? route : `/${route}`;
  }

  private applyState(state: TutorialState | null): void {
    this._hasLoaded.set(true);
    this._isCompleted.set(!state || state.isCompleted);
    this._state.set(state && !state.isCompleted ? state : null);
  }
}
