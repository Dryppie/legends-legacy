import { NgIf } from '@angular/common';
import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { TutorialStateService } from '../../../core/services/api/tutorial/tutorial-state.service';
import { TutorialPresenterService } from '../../../core/services/api/tutorial/tutorial-presenter.service';
import {
  TUTORIAL_STEP_ABSORB_ESSENCE,
  TUTORIAL_STEP_CRAFT_EQUIPMENT,
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_ESSENCE,
  TUTORIAL_STEP_START_LUMO_RUINS,
} from '../../../shared/models/tutorial';

const FIRST_STEPS_STEP_ORDER = [
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_STEP_ABSORB_ESSENCE,
  TUTORIAL_STEP_EQUIP_ESSENCE,
  TUTORIAL_STEP_CRAFT_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TUTORIAL_STEP_START_LUMO_RUINS,
];

@Component({
  selector: 'app-tutorial-quest',
  imports: [NgIf],
  templateUrl: './tutorial-quest.component.html',
  styleUrl: './tutorial-quest.component.scss',
})
export class TutorialQuestComponent implements OnInit, OnDestroy {
  private readonly tutorialState = inject(TutorialStateService);
  private readonly presenter = inject(TutorialPresenterService);
  private welcomeTransitionTimer: ReturnType<typeof setTimeout> | null = null;
  private welcomeRevealTimer: ReturnType<typeof setTimeout> | null = null;

  @ViewChild('tutorialHeader')
  private tutorialHeader?: ElementRef<HTMLElement>;
  @ViewChild('welcomePanel')
  private welcomePanel?: ElementRef<HTMLElement>;

  confirmingSkip = false;

  readonly state = this.tutorialState.state;
  readonly visible = this.tutorialState.visible;
  readonly loading = this.tutorialState.loading;
  readonly error = this.tutorialState.error;
  readonly completion = this.tutorialState.completion;
  readonly completionTransitioning = this.tutorialState.completionTransitioning;
  readonly welcomeOpen = signal(false);
  readonly welcomeTransitioning = signal(false);
  readonly welcomeRevealing = signal(false);
  readonly welcomeAction = signal<'start' | 'skip' | null>(null);
  readonly totalSteps = computed(() => {
    const tutorial = this.state();
    return tutorial &&
      Number.isInteger(tutorial.totalSteps) &&
      tutorial.totalSteps > 0
      ? tutorial.totalSteps
      : FIRST_STEPS_STEP_ORDER.length;
  });
  readonly currentStepIndex = computed(() => {
    const tutorial = this.state();
    if (!tutorial) return 0;

    if (
      Number.isInteger(tutorial.currentStepIndex) &&
      tutorial.currentStepIndex > 0
    ) {
      return Math.min(tutorial.currentStepIndex, this.totalSteps());
    }

    const fallbackIndex = FIRST_STEPS_STEP_ORDER.indexOf(tutorial.currentStep);
    return fallbackIndex >= 0 ? fallbackIndex + 1 : 1;
  });
  readonly overallProgressPercent = computed(() => {
    if (this.completionTransitioning()) return 100;

    const totalSteps = this.totalSteps();
    if (totalSteps <= 0) return 0;

    const completedSteps = Math.max(this.currentStepIndex() - 1, 0);
    return Math.round((completedSteps / totalSteps) * 100);
  });
  readonly progressLabel = computed(() => {
    const tutorial = this.state();
    if (!tutorial) return 'First Steps tutorial progress';

    if (this.completionTransitioning()) {
      return `First Steps: all ${this.totalSteps()} steps complete, 100% complete`;
    }

    return `First Steps: step ${this.currentStepIndex()} of ${this.totalSteps()}, ${this.overallProgressPercent()}% complete`;
  });

  constructor() {
    effect(
      () => {
        const tutorial = this.state();
        if (tutorial?.requiresWelcome) {
          this.welcomeOpen.set(true);
          return;
        }

        if (!tutorial && !this.welcomeTransitioning()) {
          this.welcomeOpen.set(false);
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.presenter.initialize();
  }

  ngOnDestroy(): void {
    this.clearWelcomeTimers();
    this.tutorialState.completeWelcomeTransition();
  }

  requestSkip(): void {
    this.tutorialState.clearError();
    this.confirmingSkip = true;
  }

  cancelSkip(): void {
    this.confirmingSkip = false;
  }

  confirmSkip(): void {
    this.tutorialState.skip(() => {
      this.confirmingSkip = false;
    });
  }

  beginTutorial(): void {
    if (this.loading() || this.welcomeTransitioning()) return;

    this.welcomeAction.set('start');
    this.tutorialState.clearError();
    this.tutorialState.acknowledgeWelcome(() => {
      this.animateWelcomeIntoHeader();
    });
  }

  skipFromWelcome(): void {
    if (this.loading() || this.welcomeTransitioning()) return;

    this.welcomeAction.set('skip');
    this.tutorialState.clearError();
    this.tutorialState.skip(() => {
      this.welcomeOpen.set(false);
      this.welcomeAction.set(null);
    });
  }

  dismissCompletion(): void {
    this.tutorialState.dismissCompletion();
  }

  completeTutorialTransition(event: TransitionEvent): void {
    if (event.propertyName !== 'width') return;

    this.tutorialState.completeCompletionTransition();
  }

  private animateWelcomeIntoHeader(): void {
    const panel = this.welcomePanel?.nativeElement;
    const header = this.tutorialHeader?.nativeElement;
    if (!panel || !header || this.prefersReducedMotion()) {
      this.finishWelcomeTransition();
      return;
    }

    const panelRect = panel.getBoundingClientRect();
    const headerRect = header.getBoundingClientRect();
    const shiftX =
      headerRect.left +
      headerRect.width / 2 -
      (panelRect.left + panelRect.width / 2);
    const shiftY =
      headerRect.top +
      headerRect.height / 2 -
      (panelRect.top + panelRect.height / 2);

    panel.style.setProperty('--tutorial-welcome-shift-x', `${shiftX}px`);
    panel.style.setProperty('--tutorial-welcome-shift-y', `${shiftY}px`);
    this.welcomeTransitioning.set(true);

    this.welcomeTransitionTimer = setTimeout(
      () => this.finishWelcomeTransition(),
      680,
    );
  }

  private finishWelcomeTransition(): void {
    this.clearWelcomeTimers();
    this.welcomeOpen.set(false);
    this.welcomeTransitioning.set(false);
    this.welcomeAction.set(null);
    this.welcomeRevealing.set(true);
    this.tutorialState.completeWelcomeTransition();
    this.welcomeRevealTimer = setTimeout(
      () => this.welcomeRevealing.set(false),
      700,
    );
  }

  private clearWelcomeTimers(): void {
    if (this.welcomeTransitionTimer) {
      clearTimeout(this.welcomeTransitionTimer);
      this.welcomeTransitionTimer = null;
    }
    if (this.welcomeRevealTimer) {
      clearTimeout(this.welcomeRevealTimer);
      this.welcomeRevealTimer = null;
    }
  }

  private prefersReducedMotion(): boolean {
    return (
      typeof window !== 'undefined' &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
    );
  }
}
