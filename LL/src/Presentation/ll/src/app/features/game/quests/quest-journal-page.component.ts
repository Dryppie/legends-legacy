import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { BaseItemComponent } from '../../../shared/components/base-item/base-item.component';
import { Essence } from '../../../shared/models/essence';
import { EssenceItem } from '../../../shared/models/item';
import {
  QuestChoiceOption,
  QuestObjectiveState,
  QuestRewardState,
  QuestState,
  QuestStatus,
} from '../../../shared/models/quest';

type QuestJournalTab = QuestStatus.Active | QuestStatus.Completed;
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
    QuestStatus.Completed,
  ];
  readonly activeTab = signal<QuestJournalTab>(QuestStatus.Active);
  readonly sortMode = signal<QuestSortMode>('Order');
  readonly selectedQuestId = signal<string | null>(null);
  readonly pendingChoiceKey = signal<string | null>(null);
  readonly visibleQuests = computed(() => {
    let quests: QuestState[];
    switch (this.activeTab()) {
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
    private readonly essenceItemView: EssenceItemViewService,
  ) {}

  ngOnInit(): void {
    this.questState.load();
  }

  setTab(tab: QuestJournalTab): void {
    this.activeTab.set(tab);
    this.selectedQuestId.set(null);
    this.pendingChoiceKey.set(null);
  }

  selectQuest(quest: QuestState): void {
    this.selectedQuestId.set(quest.questId);
    this.pendingChoiceKey.set(null);
  }

  isSelected(quest: QuestState): boolean {
    return this.selectedQuest()?.questId === quest.questId;
  }

  toggleSort(): void {
    this.sortMode.update((mode) => (mode === 'Order' ? 'Progress' : 'Order'));
  }

  tabCount(tab: QuestJournalTab): number {
    switch (tab) {
      case QuestStatus.Completed:
        return this.questState.completedQuests().length;
      default:
        return this.questState.activeQuests().length;
    }
  }

  currentObjective(quest: QuestState): QuestObjectiveState | null {
    return quest.objectives.find((objective) => !objective.isCompleted) ?? null;
  }

  isObjectiveAvailable(
    quest: QuestState,
    objective: QuestObjectiveState,
  ): boolean {
    return (
      !objective.isCompleted &&
      (quest.objectiveMode === 'All' ||
        this.currentObjective(quest) === objective)
    );
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

  chainSteps(totalSteps: number): number[] {
    return Array.from({ length: totalSteps }, (_, index) => index + 1);
  }

  rewardLabel(reward: QuestRewardState, includeQuantity = true): string {
    const name = (reward.itemBaseId ?? reward.key)
      .split(/[._-]/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' ');
    return includeQuantity ? `${reward.quantity} ${name}` : name;
  }

  requiresChoice(quest: QuestState): boolean {
    return !!quest.choice && !quest.choice.selectedOptionKey;
  }

  chooseOption(option: QuestChoiceOption): void {
    this.pendingChoiceKey.set(option.key);
  }

  confirmChoice(quest: QuestState): void {
    const optionKey = this.pendingChoiceKey();
    if (!optionKey || this.questState.loading()) return;
    this.questState.selectChoice(quest.questId, optionKey, () =>
      this.pendingChoiceKey.set(null),
    );
  }

  choiceEssence(option: QuestChoiceOption): Essence | null {
    if (!option.rewardItemBase) return null;
    return this.essenceItemView.asEssence(option.rewardItemBase as EssenceItem);
  }

  togglePinned(quest: QuestState): void {
    this.questState.pin(quest.isPinned ? null : quest.questId);
  }

  navigateToObjective(quest: QuestState): void {
    const route = this.currentObjective(quest)?.presentation.destinationRoute;
    if (route) void this.router.navigateByUrl(route);
  }
}
