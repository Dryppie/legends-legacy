import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  computed,
  ElementRef,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import { EventQuestStateService } from '../../../core/services/api/quest/event-quest-state.service';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { BaseItemComponent } from '../../../shared/components/base-item/base-item.component';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';
import { DefaultHeaderComponent } from '../../../shared/components/default-header/default-header.component';
import { AbilityTagsComponent } from '../../../shared/components/essences/ability-tags/ability-tags.component';
import { EssenceDescriptionComponent } from '../../../shared/components/essences/essence-description/essence-description.component';
import { Essence } from '../../../shared/models/essence';
import { EssenceItem } from '../../../shared/models/item';
import {
  EventQuestObjectiveState,
  EventQuestPersonalMilestoneState,
  EventQuestState,
  EventQuestStatus,
} from '../../../shared/models/event-quest';
import {
  QuestChoiceOption,
  QuestObjectiveState,
  QuestRewardState,
  QuestState,
  QuestStatus,
  TRAINING_DAY_QUEST_ID,
} from '../../../shared/models/quest';
import {
  buildQuestJournalEntries,
  groupQuestJournalEntries,
  preferredQuestForEntry,
  QuestJournalEntry,
  QuestJournalEntryGroup,
  QuestJournalGroupKey,
} from './quest-journal-entry';

type QuestJournalTab = QuestStatus.Active | QuestStatus.Completed;
type QuestSortMode = 'Order' | 'Progress';

