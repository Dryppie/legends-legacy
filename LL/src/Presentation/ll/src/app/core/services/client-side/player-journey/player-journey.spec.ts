import {
  buildPlayerJourneyGuidance,
  filterSidebarForPlayerJourney,
  getPlayerJourneyDestinationRoute,
  PlayerJourneyStage,
  resolvePlayerJourneyStage,
} from './player-journey';
import {
  FIRST_WEAPON_QUEST_ID,
  INTO_LUMO_RUINS_QUEST_ID,
  QuestJournal,
  QuestState,
  QuestStatus,
  SOUL_ARCHIVE_QUEST_ID,
  TOOLS_OF_THE_TRADE_QUEST_ID,
  TRAINING_DAY_QUEST_ID,
} from '../../../../shared/models/quest';
import { SidebarSection } from '../../../../shared/models/sidebar-item';

describe('player journey', () => {
  it('advances through the tutorial using completed quest state', () => {
    expect(resolvePlayerJourneyStage(journal())).toBe(
      PlayerJourneyStage.FirstHunt,
    );
    expect(resolvePlayerJourneyStage(journal(TRAINING_DAY_QUEST_ID))).toBe(
      PlayerJourneyStage.SoulArchive,
    );
    expect(
      resolvePlayerJourneyStage(
        journal(
          TRAINING_DAY_QUEST_ID,
          SOUL_ARCHIVE_QUEST_ID,
          FIRST_WEAPON_QUEST_ID,
          TOOLS_OF_THE_TRADE_QUEST_ID,
          INTO_LUMO_RUINS_QUEST_ID,
        ),
      ),
    ).toBe(PlayerJourneyStage.Shenic);
  });

  it('uses the current objective as the recommended action', () => {
    const current = quest(TRAINING_DAY_QUEST_ID, QuestStatus.Active, true);
    current.objectives = [
      {
        key: 'hunt',
        description: 'Defeat your chosen First Hunt target.',
        type: 'EncounterWon',
        currentAmount: 0,
        requiredAmount: 1,
        isCompleted: false,
        presentation: {
          actionLabel: 'Enter Training Area',
          destinationRoute:
            '/game/world/shenic?area=tutorial_area_training_grounds',
        },
      },
    ];
    const guidance = buildPlayerJourneyGuidance(
      { quests: [current], pinnedQuestId: current.questId },
      1,
    );

    expect(guidance.title).toBe('Your First Hunt');
    expect(guidance.objective).toBe('Defeat your chosen First Hunt target.');
    expect(guidance.primaryAction).toEqual({
      label: 'Enter Training Area',
      route: '/game/world/shenic?area=tutorial_area_training_grounds',
    });
  });

  it('reveals only the navigation required by the current journey stage', () => {
    const sections = sidebar();

    expect(
      itemIds(filterSidebarForPlayerJourney(sections, journal(), 1, true)),
    ).toEqual(['character-overview', 'quests', 'settings']);

    expect(
      itemIds(
        filterSidebarForPlayerJourney(
          sections,
          journal(TRAINING_DAY_QUEST_ID, SOUL_ARCHIVE_QUEST_ID),
          1,
          true,
        ),
      ),
    ).toEqual([
      'character-overview',
      'inventory',
      'essences',
      'quests',
      'crafting',
      'settings',
    ]);

    expect(
      itemIds(
        filterSidebarForPlayerJourney(
          sections,
          journal(
            TRAINING_DAY_QUEST_ID,
            SOUL_ARCHIVE_QUEST_ID,
            FIRST_WEAPON_QUEST_ID,
            TOOLS_OF_THE_TRADE_QUEST_ID,
            INTO_LUMO_RUINS_QUEST_ID,
          ),
          10,
          true,
        ),
      ),
    ).toEqual([
      'character-overview',
      'inventory',
      'essences',
      'achievements',
      'soulstone-archive',
      'world',
      'quests',
      'crafting',
      'settings',
    ]);
  });

  it('does not expose the hunt destination until a First Hunt is selected', () => {
    const firstHunt = quest(TRAINING_DAY_QUEST_ID, QuestStatus.Active, true);
    firstHunt.choice = {
      selectionTitle: 'Choose Your First Hunt',
      selectionSummary: 'Choose the creature you want to hunt.',
      confirmationText: 'Begin hunt',
      options: [],
    };
    firstHunt.objectives = [
      {
        key: 'hunt',
        description: 'Defeat your chosen First Hunt target.',
        type: 'EncounterWon',
        currentAmount: 0,
        requiredAmount: 1,
        isCompleted: false,
        presentation: {
          actionLabel: 'Enter Training Area',
          destinationRoute:
            '/game/world/shenic?area=tutorial_area_training_grounds',
        },
      },
    ];
    const unresolvedJournal: QuestJournal = {
      quests: [firstHunt],
      pinnedQuestId: firstHunt.questId,
    };

    expect(getPlayerJourneyDestinationRoute(unresolvedJournal)).toBeNull();
    expect(
      itemIds(
        filterSidebarForPlayerJourney(sidebar(), unresolvedJournal, 1, true),
      ),
    ).toEqual(['character-overview', 'quests', 'settings']);

    firstHunt.choice.selectedOptionKey = 'goblin';

    expect(getPlayerJourneyDestinationRoute(unresolvedJournal)).toBe(
      '/game/world/shenic?area=tutorial_area_training_grounds',
    );
    expect(
      itemIds(
        filterSidebarForPlayerJourney(sidebar(), unresolvedJournal, 1, true),
      ),
    ).toEqual(['character-overview', 'world', 'quests', 'settings']);
  });

  it('preserves the complete navigation when focused Beta mode is off', () => {
    const sections = sidebar();
    expect(filterSidebarForPlayerJourney(sections, journal(), 1, false)).toBe(
      sections,
    );
  });
});

