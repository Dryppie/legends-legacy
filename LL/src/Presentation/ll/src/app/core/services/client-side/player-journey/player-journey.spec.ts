import {
  buildPlayerJourneyGuidance,
  filterSidebarForPlayerJourney,
  getPlayerJourneyDestinationRoute,
  isPlayerJourneyOnboardingComplete,
  PlayerJourneyStage,
  resolvePlayerJourneyStage,
} from './player-journey';
import {
  FIRST_WEAPON_QUEST_ID,
  HEART_OF_THE_HOLLOW_QUEST_ID,
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
  it('guides character using the Forges through equipment and accessories without unlocking tutorial crafting', () => {
    const current = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
    );
    const accessories = quest(
      TOOLS_OF_THE_TRADE_QUEST_ID,
      QuestStatus.Active,
      true,
    );
    accessories.title = 'Ready for the Road';
    current.quests.push(accessories);
    const guidance = buildPlayerJourneyGuidance(current, 1)!;
    expect(guidance.title).toBe('Ready for the Road');
    expect(guidance.phaseLabel).toBe('Ready for the Road');
    expect(guidance.optionalAction.route).toBe('/game/character/inventory');
    expect(guidance.nextUnlock).toContain('equipping your accessories');
    expect(
      itemIds(filterSidebarForPlayerJourney(sidebar(), current, 1, true)),
    ).not.toContain('crafting');
  });
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
    )!;

    expect(guidance.title).toBe('Your First Hunt');
    expect(guidance.objective).toBe('Defeat your chosen First Hunt target.');
    expect(guidance.primaryAction).toEqual({
      label: 'Enter Training Area',
      route: '/game/world/shenic?area=tutorial_area_training_grounds',
    });
  });

  it('shows authored chapter identity and promised reward for the current Shenic quest', () => {
    const current = quest(
      'quest.shenic.blood_in_the_grove',
      QuestStatus.Active,
      true,
    );
    current.chain = {
      id: 'chain.shenic.chapter_01',
      title: 'Chapter I — First Blood',
      description: 'Complete the opening chapter.',
      goal: 'Defeat the boss of Goblin Mines I.',
      promisedReward: 'Choose one of three guaranteed Essences.',
      step: 2,
      totalSteps: 2,
    };
    const completedTutorial = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
      TOOLS_OF_THE_TRADE_QUEST_ID,
      INTO_LUMO_RUINS_QUEST_ID,
    );
    completedTutorial.quests.push(current);
    completedTutorial.pinnedQuestId = current.questId;

    const guidance = buildPlayerJourneyGuidance(completedTutorial, 8)!;

    expect(guidance.phaseLabel).toBe('Chapter I — First Blood');
    expect(guidance.nextUnlockLabel).toBe('Chapter reward');
    expect(guidance.nextUnlock).toBe(
      'Choose one of three guaranteed Essences.',
    );
  });

  it('celebrates the focused Beta journey after the level-30 capstone', () => {
    const completed = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
      TOOLS_OF_THE_TRADE_QUEST_ID,
      INTO_LUMO_RUINS_QUEST_ID,
      HEART_OF_THE_HOLLOW_QUEST_ID,
    );

    expect(resolvePlayerJourneyStage(completed)).toBe(
      PlayerJourneyStage.BetaComplete,
    );
    const guidance = buildPlayerJourneyGuidance(completed, 30)!;
    expect(guidance.title).toBe('Shenic Beta journey complete');
    expect(guidance.nextUnlockLabel).toBe('Future aspiration');
  });

  it('resumes normal quest guidance when post-Beta Shenic progression is active', () => {
    const completed = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
      TOOLS_OF_THE_TRADE_QUEST_ID,
      INTO_LUMO_RUINS_QUEST_ID,
      HEART_OF_THE_HOLLOW_QUEST_ID,
    );
    const futureQuest = quest(
      'quest.shenic.ash_beneath_the_earth',
      QuestStatus.Active,
    );
    futureQuest.title = 'Ash Beneath the Earth';
    futureQuest.sortOrder = 170;
    futureQuest.chain = {
      id: 'chain.shenic.future',
      title: 'Beyond the Focused Beta',
      description: 'Continue through the rest of Shenic.',
      goal: 'Finish the wider Shenic campaign.',
      promisedReward: 'Future Shenic rewards.',
      step: 1,
      totalSteps: 3,
    };
    futureQuest.objectives = [
      {
        key: 'descend_embercap',
        description: 'Win 12 encounters in Embercap Burrows.',
        type: 'CombatEncounterCompleted',
        currentAmount: 0,
        requiredAmount: 12,
        isCompleted: false,
        presentation: {
          actionLabel: 'Head to Embercap Burrows',
          destinationRoute: '/game/world/shenic?area=region_01_area_10',
        },
      },
    ];
    completed.quests.push(futureQuest);

    const guidance = buildPlayerJourneyGuidance(completed, 94)!;

    expect(guidance.title).toBe('Ash Beneath the Earth');
    expect(guidance.phaseLabel).toBe('Beyond the Focused Beta');
    expect(guidance.objective).toBe('Win 12 encounters in Embercap Burrows.');
    expect(guidance.primaryAction).toEqual({
      label: 'Head to Embercap Burrows',
      route: '/game/world/shenic?area=region_01_area_10',
    });
  });

  it('hides journey guidance for a veteran with no active authored quest', () => {
    const completed = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
      TOOLS_OF_THE_TRADE_QUEST_ID,
      INTO_LUMO_RUINS_QUEST_ID,
      HEART_OF_THE_HOLLOW_QUEST_ID,
    );

    expect(buildPlayerJourneyGuidance(completed, 94)).toBeNull();
  });

  it('progressively reveals navigation and restores the full game at level 30', () => {
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
      'prophecies',
      'guild',
      'colosseum',
      'settings',
    ]);

    const completedOnboarding = journal(
      TRAINING_DAY_QUEST_ID,
      SOUL_ARCHIVE_QUEST_ID,
      FIRST_WEAPON_QUEST_ID,
      TOOLS_OF_THE_TRADE_QUEST_ID,
      INTO_LUMO_RUINS_QUEST_ID,
    );
    expect(
      itemIds(
        filterSidebarForPlayerJourney(sidebar(), completedOnboarding, 20, true),
      ),
    ).toEqual([
      'character-overview',
      'inventory',
      'essences',
      'achievements',
      'soulstone-archive',
      'world',
      'quests',
      'prophecies',
      'guild',
      'colosseum',
      'market-place',
      'tavern',
      'settings',
    ]);

    expect(
      filterSidebarForPlayerJourney(sidebar(), journal(), 30, true),
    ).toEqual(sidebar());
    expect(isPlayerJourneyOnboardingComplete(journal(), 30)).toBeTrue();
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
    default:
      return ['city', itemId];
  }
}

function itemIds(sections: SidebarSection[]): string[] {
  return sections.flatMap((section) => section.items.map((item) => item.id));
}
