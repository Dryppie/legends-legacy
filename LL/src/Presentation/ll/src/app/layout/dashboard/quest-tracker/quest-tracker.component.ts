import { NgIf } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';

@Component({
  selector: 'app-quest-tracker',
  imports: [NgIf],
  templateUrl: './quest-tracker.component.html',
})
export class QuestTrackerComponent {
  private readonly questState = inject(QuestStateService);

  readonly quest = this.questState.pinnedQuest;
  readonly objective = this.questState.pinnedObjective;
  readonly error = this.questState.error;
  readonly progressPercent = computed(() => {
    const objective = this.objective();
    if (!objective || objective.requiredAmount <= 0) return 0;
    return Math.min(
      100,
      Math.round((objective.currentAmount / objective.requiredAmount) * 100),
    );
  });

  navigate(): void {
    this.questState.navigateToPinnedObjective();
  }
}