@Component({
  selector: 'app-quest-journal-page',
  host: { class: 'block h-full min-h-0' },
  imports: [
    NgClass,
    NgFor,
    NgIf,
    RouterLink,
    BaseItemComponent,
    CharacterTagComponent,
    DefaultHeaderComponent,
    AbilityTagsComponent,
    EssenceDescriptionComponent,
  ],
  templateUrl: './quest-journal-page.component.html',
})
export class QuestJournalPageComponent
  implements OnInit, OnDestroy, AfterViewChecked
{
  @ViewChild('questDetailScroller')
  private questDetailScroller?: ElementRef<HTMLElement>;
  @ViewChild('firstHuntConfirmation')
  private firstHuntConfirmation?: ElementRef<HTMLElement>;

  readonly tabs: QuestJournalTab[] = [
    QuestStatus.Active,
    QuestStatus.Completed,
  ];
  readonly activeTab = signal<QuestJournalTab>(QuestStatus.Active);
  readonly sortMode = signal<QuestSortMode>('Order');
  readonly expandedGroups = signal<
    Partial<Record<QuestJournalGroupKey, boolean>>
  >({});
  readonly selectedEntryKey = signal<string | null>(null);
  readonly selectedPartQuestId = signal<string | null>(null);
  readonly pendingChoiceKey = signal<string | null>(null);
  readonly selectedEventQuestId = signal<string | null>(null);
  readonly clock = signal(Date.now());
  readonly realmProgressMarkers = [25, 50, 75, 100];
  private countdownTimer: number | null = null;
  private choiceScrollPending = false;
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
  readonly visibleEntryGroups = computed(() =>
    groupQuestJournalEntries(this.visibleEntries()),
  );
  readonly visibleEvents = computed(() =>
    this.eventQuestState
      .journal()
      .events.filter((event) => event.status !== EventQuestStatus.Expired)
      .sort((a, b) => a.sortOrder - b.sortOrder),
  );
  readonly selectedEvent = computed(() => {
    const events = this.visibleEvents();
    return (
      events.find(
        (event) => event.eventQuestId === this.selectedEventQuestId(),
      ) ??
      events[0] ??
      null
    );
  });
  readonly selectedEntry = computed(() => {
    const entries = this.visibleEntries();
    return (
      entries.find((entry) => entry.key === this.selectedEntryKey()) ??
      entries.find((entry) => entry.quests.some((quest) => quest.isPinned)) ??
      entries[0] ??
      null
    );
  });
  readonly showingEventDetail = computed(
    () =>
      this.activeTab() === QuestStatus.Active &&
      this.visibleEvents().length > 0 &&
      this.selectedEntryKey() === null,
  );
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
  readonly EventQuestStatus = EventQuestStatus;

  constructor(
    readonly questState: QuestStateService,
    readonly eventQuestState: EventQuestStateService,
    private readonly router: Router,
    private readonly essenceItemView: EssenceItemViewService,
  ) {}

  ngOnInit(): void {
    this.eventQuestState.activateView();
    this.questState.load();
    this.eventQuestState.load();
    this.countdownTimer = window.setInterval(
      () => this.clock.set(Date.now()),
      60_000,
    );
  }

  ngOnDestroy(): void {
    this.eventQuestState.deactivateView();
    if (this.countdownTimer !== null) {
      window.clearInterval(this.countdownTimer);
    }
  }

  ngAfterViewChecked(): void {
    if (!this.choiceScrollPending) return;

    this.choiceScrollPending = false;
    this.scrollChoiceConfirmationIntoView();
  }

  setTab(tab: QuestJournalTab): void {
    this.activeTab.set(tab);
    this.expandedGroups.set({});
    const firstEntry =
      tab === QuestStatus.Completed
        ? (this.visibleEntryGroups()[0]?.entries[0] ?? null)
        : null;
    this.selectedEntryKey.set(firstEntry?.key ?? null);
    this.selectedPartQuestId.set(
      firstEntry ? preferredQuestForEntry(firstEntry).questId : null,
    );
    this.pendingChoiceKey.set(null);
    this.selectedEventQuestId.set(null);
  }

  selectEntry(entry: QuestJournalEntry): void {
    this.selectedEventQuestId.set(null);
    this.selectedEntryKey.set(entry.key);
    this.selectedPartQuestId.set(preferredQuestForEntry(entry).questId);
    this.pendingChoiceKey.set(null);
  }

  isSelected(entry: QuestJournalEntry): boolean {
    return (
      !this.showingEventDetail() && this.selectedEntry()?.key === entry.key
    );
  }

  isGroupExpanded(group: QuestJournalEntryGroup): boolean {
    const preference = this.expandedGroups()[group.key];
    return preference ?? true;
  }

  toggleGroup(group: QuestJournalEntryGroup): void {
    const isExpanded = this.isGroupExpanded(group);
    this.expandedGroups.update((groups) => ({
      ...groups,
      [group.key]: !isExpanded,
    }));
  }

  toggleSort(): void {
    this.sortMode.update((mode) => (mode === 'Order' ? 'Progress' : 'Order'));
  }

  tabCount(tab: QuestJournalTab): number {
    return this.journalEntries().filter((entry) => entry.status === tab).length;
  }

  selectEvent(event: EventQuestState): void {
    this.selectedEntryKey.set(null);
    this.selectedPartQuestId.set(null);
    this.pendingChoiceKey.set(null);
    this.selectedEventQuestId.set(event.eventQuestId);
  }

  isSelectedEvent(event: EventQuestState): boolean {
    return (
      this.showingEventDetail() &&
      this.selectedEvent()?.eventQuestId === event.eventQuestId
    );
  }

  eventObjectiveProgress(objective: EventQuestObjectiveState): number {
    if (objective.requiredAmount <= 0) return 0;
    return Math.min(
      100,
      Math.round((objective.currentAmount / objective.requiredAmount) * 100),
    );
  }

  eventProgress(event: EventQuestState): number {
    const required = this.eventRequiredAmount(event);
    if (required <= 0) return 0;
    return Math.min(
      100,
      Math.round((this.eventCurrentAmount(event) / required) * 100),
    );
  }

  eventCurrentAmount(event: EventQuestState): number {
    return event.objectives.reduce(
      (total, objective) => total + objective.currentAmount,
      0,
    );
  }

  eventRequiredAmount(event: EventQuestState): number {
    return event.objectives.reduce(
      (total, objective) => total + objective.requiredAmount,
      0,
    );
  }

  formatAmount(amount: number): string {
    return new Intl.NumberFormat().format(amount);
  }

  eventTimeLabel(event: EventQuestState): string {
    const now = this.clock();
    if (now < new Date(event.startsAtUtc).getTime()) return 'Starts in';
    if (now <= new Date(event.endsAtUtc).getTime()) return 'Ends in';
    return 'Claims close in';
  }

  eventTimeRemaining(event: EventQuestState): string {
    const now = this.clock();
    const start = new Date(event.startsAtUtc).getTime();
    const end = new Date(event.endsAtUtc).getTime();
    const claimEnd = new Date(event.claimEndsAtUtc).getTime();
    const target = now < start ? start : now <= end ? end : claimEnd;
    const remainingMinutes = Math.max(0, Math.ceil((target - now) / 60_000));
    const days = Math.floor(remainingMinutes / 1_440);
    const hours = Math.floor((remainingMinutes % 1_440) / 60);
    const minutes = remainingMinutes % 60;
    return `${days}d ${hours.toString().padStart(2, '0')}h ${minutes
      .toString()
      .padStart(2, '0')}m`;
  }

  claimedMilestoneCount(event: EventQuestState): number {
    return event.personalMilestones.filter((milestone) => milestone.isClaimed)
      .length;
  }

  isHighlightedMilestone(
    event: EventQuestState,
    milestone: EventQuestPersonalMilestoneState,
  ): boolean {
    return (
      event.personalMilestones.find((candidate) => !candidate.isClaimed)
        ?.key === milestone.key
    );
  }

  milestoneTitle(milestone: EventQuestPersonalMilestoneState): string {
    return milestone.key
      .split(/[._-]/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' ');
  }

  milestoneIndex(index: number): string {
    return ['I', 'II', 'III', 'IV', 'V', 'VI'][index] ?? `${index + 1}`;
  }

  milestoneRemaining(
    event: EventQuestState,
    milestone: EventQuestPersonalMilestoneState,
  ): number {
    return Math.max(0, milestone.requiredContribution - event.myContribution);
  }

  canClaimEvent(event: EventQuestState): boolean {
    return (
      event.status === EventQuestStatus.Completed &&
      event.isEligible &&
      !event.hasClaimed &&
      new Date(event.claimEndsAtUtc).getTime() >= Date.now()
    );
  }

  claimableMilestones(
    event: EventQuestState,
  ): EventQuestPersonalMilestoneState[] {
    return event.personalMilestones.filter(
      (milestone) => milestone.isUnlocked && !milestone.isClaimed,
    );
  }

  canClaimEventMilestones(event: EventQuestState): boolean {
    return (
      this.claimableMilestones(event).length > 0 &&
      new Date(event.claimEndsAtUtc).getTime() >= Date.now()
    );
  }

  canClaimEventMilestone(
    event: EventQuestState,
    milestone: EventQuestPersonalMilestoneState,
  ): boolean {
    return (
      milestone.isUnlocked &&
      !milestone.isClaimed &&
      new Date(event.claimEndsAtUtc).getTime() >= Date.now()
    );
  }

  milestoneProgress(
    event: EventQuestState,
    milestone: EventQuestPersonalMilestoneState,
  ): number {
    if (milestone.requiredContribution <= 0) return 100;
    return Math.min(
      100,
      Math.round((event.myContribution / milestone.requiredContribution) * 100),
    );
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
    const name =
      reward.type === 'Cinders'
        ? 'Cinders'
        : (reward.itemBaseId ?? reward.key)
            .split(/[._-]/)
            .filter(Boolean)
            .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
            .join(' ');
    return includeQuantity ? `${reward.quantity} ${name}` : name;
  }

  requiresChoice(quest: QuestState): boolean {
    return !!quest.choice && !quest.choice.selectedOptionKey;
  }

  choiceConfirmationLabel(quest: QuestState): string {
    return quest.questId === TRAINING_DAY_QUEST_ID
      ? 'Confirm First Hunt'
      : 'Confirm Reward';
  }

  chooseOption(option: QuestChoiceOption): void {
    this.pendingChoiceKey.set(option.key);
    this.choiceScrollPending = true;
  }

  private scrollChoiceConfirmationIntoView(): void {
    const scroller = this.questDetailScroller?.nativeElement;
    const confirmation = this.firstHuntConfirmation?.nativeElement;
    if (!scroller || !confirmation) return;

    const hiddenHeight =
      confirmation.getBoundingClientRect().bottom -
      scroller.getBoundingClientRect().bottom;
    if (hiddenHeight <= 0) return;

    scroller.scrollTo({
      top: scroller.scrollTop + hiddenHeight + 12,
      behavior: 'smooth',
    });
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
