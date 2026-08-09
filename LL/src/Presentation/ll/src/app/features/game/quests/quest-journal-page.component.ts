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
import {
  buildQuestJournalEntries,
  preferredQuestForEntry,
  QuestJournalEntry,
} from './quest-journal-entry';

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
  readonly selectedEntryKey = signal<string | null>(null);
  readonly selectedPartQuestId = signal<string | null>(null);
  readonly pendingChoiceKey = signal<string | null>(null);
  readonly journalEntries = computed(() =>
    buildQuestJournalEntries(this.questState.journal().quests),
  );
  readonly visibleEntries = computed(() =>
    this.journalEntries()
      .filter((entry) => entry.status === this.activeTab())
      .sort((a, b) =>
        this.sortMode() === 'Progress'
          ? this.entryProgress(b) - this.entryProgress(a) ||
            a.sortOrder - b.sortOrder
          : a.sortOrder - b.sortOrder,
      ),
  );
  readonly selectedEntry = computed(() => {
    const entries = this.visibleEntries();
    return (
      entries.find((entry) => entry.key === this.selectedEntryKey()) ??
      entries.find((entry) => entry.quests.some((quest) => quest.isPinned)) ??
      entries[0] ??
      null
    );
  });
  readonly selectedQuest = computed(() => {
    const entry = this.selectedEntry();
    if (!entry) return null;

    return (
      entry.quests.find(
        (quest) => quest.questId === this.selectedPartQuestId(),
      ) ?? preferredQuestForEntry(entry)
    );
  });
  readonly trackedQuestCount = computed(
    () =>
      this.journalEntries().filter(
        (entry) => entry.status === QuestStatus.Active,
      ).length,
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
    this.selectedEntryKey.set(null);
    this.selectedPartQuestId.set(null);
    this.pendingChoiceKey.set(null);
  }

  selectEntry(entry: QuestJournalEntry): void {
    this.selectedEntryKey.set(entry.key);
    this.selectedPartQuestId.set(preferredQuestForEntry(entry).questId);
    this.pendingChoiceKey.set(null);
  }

  isSelected(entry: QuestJournalEntry): boolean {
    return this.selectedEntry()?.key === entry.key;
  }

  toggleSort(): void {
    this.sortMode.update((mode) => (mode === 'Order' ? 'Progress' : 'Order'));
  }

  tabCount(tab: QuestJournalTab): number {
    return this.journalEntries().filter((entry) => entry.status === tab).length;
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

  entryProgress(entry: QuestJournalEntry): number {
    if (!entry.isChain) {
      return this.questProgress(entry.quests[0]);
    }

    const total = entry.quests.reduce(
      (progress, quest) => progress + this.questProgress(quest),
      0,
    );
    return Math.round(total / entry.totalParts);
  }

  completedPartCount(entry: QuestJournalEntry): number {
    return entry.quests.filter(
      (quest) => quest.status === QuestStatus.Completed,
    ).length;
  }

  entryCurrentPart(entry: QuestJournalEntry): QuestState {
    return preferredQuestForEntry(entry);
  }

  entrySummary(entry: QuestJournalEntry): string {
    const current = this.entryCurrentPart(entry);
    if (!entry.isChain) {
      return (
        (this.requiresChoice(current)
          ? current.choice?.selectionSummary
          : this.currentObjective(current)?.description) ??
        (current.status === QuestStatus.Completed
          ? 'Quest completed'
          : current.summary)
      );
    }

    if (current.status === QuestStatus.Active) {
      return 'Current: ' + current.title;
    }

    if (entry.status === QuestStatus.Completed) {
      const completedParts = this.completedPartCount(entry);
      return completedParts === entry.totalParts
        ? 'Chain completed'
        : completedParts + ' parts completed';
    }

    return 'Next part unlocks as you progress';
  }

  chainSteps(totalSteps: number): number[] {
    return Array.from({ length: totalSteps }, (_, index) => index + 1);
  }

  chainNavigationSteps(entry: QuestJournalEntry): number[] {
    if (entry.status === QuestStatus.Completed) {
      return entry.quests
        .map((quest) => quest.chain?.step)
        .filter((step): step is number => step !== undefined);
    }

    return this.chainSteps(entry.totalParts);
  }

  chainQuestForStep(entry: QuestJournalEntry, step: number): QuestState | null {
    return entry.quests.find((quest) => quest.chain?.step === step) ?? null;
  }

  selectChainStep(entry: QuestJournalEntry, step: number): void {
    const quest = this.chainQuestForStep(entry, step);
    if (!quest) return;

    this.selectedPartQuestId.set(quest.questId);
    this.pendingChoiceKey.set(null);
  }

  isSelectedChainStep(step: number): boolean {
    return this.selectedQuest()?.chain?.step === step;
  }

  isCompletedChainStep(entry: QuestJournalEntry, step: number): boolean {
    return (
      this.chainQuestForStep(entry, step)?.status === QuestStatus.Completed
    );
  }

  chainStepTitle(entry: QuestJournalEntry, step: number): string {
    return this.chainQuestForStep(entry, step)?.title ?? 'Locked';
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
