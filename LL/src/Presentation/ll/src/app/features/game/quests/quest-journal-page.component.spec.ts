import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { EventQuestStateService } from '../../../core/services/api/quest/event-quest-state.service';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import {
  EventQuestState,
  EventQuestStatus,
} from '../../../shared/models/event-quest';
import { QuestState, QuestStatus } from '../../../shared/models/quest';
import { QuestJournalPageComponent } from './quest-journal-page.component';

describe('QuestJournalPageComponent', () => {
  it('does not keep the first active quest highlighted when an event is selected', () => {
    const quest = createQuest();
    const event = createEvent();
    const component = new QuestJournalPageComponent(
      {
        journal: signal({ quests: [quest] }).asReadonly(),
      } as unknown as QuestStateService,
      {
        journal: signal({ events: [event] }).asReadonly(),
      } as unknown as EventQuestStateService,
      {} as Router,
      new EssenceItemViewService(),
    );
    const entry = component.visibleEntries()[0];

    component.selectEntry(entry);
    expect(component.isSelected(entry)).toBeTrue();

    component.selectEvent(event);

    expect(component.isSelectedEvent(event)).toBeTrue();
    expect(component.isSelected(entry)).toBeFalse();
  });

  it('scrolls the quest detail pane down to reveal the first-hunt confirmation', () => {
    const component = createComponent();
    const scrollTo = jasmine.createSpy('scrollTo');
    const scroller = {
      scrollTop: 100,
      getBoundingClientRect: () => ({ bottom: 600 }),
      scrollTo,
    } as unknown as HTMLElement;
    const confirmation = {
      getBoundingClientRect: () => ({ bottom: 750 }),
    } as unknown as HTMLElement;
    (
      component as unknown as {
        questDetailScroller: { nativeElement: HTMLElement };
        firstHuntConfirmation: { nativeElement: HTMLElement };
      }
    ).questDetailScroller = { nativeElement: scroller };
    (
      component as unknown as {
        firstHuntConfirmation: { nativeElement: HTMLElement };
      }
    ).firstHuntConfirmation = { nativeElement: confirmation };
    spyOn(window, 'requestAnimationFrame').and.callFake((callback) => {
      callback(0);
      return 1;
    });

    component.chooseOption({ key: 'goblin-warrior' } as never);

    expect(component.pendingChoiceKey()).toBe('goblin-warrior');
    expect(scrollTo).toHaveBeenCalledOnceWith({
      top: 262,
      behavior: 'smooth',
    });
  });
});

function createComponent(): QuestJournalPageComponent {
  return new QuestJournalPageComponent(
    {
      journal: signal({ quests: [] }).asReadonly(),
    } as unknown as QuestStateService,
    {
      journal: signal({ events: [] }).asReadonly(),
    } as unknown as EventQuestStateService,
    {} as Router,
    new EssenceItemViewService(),
  );
}

function createQuest(): QuestState {
  return {
    questId: 'quest.active',
    version: 1,
    title: 'Active quest',
    summary: 'Complete an objective.',
    category: 'Test',
    objectiveMode: 'Sequential',
    sortOrder: 1,
    status: QuestStatus.Active,
    isPinned: false,
    requiresWelcome: false,
    objectives: [],
    rewards: [],
  };
}

function createEvent(): EventQuestState {
  return {
    eventQuestId: 'event.active',
    version: 1,
    title: 'Server-wide event',
    summary: 'Contribute to the event.',
    status: EventQuestStatus.Active,
    startsAtUtc: '2026-08-01T00:00:00Z',
    endsAtUtc: '2026-08-31T00:00:00Z',
    claimEndsAtUtc: '2026-09-07T00:00:00Z',
    minimumContribution: 0,
    myContribution: 0,
    isEligible: false,
    hasClaimed: false,
    contributorCount: 0,
    sortOrder: 1,
    objectives: [],
    rewards: [],
    personalMilestones: [],
    topContributors: [],
  };
}