function journal(...completedQuestIds: string[]): QuestJournal {
  return {
    quests: completedQuestIds.map((questId) =>
      quest(questId, QuestStatus.Completed),
    ),
  };
}

function quest(
  questId: string,
  status: QuestStatus,
  isPinned = false,
): QuestState {
  return {
    questId,
    version: 1,
    title: questId === TRAINING_DAY_QUEST_ID ? 'Your First Hunt' : questId,
    summary: 'Follow the journey.',
    category: 'Tutorial',
    objectiveMode: 'Sequential',
    sortOrder: 1,
    status,
    isPinned,
    requiresWelcome: false,
    objectives: [],
    rewards: [],
  };
}

function sidebar(): SidebarSection[] {
  const section = (id: string, itemIds: string[]): SidebarSection => ({
    id,
    label: id,
    items: itemIds.map((itemId) => ({
      id: itemId,
      route: routeFor(itemId),
      icon: itemId,
      title: itemId,
    })),
  });

  return [
    section('character', [
      'character-overview',
      'inventory',
      'essences',
      'achievements',
      'soulstone-archive',
    ]),
    section('world', ['world', 'legacy-ascension', 'quests', 'prophecies']),
    section('professions', ['crafting']),
    section('city', ['guild', 'colosseum', 'market-place', 'tavern']),
    section('system', ['settings']),
  ];
}

function routeFor(itemId: string): string[] {
  switch (itemId) {
    case 'character-overview':
    case 'inventory':
    case 'essences':
    case 'achievements':
    case 'soulstone-archive':
      return ['character', itemId];
    case 'world':
      return ['world'];
    case 'quests':
    case 'prophecies':
    case 'settings':
      return [itemId];
    case 'crafting':
      return ['professions', 'crafting'];
    default:
      return ['city', itemId];
  }
}

function itemIds(sections: SidebarSection[]): string[] {
  return sections.flatMap((section) => section.items.map((item) => item.id));
}
