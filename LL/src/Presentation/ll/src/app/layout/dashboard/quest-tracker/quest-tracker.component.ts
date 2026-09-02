import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  OnDestroy,
  signal,
  ViewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import { DialogFocusDirective } from '../../../shared/directives/dialog-focus/dialog-focus.directive';
import {
  QuestState,
  TRAINING_DAY_QUEST_ID,
} from '../../../shared/models/quest';

@Component({
  selector: 'app-quest-tracker',
  imports: [NgFor, NgIf, DialogFocusDirective],
  templateUrl: './quest-tracker.component.html',
  styleUrl: './quest-tracker.component.scss',
})
export class QuestTrackerComponent implements OnDestroy {
  private readonly questState = inject(QuestStateService);
  private readonly router = inject(Router);
  private welcomeTransitionTimer: ReturnType<typeof setTimeout> | null = null;
  private welcomeRevealTimer: ReturnType<typeof setTimeout> | null = null;

  @ViewChild('questHeader')
  private questHeader?: ElementRef<HTMLElement>;
  @ViewChild('welcomePanel')
  private welcomePanel?: ElementRef<HTMLElement>;

  readonly quest = this.questState.pinnedQuest;
  readonly objective = this.questState.pinnedObjective;
  readonly requiresChoice = computed(() => {
    const choice = this.quest()?.choice;
    return !!choice && !choice.selectedOptionKey;
  });
  readonly error = this.questState.error;
  readonly loading = this.questState.loading;
  readonly welcomeOpen = signal(false);
  readonly welcomeStarting = signal(false);
  readonly welcomeTransitioning = signal(false);
  readonly welcomeRevealing = signal(false);
  readonly completedObjectiveCount = computed(
    () =>
      this.quest()?.objectives.filter((objective) => objective.isCompleted)
        .length ?? 0,
  );
  readonly progressPercent = computed(() => {
    const objective = this.objective();
    if (!objective || objective.requiredAmount <= 0) return 0;
    return Math.min(
      100,
      Math.round((objective.currentAmount / objective.requiredAmount) * 100),
    );
  });

  constructor() {
    effect(
      () => {
        const quest = this.quest();
        if (quest?.requiresWelcome) {
          this.welcomeOpen.set(true);
          return;
        }

        if (!quest && !this.welcomeTransitioning()) {
          this.welcomeOpen.set(false);
        }
      },
    );
  }

  ngOnDestroy(): void {
    this.clearWelcomeTimers();
  }

  navigate(): void {
    this.questState.navigateToPinnedObjective();
  }

  objectiveProgress(currentAmount: number, requiredAmount: number): number {
    if (requiredAmount <= 0) return 0;
    return Math.min(100, Math.round((currentAmount / requiredAmount) * 100));
  }

  choiceActionLabel(quest: QuestState): string {
    return quest.questId === TRAINING_DAY_QUEST_ID
      ? 'Choose Hunt'
      : 'Choose Reward';
  }

  beginTutorial(): void {
    if (this.loading() || this.welcomeStarting() || this.welcomeTransitioning())
      return;

    this.welcomeStarting.set(true);
    this.questState.clearError();
    this.questState.acknowledgeWelcome(
      () => {
        void this.router.navigateByUrl('/game/quests').then(() => {
          requestAnimationFrame(() => this.animateWelcomeIntoHeader());
        });
      },
      () => this.welcomeStarting.set(false),
    );
  }

  private animateWelcomeIntoHeader(): void {
    const panel = this.welcomePanel?.nativeElement;
    const header = this.questHeader?.nativeElement;
    if (!panel || !header || this.prefersReducedMotion()) {
      this.finishWelcomeTransition();
      return;
    }

    const panelRect = panel.getBoundingClientRect();
    const headerRect = header.getBoundingClientRect();
    if (headerRect.width <= 0 || headerRect.height <= 0) {
      this.finishWelcomeTransition();
      return;
    }

    const shiftX =
      headerRect.left +
      headerRect.width / 2 -
      (panelRect.left + panelRect.width / 2);
    const shiftY =
      headerRect.top +
      headerRect.height / 2 -
      (panelRect.top + panelRect.height / 2);

    panel.style.setProperty('--quest-welcome-shift-x', `${shiftX}px`);
    panel.style.setProperty('--quest-welcome-shift-y', `${shiftY}px`);
    this.welcomeTransitioning.set(true);
    this.welcomeTransitionTimer = setTimeout(
      () => this.finishWelcomeTransition(),
      680,
    );
  }

  private finishWelcomeTransition(): void {
    this.clearWelcomeTimers();
    this.welcomeOpen.set(false);
    this.welcomeStarting.set(false);
    this.welcomeTransitioning.set(false);
    this.welcomeRevealing.set(true);
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
