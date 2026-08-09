import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import { BaseItemComponent } from '../../../shared/components/base-item/base-item.component';
import {
  QuestObjectiveState,
  QuestRewardState,
  QuestState,
  QuestStatus,
} from '../../../shared/models/quest';

type QuestJournalTab =
  | QuestStatus.Active
  | QuestStatus.Available
  | QuestStatus.Completed;
type QuestSortMode = 'Order' | 'Progress';

@Component({
  selector: 'app-quest-journal-page',
  host: { class: 'block h-full min-h-0' },
  imports: [NgClass, NgFor, NgIf, BaseItemComponent],
  templateUrl: './quest-journal-page.component.html',
})
export class QuestJournalPageComponent implements OnInit {
  readonly tabs: QuestJournalTab[] = [
    QuestStatus.Active,
    QuestStatus.Available,
    QuestStatus.Completed,
  ];
  readonly activeTab = signal<QuestJournalTab>(QuestStatus.Active);
  readonly sortMode = signal<QuestSortMode>('Order');
  readonly selectedQuestId = signal<string | null>(null);
  readonly visibleQuests = computed(() => {
    let quests: QuestState[];
    switch (this.activeTab()) {
      case QuestStatus.Available:
        quests = this.questState.availableQuests();
        break;
      case QuestStatus.Completed:
        quests = this.questState.completedQuests();
        break;
      default:
        quests = this.questState.activeQuests();
        break;
    }

    return [...quests].sort((a, b) =>
      this.sortMode() === 'Progress'
        ? this.questProgress(b) - this.questProgress(a) ||
          a.sortOrder - b.sortOrder
        : a.sortOrder - b.sortOrder,
    );
  });
  readonly selectedQuest = computed(() => {
    const quests = this.visibleQuests();
    return (
      quests.find((quest) => quest.questId === this.selectedQuestId()) ??
      quests.find((quest) => quest.isPinned) ??
      quests[0] ??
      null
    );
  });
  readonly trackedQuestCount = computed(
    () => this.questState.activeQuests().length,
  );

  readonly QuestStatus = QuestStatus;

  constructor(
    readonly questState: QuestStateService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.questState.load();
  }

  setTab(tab: QuestJournalTab): void {
    this.activeTab.set(tab);
    this.selectedQuestId.set(null);
  }

  selectQuest(quest: QuestState): void {
    this.selectedQuestId.set(quest.questId);
  }

  isSelected(quest: QuestState): boolean {
    return this.selectedQuest()?.questId === quest.questId;
  }

  toggleSort(): void {
    this.sortMode.update((mode) => (mode === 'Order' ? 'Progress' : 'Order'));
  }

  tabCount(tab: QuestJournalTab): number {
    switch (tab) {
      case QuestStatus.Available:
        return this.questState.availableQuests().length;
      case QuestStatus.Completed:
        return this.questState.completedQuests().length;
      default:
        return this.questState.activeQuests().length;
    }
  }

  currentObjective(quest: QuestState): QuestObjectiveState | null {
    return quest.objectives.find((objective) => !objective.isCompleted) ?? null;
  }

  completedObjectiveCount(quest: QuestState): number {
    return quest.objectives.filter((objective) => objective.isCompleted).length;
  }

  objectiveProgress(objective: QuestObjectiveState): number {
    if (objective.isCompleted) return 100;
    if (objective.requiredAmount <= 0) return 0;

    return Math.min(
      100,
      Math.round((objective.currentAmount / objective.requiredAmount) * 100),
    );
  }

  questProgress(quest: QuestState): number {
    if (!quest.objectives.length) return 0;

    const progress = quest.objectives.reduce(
      (total, objective) => total + this.objectiveProgress(objective),
      0,
    );
    return Math.round(progress / quest.objectives.length);
  }

  rewardLabel(reward: QuestRewardState, includeQuantity = true): string {
    const name = (reward.itemBaseId ?? reward.key)
      .split(/[._-]/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' ');
    return includeQuantity ? `${reward.quantity} ${name}` : name;
  }

  acceptQuest(quest: QuestState): void {
    this.selectedQuestId.set(quest.questId);
    this.activeTab.set(QuestStatus.Active);
    this.questState.accept(quest.questId);
  }

  togglePinned(quest: QuestState): void {
    this.questState.pin(quest.isPinned ? null : quest.questId);
  }

  navigateToObjective(quest: QuestState): void {
    const route = this.currentObjective(quest)?.presentation.destinationRoute;
    if (route) void this.router.navigateByUrl(route);
  }
}
