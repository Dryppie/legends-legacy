import { QuestState, QuestStatus } from '../../../shared/models/quest';
import {
  buildQuestJournalEntries,
  groupQuestJournalEntries,
  preferredQuestForEntry,
  questJournalGroupForCategory,
} from './quest-journal-entry';

describe('quest journal entries', () => {
  it('keeps completed chain parts inside one active chain entry', () => {
    const entries = buildQuestJournalEntries([
      quest('part-1', QuestStatus.Completed, 1, 3),
      quest('part-2', QuestStatus.Active, 2, 3),
    ]);

    const activeEntry = entries.find(
      (entry) => entry.status === QuestStatus.Active,
    )!;
    const completedEntry = entries.find(
      (entry) => entry.status === QuestStatus.Completed,
    )!;
    expect(entries.length).toBe(2);
    expect(activeEntry.key).toBe('chain:campaign:active');
    expect(activeEntry.title).toBe('Campaign');
    expect(activeEntry.totalParts).toBe(3);
    expect(activeEntry.quests.map((part) => part.questId)).toEqual([
      'part-1',
      'part-2',
    ]);
    expect(preferredQuestForEntry(activeEntry).questId).toBe('part-2');
    expect(completedEntry.key).toBe('chain:campaign:completed');
    expect(completedEntry.totalParts).toBe(3);
    expect(completedEntry.quests.map((part) => part.questId)).toEqual([
      'part-1',
    ]);
  });

  it('moves the chain to completed only after every part is complete', () => {
    const entries = buildQuestJournalEntries([
      quest('part-1', QuestStatus.Completed, 1, 2),
      quest('part-2', QuestStatus.Completed, 2, 2),
    ]);
    const entry = entries[0];

    expect(entries.length).toBe(1);
    expect(entry.status).toBe(QuestStatus.Completed);
    expect(preferredQuestForEntry(entry).questId).toBe('part-2');
  });

  it('keeps standalone quests as individual entries', () => {
    const standalone = quest('standalone', QuestStatus.Completed);
    standalone.chain = null;

    const entry = buildQuestJournalEntries([standalone])[0];

    expect(entry.key).toBe('quest:standalone');
    expect(entry.isChain).toBeFalse();
    expect(entry.status).toBe(QuestStatus.Completed);
  });

  it('maps detailed quest categories into journal sections', () => {
    expect(questJournalGroupForCategory('Shenic')).toBe('World Map');
    expect(questJournalGroupForCategory('Dungeons')).toBe('World Map');
    expect(questJournalGroupForCategory('Gathering')).toBe('World Map');
    expect(questJournalGroupForCategory('Crafting')).toBe('Crafting');
    expect(questJournalGroupForCategory('Character')).toBe('Character');
    expect(questJournalGroupForCategory('Essences')).toBe('Character');
    expect(questJournalGroupForCategory('Tutorial')).toBe('Tutorial');
    expect(questJournalGroupForCategory('Colosseum')).toBe('Other');
    expect(questJournalGroupForCategory('New category')).toBe('Other');
  });

  it('groups entries in a stable journal section order', () => {
    const categories = [
      'Tutorial',
      'Colosseum',
      'Essences',
      'Crafting',
      'Shenic',
    ];
    const entries = categories.map((category, index) => {
      const state = quest('quest-' + index, QuestStatus.Active);
      state.chain = null;
      state.category = category;
      return buildQuestJournalEntries([state])[0];
    });

    const groups = groupQuestJournalEntries(entries);

    expect(groups.map((group) => group.key)).toEqual([
      'World Map',
      'Crafting',
      'Character',
      'Other',
      'Tutorial',
    ]);
    expect(groups.map((group) => group.entries[0].category)).toEqual([
      'Shenic',
      'Crafting',
      'Essences',
      'Colosseum',
      'Tutorial',
    ]);
  });
});

function quest(
  questId: string,
  status: QuestStatus,
  step = 1,
  totalSteps = 1,
): QuestState {
  return {
    questId,
    version: 1,
    title: 'Part ' + step,
    summary: 'Summary',
    category: 'Campaign',
    objectiveMode: 'Sequential',
    chain: {
      id: 'campaign',
      title: 'Campaign',
      description: 'Campaign description',
      goal: 'Complete the campaign.',
      promisedReward: 'A campaign reward.',
      step,
      totalSteps,
    },
    choice: null,
    sortOrder: step,
    status,
    isPinned: false,
    requiresWelcome: false,
    objectives: [
      {
        key: 'objective',
        description: 'Objective',
        type: 'Test',
        currentAmount: status === QuestStatus.Completed ? 1 : 0,
        requiredAmount: 1,
        isCompleted: status === QuestStatus.Completed,
        presentation: {
          actionLabel: '',
          destinationRoute: '',
        },
      },
    ],
    rewards: [],
  };
}
