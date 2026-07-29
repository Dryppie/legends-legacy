import { Injectable, computed, effect, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TutorialCompletion,
  TutorialState,
} from '../../../../shared/models/tutorial';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameEventService } from '../../real-time/game-event.service';
import { TutorialService } from './tutorial.service';

@Injectable({ providedIn: 'root' })
export class TutorialStateService {
  private readonly _state = signal<TutorialState | null>(null);
  private readonly _hasLoaded = signal(false);
  private readonly _isCompleted = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _completion = signal<TutorialCompletion | null>(null);
  private lastLogoutCount = 0;

  readonly state = computed(() => this._state());
  readonly hasLoaded = computed(() => this._hasLoaded());
  readonly isCompleted = computed(() => this._isCompleted());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly completion = computed(() => this._completion());
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
          this.applyCompletion({
            tutorialId: event.tutorialId,
            rewardCinders: event.rewardCinders ?? 0,
            nextRoute: event.nextRoute ?? '/game/combat',
            wasSkipped: event.wasSkipped ?? false,
          });
        }
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
    this._state.set(null);
    this._hasLoaded.set(false);
    this._isCompleted.set(false);
    this._loading.set(false);
    this._error.set(null);
    this._completion.set(null);
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
    this._isCompleted.set(!state || state.isCompleted);
    this._state.set(state && !state.isCompleted ? state : null);
  }

  private applyCompletion(completion: TutorialCompletion): void {
    this._hasLoaded.set(true);
    this._isCompleted.set(true);
    this._state.set(null);
    this._completion.set(completion);
    this.firstPartyTour.stop(true);
  }
}
