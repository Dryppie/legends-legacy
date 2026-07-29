import { NgIf } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TutorialStateService } from '../../../core/services/api/tutorial/tutorial-state.service';
import { TutorialPresenterService } from '../../../core/services/api/tutorial/tutorial-presenter.service';
import {
  TUTORIAL_STEP_ABSORB_ESSENCE,
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_ESSENCE,
  TUTORIAL_STEP_START_LUMO_RUINS,
} from '../../../shared/models/tutorial';

const FIRST_STEPS_STEP_ORDER = [
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_STEP_ABSORB_ESSENCE,
  TUTORIAL_STEP_EQUIP_ESSENCE,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TUTORIAL_STEP_START_LUMO_RUINS,
];

@Component({
  selector: 'app-tutorial-quest',
  imports: [NgIf],
  templateUrl: './tutorial-quest.component.html',
  styleUrl: './tutorial-quest.component.scss',
})
export class TutorialQuestComponent implements OnInit {
  private readonly tutorialState = inject(TutorialStateService);
  private readonly presenter = inject(TutorialPresenterService);
  private readonly router = inject(Router);
  confirmingSkip = false;

  readonly state = this.tutorialState.state;
  readonly visible = this.tutorialState.visible;
  readonly loading = this.tutorialState.loading;
  readonly error = this.tutorialState.error;
  readonly completion = this.tutorialState.completion;
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
    const totalSteps = this.totalSteps();
    if (totalSteps <= 0) return 0;

    const completedSteps = Math.max(this.currentStepIndex() - 1, 0);
    return Math.round((completedSteps / totalSteps) * 100);
  });
  readonly progressLabel = computed(() => {
    const tutorial = this.state();
    if (!tutorial) return 'First Steps tutorial progress';

    return `First Steps: step ${this.currentStepIndex()} of ${this.totalSteps()}, ${this.overallProgressPercent()}% complete`;
  });

  ngOnInit(): void {
    this.presenter.initialize();
  }

  go(): void {
    this.tutorialState.navigateToCurrentStep();
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

  continueAfterCompletion(): void {
    const completion = this.completion();
    if (!completion?.nextRoute) return;

    this.tutorialState.dismissCompletion();
    void this.router.navigateByUrl(completion.nextRoute);
  }

  dismissCompletion(): void {
    this.tutorialState.dismissCompletion();
  }
}
